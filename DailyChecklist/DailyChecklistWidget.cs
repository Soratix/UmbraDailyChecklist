namespace Umbra.DailyChecklist;

[ToolbarWidget(
    "DailyChecklist",
    "Quotidien & hebdo",
    "Compte à rebours des resets et liste de ce qu'il reste à faire : roulettes, tribus alliées, livre d'Idylle, livraisons spéciales.",
    ["daily", "weekly", "reset", "roulette", "checklist", "quotidien", "hebdo", "tribu", "idylle", "wondrous"]
)]
public sealed class DailyChecklistWidget(
    WidgetInfo                  info,
    string?                     guid         = null,
    Dictionary<string, object>? configValues = null
) : StandardToolbarWidget(info, guid, configValues)
{
    private enum Section { Daily, Weekly }

    private sealed record Item(string Id, Section Section, string Label, string Status, bool Done, Action? OnClick);

    private static readonly Color DoneColor = new(0xFF66DD66);
    private static readonly Color TodoColor = new(0xFF5AB4FF);

    protected override StandardWidgetFeatures Features =>
        StandardWidgetFeatures.Text |
        StandardWidgetFeatures.SubText |
        StandardWidgetFeatures.Icon |
        StandardWidgetFeatures.CustomizableIcon;

    protected override string          DefaultIconType        => IconTypeFontAwesome;
    protected override FontAwesomeIcon DefaultFontAwesomeIcon => FontAwesomeIcon.ClipboardCheck;

    public override MenuPopup Popup { get; } = new();

    private readonly Dictionary<Section, MenuPopup.Group> _groups  = [];
    private readonly Dictionary<string, MenuPopup.Button> _buttons = [];

    private List<Item> _items         = [];
    private string     _menuSignature = string.Empty;
    private DateTime   _nextRefresh   = DateTime.MinValue;

    // ------------------------------------------------------------------ Config

    protected override IEnumerable<IWidgetConfigVariable> GetConfigVariables()
    {
        const string cat = "Quotidien & hebdo";

        return [
            ..base.GetConfigVariables(),
            new BooleanWidgetConfigVariable("TrackTribes", "Allocations de tribus alliées", "Quotidien : allocations de quêtes de tribus restantes.", true) { Category = cat },
            new BooleanWidgetConfigVariable("TrackWondrousTails", "Livre d'Idylle", "Hebdomadaire : livre d'Idylle (Wondrous Tails) à récupérer, à compléter ou à rendre.", true) { Category = cat },
            new BooleanWidgetConfigVariable("TrackDeliveries", "Livraisons spéciales", "Hebdomadaire : allocations de livraisons spéciales restantes.", true) { Category = cat },
            new BooleanWidgetConfigVariable("CountWeekly", "Compter l'hebdo dans le total", "Le nombre affiché dans la barre inclut aussi les tâches hebdomadaires.", true) { Category = cat },
            new BooleanWidgetConfigVariable("ShowGcReset", "Afficher le reset de Grande Compagnie", "Ajoute le compte à rebours du reset des livraisons de Grande Compagnie (20:00 UTC) dans l'infobulle.", true) { Category = cat },
            new BooleanWidgetConfigVariable("HideDone", "Masquer les tâches faites", "N'affiche dans la liste que ce qu'il reste à faire.", false) { Category = cat },
            ..GetRouletteVariables(),
        ];
    }

    private static IEnumerable<IWidgetConfigVariable> GetRouletteVariables()
    {
        IReadOnlyList<RouletteInfo> roulettes;
        try {
            roulettes = RouletteRepository.Roulettes;
        } catch (Exception e) {
            Logger.Error($"[DailyChecklist] Impossible de lire les roulettes : {e.Message}");
            yield break;
        }

        foreach (var r in roulettes) {
            yield return new BooleanWidgetConfigVariable(RouletteVar(r), r.Name, "Suivre cette roulette quotidienne.", r.DefaultTracked) {
                Category = "Roulettes",
            };
        }
    }

    private static string RouletteVar(RouletteInfo r) => $"Roulette_{r.Id}";

    // --------------------------------------------------------------- Lifecycle

    protected override void OnLoad()
    {
        Popup.OnPopupOpen += OnPopupOpened;
        Node.OnRightClick += _ => Framework.Service<IChatSender>().Send("/dutyfinder");

        SetText("Quotidien & hebdo");
        SetSubText("Chargement…");
    }

    protected override void OnUnload()
    {
        Popup.OnPopupOpen -= OnPopupOpened;
        ClearMenu();
    }

    protected override void OnDraw()
    {
        if (DateTime.UtcNow < _nextRefresh) return;
        _nextRefresh = DateTime.UtcNow.AddMilliseconds(500);

        var now = DateTime.UtcNow;

        if (!GameState.IsAvailable) {
            SetText("Quotidien & hebdo");
            SetSubText($"Reset dans {Resets.Format(Resets.NextDaily(now) - now)}");
            return;
        }

        try {
            _items = CollectItems();
        } catch (Exception e) {
            Logger.Error($"[DailyChecklist] Erreur de lecture de l'état du jeu : {e}");
            SetSubText("Erreur de lecture");
            return;
        }

        UpdateToolbarButton(now);
        if (Popup.IsOpen) UpdateMenu(now);
    }

    // -------------------------------------------------------------- Collecte

    private List<Item> CollectItems()
    {
        List<Item> items = [];

        foreach (var r in RouletteRepository.Roulettes) {
            if (!GetConfigValue<bool>(RouletteVar(r))) continue;

            bool done = GameState.IsRouletteComplete(r.Id);
            items.Add(new($"R{r.Id}", Section.Daily, r.Name, done ? "Fait" : "À faire", done,
                () => Framework.Service<IChatSender>().Send("/dutyfinder")));
        }

        if (GetConfigValue<bool>("TrackTribes")) {
            int left = GameState.BeastTribeAllowances;
            items.Add(new("Tribes", Section.Daily, "Tribus alliées", left == 0 ? "Fait" : $"{left} allocation{(left > 1 ? "s" : "")}", left == 0, null));
        }

        if (GetConfigValue<bool>("TrackWondrousTails")) {
            var (has, expired, stickers) = GameState.WondrousTails;

            var (status, done) = (has && !expired) switch {
                false              => ("À récupérer", false),
                true when stickers >= 9 => ("Complet, à rendre", false),
                _                  => ($"{stickers}/9 vignettes", false),
            };

            items.Add(new("WT", Section.Weekly, "Livre d'Idylle", status, done, null));
        }

        if (GetConfigValue<bool>("TrackDeliveries")) {
            int left = GameState.CustomDeliveryAllowances;
            if (left >= 0) {
                items.Add(new("CD", Section.Weekly, "Livraisons spéciales", left == 0 ? "Fait" : $"{left} restante{(left > 1 ? "s" : "")}", left == 0, null));
            }
        }

        return items;
    }

    // ----------------------------------------------------------------- Display

    private void UpdateToolbarButton(DateTime now)
    {
        bool countWeekly = GetConfigValue<bool>("CountWeekly");
        int  remaining   = _items.Count(i => !i.Done && (countWeekly || i.Section == Section.Daily));

        SetText(remaining == 0 ? "Tout est fait" : $"{remaining} tâche{(remaining > 1 ? "s" : "")} restante{(remaining > 1 ? "s" : "")}");
        SetSubText($"Reset dans {Resets.Format(Resets.NextDaily(now) - now)}");

        List<string> lines = [
            $"Reset quotidien : {Resets.Format(Resets.NextDaily(now) - now)}",
            $"Reset hebdomadaire : {Resets.Format(Resets.NextWeekly(now) - now)}",
        ];

        if (GetConfigValue<bool>("ShowGcReset")) {
            lines.Add($"Reset Grande Compagnie : {Resets.Format(Resets.NextGrandCompany(now) - now)}");
        }

        lines.Add("");
        lines.Add("Clic : liste · Clic droit : outil de mission");

        SetTooltip(string.Join("\n", lines));
    }

    private void OnPopupOpened()
    {
        _menuSignature = string.Empty;
        UpdateMenu(DateTime.UtcNow);
    }

    private void UpdateMenu(DateTime now)
    {
        bool hideDone = GetConfigValue<bool>("HideDone");
        var  visible  = _items.Where(i => !hideDone || !i.Done).ToList();

        string signature = string.Join(",", visible.Select(i => i.Id));

        if (signature != _menuSignature) {
            _menuSignature = signature;
            ClearMenu();

            int sort = 0;
            foreach (var section in new[] { Section.Daily, Section.Weekly }) {
                var sectionItems = visible.Where(i => i.Section == section).ToList();
                if (sectionItems.Count == 0) continue;

                var group = new MenuPopup.Group(string.Empty) { SortIndex = sort++ };
                _groups[section] = group;
                Popup.Add(group);

                int index = 0;
                foreach (var item in sectionItems) {
                    var button = new MenuPopup.Button(item.Label) {
                        Id                = item.Id,
                        SortIndex         = index++,
                        ClosePopupOnClick = item.OnClick != null,
                        OnClick           = item.OnClick,
                    };
                    _buttons[item.Id] = button;
                    group.Add(button);
                }
            }

            if (visible.Count == 0) {
                Popup.Add(new MenuPopup.Header("Tout est fait, bravo !"));
            }
        }

        // Mise à jour des textes sans reconstruire le menu.
        if (_groups.TryGetValue(Section.Daily, out var daily)) {
            daily.Label = $"Quotidien · reset dans {Resets.Format(Resets.NextDaily(now) - now)}";
        }
        if (_groups.TryGetValue(Section.Weekly, out var weekly)) {
            weekly.Label = $"Hebdomadaire · reset dans {Resets.Format(Resets.NextWeekly(now) - now)}";
        }

        foreach (var item in visible) {
            if (!_buttons.TryGetValue(item.Id, out var button)) continue;

            button.AltText      = item.Status;
            button.AltTextColor = item.Done ? DoneColor : TodoColor;
            button.Icon         = item.Done ? FontAwesomeIcon.CheckCircle : FontAwesomeIcon.Circle;
            button.IconColor    = item.Done ? DoneColor : null;
        }
    }

    private void ClearMenu()
    {
        Popup.Clear(true);
        _groups.Clear();
        _buttons.Clear();
    }
}
