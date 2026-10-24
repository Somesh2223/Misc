# Steam store page (draft)

A first draft of the store page text and the assets it needs. Edit freely; Steam's limits are noted in brackets.

## Name
**Night Shift Couriers**

## Short description [max 300 characters]
The monsoon is here, the city is flooding, and the packages still need delivering. Work the night shift with
2-4 friends: carry fragile parcels through rising water, read the tells of creatures from Indian folklore,
and get back to the depot before dawn. Miss the quota and the company lets you go.

## About this game
**Deliver before dawn. Don't open the packages.**

Night Shift Couriers is a co-op horror game for 2-4 players (playable solo). Every night the water rises,
streets close, and something in the dark is listening for noise and light.

- **The flood is the clock.** There is no timer on screen. Sirens warn you, then the water climbs: the market
  street becomes a river, ground floors fill, and the only way across is a rooftop, a rope, or a raft you
  built yourself.
- **Physics packages, proximity voice.** Every parcel is a real object. Fragile ones break, wet-sensitive ones
  soak through, heavy ones need two of you. Your voice carries through the street, and monsters hear it too.
- **Monsters you can read.** Each creature has clear tells and a counter. The Jal-Pishach ripples under the
  water. The Chudail's footprints point the wrong way, and she will call you in your friend's voice. The
  Vetala hangs from the wires and drops on anyone carrying a light. The Brahmarakshas chants inside the old
  haveli and has to break every door you close behind you. The Mud Crawler waits under the grates for
  whoever stands still.
- **Nobody sits out.** Die and you become a Signal Ghost: flicker streetlights to guide your crew (or mislead
  them), ring bells to distract monsters, and talk only through their radios. Carry a fallen courier's badge
  to a shrine to bring them back.
- **Strange customers.** "Knock once. Do NOT knock twice." Follow their rules for a bonus, break them and find
  out. Some packages whisper. Some are warm. You can open them, if the crew agrees.
- **Body cam.** Your best and worst moments are flagged automatically and shown after every shift.

## Tags (pick up to 20, most important first)
Co-op, Online Co-Op, Horror, Multiplayer, Physics, Funny, Survival Horror, Proximity Voice Chat (if offered),
Atmospheric, Indie, First-Person, Psychological Horror, Cute (no), Exploration, Delivery, Retro, Lovecraftian (no),
Mythology, Supernatural, Dark Humor, Action, Simulation.

## System requirements (estimate; confirm after testing on a low-end PC)
| | Minimum | Recommended |
|---|---|---|
| OS | Windows 10 64-bit | Windows 10/11 64-bit |
| CPU | 4 cores, 2.5 GHz | 6 cores, 3.0 GHz |
| Memory | 8 GB RAM | 16 GB RAM |
| Graphics | GTX 960 / RX 470, 2 GB | GTX 1660 / RX 5600, 6 GB |
| Storage | 2 GB | 2 GB |
| Network | Broadband (online co-op) | |

## Assets checklist
| Asset | Size | Notes |
|---|---|---|
| Header capsule | 920 x 430 | Logo over a flooded street at night, one courier holding a parcel above the water |
| Small capsule | 462 x 174 | Logo must read at this size |
| Main capsule | 1232 x 706 | |
| Vertical capsule | 748 x 896 | |
| Library capsule | 600 x 900 | |
| Library hero | 3840 x 1240 | No logo |
| Library logo | 1280 wide (PNG, transparent) | |
| Page background | 1438 x 810 (optional) | |
| Screenshots | 1920 x 1080, at least 5 | Use the free camera (F9) with the HUD hidden (F1). One per pillar: flood, carrying, each monster, ghost, results screen |
| Trailer | 1080p, 60-90 s | See `TRAILER_AND_DEMO.md` |

## Content survey notes
- Frequent violence or gore: no. Monsters attack; no blood, no dismemberment.
- Horror themes and jump scares: yes (say so in the description).
- User-generated content: voice chat between players (proximity, Vivox). Mention the Chudail's voice mimic:
  recent voice snippets are kept in memory on the host's PC only, never uploaded.

## Before "Coming Soon"
- [ ] Replace `BuildInfo.StorePageUrl` with the real store link (the demo's Wishlist button opens it).
- [ ] Set the Steam App ID in `SteamLobbyService` (it uses 480, the Spacewar test app, until then).
- [ ] Company name in Project Settings > Player (it decides where saves and body cam files go).
