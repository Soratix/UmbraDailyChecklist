# Umbra – Quotidien & hebdo

Plugin pour [Umbra](https://github.com/una-xiv/umbra) (FINAL FANTASY XIV, via Dalamud) qui ajoute un widget de barre d'outils pour suivre ce qu'il reste à faire avant les resets.

## Fonctionnalités

- **Dans la barre :** le nombre de tâches restantes et le compte à rebours jusqu'au reset quotidien.
- **Infobulle :** les resets quotidien (15:00 UTC), hebdomadaire (mardi 08:00 UTC) et de Grande Compagnie (20:00 UTC).
- **Clic :** une checklist en deux sections.
  - **Quotidien :** les roulettes avec bonus pas encore faites, au choix parmi les 11 roulettes du jeu, et les allocations de tribus alliées restantes.
  - **Hebdomadaire :** le livre d'Idylle (Wondrous Tails), avec le nombre de vignettes posées, et les livraisons spéciales restantes.
- **Clic sur une roulette, ou clic droit sur le widget :** ouvre l'outil de mission.
- **Option :** masquer les tâches déjà faites.

L'état est lu directement dans le jeu : roulettes faites, allocations, livre d'Idylle. Rien n'est saisi à la main.

## Installation

1. Dans le jeu, ouvrez les **paramètres d'Umbra → Plugins**.
2. Ajoutez le dépôt GitHub de ce projet, ou un fichier local pointant vers `Umbra.DailyChecklist.dll`.
3. Passez la barre d'outils en mode édition et ajoutez le widget **« Quotidien & hebdo »**. Dans la recherche, vous pouvez aussi taper `daily`, `roulette` ou `reset`.
4. Choisissez les roulettes à suivre dans l'onglet **« Roulettes »** des réglages du widget.

## Compiler soi-même

Il faut le SDK .NET 10 et XIVLauncher avec Umbra installé : les références pointent vers `%AppData%\XIVLauncher`.

```powershell
dotnet build -c Release DailyChecklist\DailyChecklist.csproj
```

## Limite connue

Le jeu ne permet pas de distinguer un livre d'Idylle **pas encore récupéré** d'un livre **déjà rendu** cette semaine. Dans les deux cas, le widget affiche « À récupérer ».

## Licence

AGPL-3.0, comme Umbra.
