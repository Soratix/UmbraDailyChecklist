# Umbra – Daily & Weekly

[Français](README.md) · **English**

A toolbar widget for [Umbra](https://github.com/una-xiv/umbra) (FINAL FANTASY XIV, via Dalamud) that tracks what is left to do before the daily and weekly resets.

## Features

- **In the toolbar:** the number of tasks left and a countdown to the daily reset.
- **Tooltip:** the daily (15:00 UTC), weekly (Tuesday 08:00 UTC) and Grand Company (20:00 UTC) resets.
- **Click:** a checklist in two sections.
  - **Daily:** the duty roulettes whose bonus you have not claimed yet, chosen among the game's 11 roulettes, and your remaining Allied Society quest allowances.
  - **Weekly:** Wondrous Tails, with the number of stickers placed, and your remaining Custom Deliveries.
- **Click on a roulette, or right-click the widget:** opens the Duty Finder.
- **Option:** hide tasks that are already done.

Everything is read directly from the game: completed roulettes, allowances, Wondrous Tails. Nothing needs to be entered by hand.

The widget's interface is in English, or in French when Umbra (or, failing that, the game client) is set to French. Roulette names follow your client's language.

## Installation

1. In game, open **Umbra settings → Plugins**.
2. Add a GitHub repository: owner `Soratix`, repository `UmbraDailyChecklist`.
3. Switch the toolbar to edit mode and add the **Daily & Weekly** widget (listed as « Quotidien & hebdo / Daily & Weekly » in the widget list). You can also search for `daily`, `roulette` or `reset`.
4. Pick the roulettes to track in the **Roulettes** tab of the widget settings.

## Building it yourself

You need the .NET 10 SDK and XIVLauncher with Umbra installed: the references point to `%AppData%\XIVLauncher`.

```powershell
dotnet build -c Release DailyChecklist\DailyChecklist.csproj
```

## Known limitation

The game does not tell apart a Wondrous Tails journal that has **not been picked up yet** from one **already turned in** this week. In both cases the widget shows "To pick up".

## License

AGPL-3.0, like Umbra.
