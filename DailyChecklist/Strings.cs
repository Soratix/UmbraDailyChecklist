using Dalamud.Game;

namespace Umbra.DailyChecklist;

/// <summary>
/// Textes du widget en français et en anglais.
/// Umbra ne permet pas aux widgets tiers d'ajouter leurs clés à sa propre table I18N :
/// on suit donc simplement la langue choisie par Umbra (I18N.GetCurrentLanguage),
/// et à défaut celle du client du jeu. Français si « fr », anglais sinon.
/// </summary>
internal static class Strings
{
    public static bool IsFrench
    {
        get {
            try {
                string lang = I18N.GetCurrentLanguage();
                if (!string.IsNullOrEmpty(lang)) return lang.StartsWith("fr", StringComparison.OrdinalIgnoreCase);
            } catch {
                // I18N pas encore prêt : on se rabat sur la langue du client.
            }

            try {
                return Framework.Service<IClientState>().ClientLanguage == ClientLanguage.French;
            } catch {
                return false;
            }
        }
    }

    private static readonly Dictionary<string, (string Fr, string En)> Table = new() {
        ["WidgetName"]        = ("Quotidien & hebdo", "Daily & Weekly"),
        ["Loading"]           = ("Chargement…", "Loading…"),
        ["ReadError"]         = ("Erreur de lecture", "Read error"),
        ["ResetIn"]           = ("Reset dans {0}", "Reset in {0}"),

        // Réglages
        ["Cfg.Roulettes"]          = ("Roulettes", "Roulettes"),
        ["Cfg.Tribes"]             = ("Allocations de tribus alliées", "Allied society allowances"),
        ["Cfg.Tribes.Desc"]        = ("Quotidien : allocations de quêtes de tribus restantes.", "Daily: remaining allied society quest allowances."),
        ["Cfg.WT"]                 = ("Livre d'Idylle", "Wondrous Tails"),
        ["Cfg.WT.Desc"]            = ("Hebdomadaire : livre d'Idylle (Wondrous Tails) à récupérer, à compléter ou à rendre.", "Weekly: Wondrous Tails journal to pick up, complete or turn in."),
        ["Cfg.Deliveries"]         = ("Livraisons spéciales", "Custom deliveries"),
        ["Cfg.Deliveries.Desc"]    = ("Hebdomadaire : allocations de livraisons spéciales restantes.", "Weekly: remaining custom delivery allowances."),
        ["Cfg.CountWeekly"]        = ("Compter l'hebdo dans le total", "Count weekly tasks in the total"),
        ["Cfg.CountWeekly.Desc"]   = ("Le nombre affiché dans la barre inclut aussi les tâches hebdomadaires.", "The number shown in the toolbar also includes weekly tasks."),
        ["Cfg.ShowGcReset"]        = ("Afficher le reset de Grande Compagnie", "Show the Grand Company reset"),
        ["Cfg.ShowGcReset.Desc"]   = ("Ajoute le compte à rebours du reset des livraisons de Grande Compagnie (20:00 UTC) dans l'infobulle.", "Adds the Grand Company delivery reset countdown (20:00 UTC) to the tooltip."),
        ["Cfg.HideDone"]           = ("Masquer les tâches faites", "Hide completed tasks"),
        ["Cfg.HideDone.Desc"]      = ("N'affiche dans la liste que ce qu'il reste à faire.", "Only list what is left to do."),
        ["Cfg.Roulette.Desc"]      = ("Suivre cette roulette quotidienne.", "Track this daily roulette."),

        // Éléments de la liste
        ["Done"]              = ("Fait", "Done"),
        ["Todo"]              = ("À faire", "To do"),
        ["Tribes"]            = ("Tribus alliées", "Allied societies"),
        ["Allowance.One"]     = ("{0} allocation", "{0} allowance"),
        ["Allowance.Many"]    = ("{0} allocations", "{0} allowances"),
        ["WT"]                = ("Livre d'Idylle", "Wondrous Tails"),
        ["WT.PickUp"]         = ("À récupérer", "To pick up"),
        ["WT.TurnIn"]         = ("Complet, à rendre", "Complete, turn in"),
        ["WT.Stickers"]       = ("{0}/9 vignettes", "{0}/9 stickers"),
        ["Deliveries"]        = ("Livraisons spéciales", "Custom deliveries"),
        ["Remaining.One"]     = ("{0} restante", "{0} left"),
        ["Remaining.Many"]    = ("{0} restantes", "{0} left"),

        // Barre et infobulle
        ["AllDone"]           = ("Tout est fait", "All done"),
        ["TasksLeft.One"]     = ("{0} tâche restante", "{0} task left"),
        ["TasksLeft.Many"]    = ("{0} tâches restantes", "{0} tasks left"),
        ["DailyReset"]        = ("Reset quotidien : {0}", "Daily reset: {0}"),
        ["WeeklyReset"]       = ("Reset hebdomadaire : {0}", "Weekly reset: {0}"),
        ["GcReset"]           = ("Reset Grande Compagnie : {0}", "Grand Company reset: {0}"),
        ["TooltipHint"]       = ("Clic : liste · Clic droit : outil de mission", "Click: list · Right-click: Duty Finder"),

        // Menu
        ["AllDoneBravo"]      = ("Tout est fait, bravo !", "All done, well played!"),
        ["DailyHeader"]       = ("Quotidien · reset dans {0}", "Daily · reset in {0}"),
        ["WeeklyHeader"]      = ("Hebdomadaire · reset dans {0}", "Weekly · reset in {0}"),

        // Durées
        ["DaysHours"]         = ("{0} j {1} h", "{0}d {1}h"),
    };

    public static string T(string key)
    {
        if (!Table.TryGetValue(key, out var entry)) return key;
        return IsFrench ? entry.Fr : entry.En;
    }

    public static string T(string key, params object[] args) => string.Format(T(key), args);

    /// <summary>Choisit la clé « .One » ou « .Many » selon le nombre.</summary>
    public static string Plural(string key, int count) => T(count > 1 ? $"{key}.Many" : $"{key}.One", count);
}
