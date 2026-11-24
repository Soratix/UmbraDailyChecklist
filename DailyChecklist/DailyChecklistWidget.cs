namespace Umbra.DailyChecklist;

/// <summary>
/// Le nom et la description d'un [ToolbarWidget] sont des constantes. Umbra lit l'attribut
/// avec GetCustomAttribute au démarrage de son framework, ce qui accepte une sous-classe :
/// celle-ci choisit le nom et la description dans la langue du jeu à ce moment-là.
/// </summary>
internal sealed class LocalizedToolbarWidgetAttribute() : ToolbarWidgetAttribute(
    "DailyChecklist",
    Strings.T("WidgetName"),
    Strings.T("WidgetDesc"),
    ["daily", "weekly", "reset", "roulette", "checklist", "quotidien", "hebdo", "tribu", "idylle", "wondrous", "tails", "allied", "society", "delivery"]
);

[LocalizedToolbarWidget]
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
        string cat = Strings.T("WidgetName");

        return [
            ..base.GetConfigVariables(),
            new BooleanWidgetConfigVariable("TrackTribes", Strings.T("Cfg.Tribes"), Strings.T("Cfg.Tribes.Desc"), true) { Category = cat },
            new BooleanWidgetConfigVariable("TrackWondrousTails", Strings.T("Cfg.WT"), Strings.T("Cfg.WT.Desc"), true) { Category = cat },
            new BooleanWidgetConfigVariable("TrackDeliveries", Strings.T("Cfg.Deliveries"), Strings.T("Cfg.Deliveries.Desc"), true) { Category = cat },
            new BooleanWidgetConfigVariable("CountWeekly", Strings.T("Cfg.CountWeekly"), Strings.T("Cfg.CountWeekly.Desc"), true) { Category = cat },
            new BooleanWidgetConfigVariable("ShowGcReset", Strings.T("Cfg.ShowGcReset"), Strings.T("Cfg.ShowGcReset.Desc"), true) { Category = cat },
            new BooleanWidgetConfigVariable("HideDone", Strings.T("Cfg.HideDone"), Strings.T("Cfg.HideDone.Desc"), false) { Category = cat },
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
            yield return new BooleanWidgetConfigVariable(RouletteVar(r), r.Name, Strings.T("Cfg.Roulette.Desc"), r.DefaultTracked) {
                Category = Strings.T("Cfg.Roulettes"),
            };
        }
    }

    private static string RouletteVar(RouletteInfo r) => $"Roulette_{r.Id}";

    // --------------------------------------------------------------- Lifecycle

    protected override void OnLoad()
    {
        Popup.OnPopupOpen += OnPopupOpened;
        Node.OnRightClick += _ => Framework.Service<IChatSender>().Send("/dutyfinder");

        SetText(Strings.T("WidgetName"));
        SetSubText(Strings.T("Loading"));
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
            SetText(Strings.T("WidgetName"));
            SetSubText(Strings.T("ResetIn", Resets.Format(Resets.NextDaily(now) - now)));
            return;
        }

        try {
            _items = CollectItems();
        } catch (Exception e) {
            Logger.Error($"[DailyChecklist] Erreur de lecture de l'état du jeu : {e}");
            SetSubText(Strings.T("ReadError"));
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
            items.Add(new($"R{r.Id}", Section.Daily, r.Name, done ? Strings.T("Done") : Strings.T("Todo"), done,
                () => Framework.Service<IChatSender>().Send("/dutyfinder")));
        }

        if (GetConfigValue<bool>("TrackTribes")) {
            int left = GameState.BeastTribeAllowances;
            items.Add(new("Tribes", Section.Daily, Strings.T("Tribes"), left == 0 ? Strings.T("Done") : Strings.Plural("Allowance", left), left == 0, null));
        }

        if (GetConfigValue<bool>("TrackWondrousTails")) {
            var (has, expired, stickers) = GameState.WondrousTails;

            var (status, done) = (has && !expired) switch {
                false                   => (Strings.T("WT.PickUp"), false),
                true when stickers >= 9 => (Strings.T("WT.TurnIn"), false),
                _                       => (Strings.T("WT.Stickers", stickers), false),
            };

            items.Add(new("WT", Section.Weekly, Strings.T("WT"), status, done, null));
        }

        if (GetConfigValue<bool>("TrackDeliveries")) {
            int left = GameState.CustomDeliveryAllowances;
            if (left >= 0) {
                items.Add(new("CD", Section.Weekly, Strings.T("Deliveries"), left == 0 ? Strings.T("Done") : Strings.Plural("Remaining", left), left == 0, null));
            }
        }

        return items;
    }

    // ----------------------------------------------------------------- Display

    private void UpdateToolbarButton(DateTime now)
    {
        bool countWeekly = GetConfigValue<bool>("CountWeekly");
        int  remaining   = _items.Count(i => !i.Done && (countWeekly || i.Section == Section.Daily));

        SetText(remaining == 0 ? Strings.T("AllDone") : Strings.Plural("TasksLeft", remaining));
        SetSubText(Strings.T("ResetIn", Resets.Format(Resets.NextDaily(now) - now)));

        List<string> lines = [
            Strings.T("DailyReset", Resets.Format(Resets.NextDaily(now) - now)),
            Strings.T("WeeklyReset", Resets.Format(Resets.NextWeekly(now) - now)),
        ];

        if (GetConfigValue<bool>("ShowGcReset")) {
            lines.Add(Strings.T("GcReset", Resets.Format(Resets.NextGrandCompany(now) - now)));
        }

        lines.Add("");
        lines.Add(Strings.T("TooltipHint"));

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
                Popup.Add(new MenuPopup.Header(Strings.T("AllDoneBravo")));
            }
        }

        // Mise à jour des textes sans reconstruire le menu.
        if (_groups.TryGetValue(Section.Daily, out var daily)) {
            daily.Label = Strings.T("DailyHeader", Resets.Format(Resets.NextDaily(now) - now));
        }
        if (_groups.TryGetValue(Section.Weekly, out var weekly)) {
            weekly.Label = Strings.T("WeeklyHeader", Resets.Format(Resets.NextWeekly(now) - now));
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
