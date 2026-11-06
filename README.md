# LootScanner

Room scanner and loot checklist for R.E.P.O. BepInEx 5 mod, client-side.
Player docs: [thunderstore/README.md](thunderstore/README.md).

## How it works
`src/Scanner.cs` matches `ValuableDirector.valuableList` against the local player's rooms, using the rooms' `MapModule` so a scan covers the whole room on the map. It calls the game's `ValuableObject.Discover`, then draws labels and the checklist with IMGUI.

## Build
`.\build.ps1` → `dist\LootScanner-<version>.zip`. `build.cmd` also installs it into your r2modman profile.
