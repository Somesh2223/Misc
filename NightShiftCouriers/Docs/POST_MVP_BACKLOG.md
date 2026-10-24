# Post-MVP backlog

Things that are planned but not built, roughly in the order they are worth doing. The plan sections they come
from are in brackets. Nothing here is needed for the demo.

## Must do before release
- **Playtests and tuning**: every milestone's playtest question is still open (see the README checklists).
  Tune monster speeds, flood timings, quota and the AI Director's budget from real sessions.
- **Art pass** [2, 4.16]: a low-poly city kit (Synty-style) to replace the greybox, couriers with animations
  (Mixamo), monster models, a water shader, rain particles.
- **Audio pass, part 2** [2]: replace the generated placeholder sounds with recorded/licensed ones, an Audio
  Mixer with snapshots (underwater, indoors, dead), voices muffled through walls [4.12], radio static.
- **Body cam video** [4.11]: stitch the saved PNG frames into MP4/GIF (FFmpeg) instead of loose frames.
- **Steam**: real App ID, achievements, rich presence ("Night 4 - 2 couriers alive"), invite through the overlay.
- **Phone booths** for Signal Ghosts [4.10].
- **Permanent unlocks** [4.15]: raincoats, helmets and new roles bought between runs.

## Persistent Helpers (Death Stranding-style) [4.13]
- Ropes, planks and glow sticks that other crews left behind appear in your city at the same spots.
- Players can "like" a helpful object; most-liked objects last longer.
- Needs a backend (Supabase or Firebase): object type, map position, likes, timestamp.
- Start small: upload only on a successful shift, download 10-20 objects at shift start, no accounts beyond
  the Steam ID.

## Daily Weird Contract [4.14]
- One seeded contract per day for everyone: same packages, same addresses, one modifier
  ("every package is fragile", "no flashlights", "the water rises twice as fast").
- Leaderboard by money earned and crew size.
- Same backend as Persistent Helpers.

## More districts [4.16]
- A second hand-built district (for example the docks or the old railway colony), chosen at the depot.
- Procedural variation inside a district: which alleys are blocked, which buildings are open, where the
  raft spots are.
- New monsters that fit each district.

## Smaller ideas
- Gamepad support for the menus (the controls already work on a gamepad; the menus are mouse-only).
- Spectator camera for dead players who do not want to be a ghost.
- Accessibility: subtitles for important sounds ("chanting from the haveli", "grate rattling"), colour-blind
  friendly markers, a "reduce flashing" option for thunder and flicker.
- Localisation (Hindi, Tamil, Bengali first).
