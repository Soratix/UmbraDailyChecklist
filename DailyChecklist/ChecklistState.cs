using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ContentRouletteSheet = Lumina.Excel.Sheets.ContentRoulette;

namespace Umbra.DailyChecklist;

/// <summary>Une roulette avec bonus quotidien.</summary>
public sealed record RouletteInfo(byte Id, string Name, int SortKey, bool DefaultTracked);

/// <summary>Horaires des resets (en UTC).</summary>
public static class Resets
{
    /// <summary>Reset quotidien : 15:00 UTC.</summary>
    public static DateTime NextDaily(DateTime nowUtc) => NextAtHour(nowUtc, 15);

    /// <summary>Reset de la Grande Compagnie (livraisons, approvisionnement) : 20:00 UTC.</summary>
    public static DateTime NextGrandCompany(DateTime nowUtc) => NextAtHour(nowUtc, 20);

    /// <summary>Reset hebdomadaire : mardi 08:00 UTC.</summary>
    public static DateTime NextWeekly(DateTime nowUtc)
    {
        var candidate = nowUtc.Date.AddHours(8);
        int days      = ((int)DayOfWeek.Tuesday - (int)candidate.DayOfWeek + 7) % 7;
        candidate = candidate.AddDays(days);

        return candidate <= nowUtc ? candidate.AddDays(7) : candidate;
    }

    private static DateTime NextAtHour(DateTime nowUtc, int hour)
    {
        var candidate = nowUtc.Date.AddHours(hour);
        return candidate <= nowUtc ? candidate.AddDays(1) : candidate;
    }

    public static string Format(TimeSpan span)
    {
        if (span < TimeSpan.Zero) span = TimeSpan.Zero;

        return span.TotalDays >= 1
            ? Strings.T("DaysHours", (int)span.TotalDays, span.Hours)
            : $"{(int)span.TotalHours}:{span.Minutes:00}:{span.Seconds:00}";
    }
}

public static class RouletteRepository
{
    // Roulettes suivies par défaut : gain de niveaux, avancés, épopée, expert, défis, supérieurs, alliance, raids normaux.
    private static readonly HashSet<uint> DefaultIds = [1, 2, 3, 5, 6, 8, 15, 17];

    private static List<RouletteInfo>? _roulettes;

    public static IReadOnlyList<RouletteInfo> Roulettes => _roulettes ??= Load(Framework.Service<IDataManager>().Excel);

    /// <summary>Roulettes qui ont un bonus quotidien (celles qui ont un index de complétion).</summary>
    public static List<RouletteInfo> Load(Lumina.Excel.ExcelModule excel)
    {
        List<RouletteInfo> result = [];

        foreach (var row in excel.GetSheet<ContentRouletteSheet>()) {
            if (row.CompletionArrayIndex < 0 || row.IsGoldSaucer) continue;

            string name = row.Category.ExtractText();
            if (string.IsNullOrWhiteSpace(name)) name = row.Name.ExtractText();
            if (string.IsNullOrWhiteSpace(name)) continue;

            result.Add(new((byte)row.RowId, name, row.SortKey, DefaultIds.Contains(row.RowId)));
        }

        return result.OrderBy(r => r.SortKey).ToList();
    }
}

/// <summary>Lecture de l'état du personnage (à appeler sur le thread du jeu).</summary>
public static unsafe class GameState
{
    public static bool IsAvailable =>
        Framework.Service<IClientState>().IsLoggedIn
        && PlayerState.Instance() != null
        && InstanceContent.Instance() != null
        && QuestManager.Instance() != null;

    public static bool IsRouletteComplete(byte id) => InstanceContent.Instance()->IsRouletteComplete(id);

    /// <summary>Allocations quotidiennes de tribus alliées restantes.</summary>
    public static int BeastTribeAllowances => (int)QuestManager.Instance()->GetBeastTribeAllowance();

    /// <summary>Allocations hebdomadaires de livraisons spéciales restantes.</summary>
    public static int CustomDeliveryAllowances
    {
        get {
            var manager = SatisfactionSupplyManager.Instance();
            return manager == null ? -1 : manager->GetRemainingAllowances();
        }
    }

    /// <summary>État du livre d'Idylle (Wondrous Tails).</summary>
    public static (bool HasJournal, bool IsExpired, int Stickers) WondrousTails
    {
        get {
            var ps = PlayerState.Instance();
            return (ps->HasWeeklyBingoJournal, ps->IsWeeklyBingoExpired(), ps->WeeklyBingoNumPlacedStickers);
        }
    }
}
