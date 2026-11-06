# LootScanner

Find out what's in a room before you search it.

- **Room scan**: press **H** to reveal all the loot in the room you're in. Each item shows its name and value through walls for 12 seconds, and is added to the map for your whole team.
- **Loot checklist**: press **J** to hide or show it. It lists everything you've scanned, highest value first, and ticks items off once they reach the truck or extraction. Broken items are crossed out. It also shows how much loot is on the map and how much nobody has found yet.

## Who needs it
**Only you.** It works as host or guest, and other players don't need it. Items you scan do appear on their maps.

## Config (`gabe.lootscanner.cfg`, or r2modman's Config editor)
| Setting | Default | |
| --- | --- | --- |
| ScanKey | H | key that scans the room |
| ChecklistKey | J | key that hides or shows the checklist |
| LabelSeconds | 12 | how long item labels stay up |
| Cooldown | 3 | seconds between scans |
| ShowChecklist | true | show the checklist when a level starts |
