# Night Shift Couriers

Unity 6 (URP) project. Design: see `GAME_PLAN.md`.

## Milestone 1 progress
- [x] Folder structure (plan section 8; plus `Scripts/Core` for the event bus and shared editor helpers)
- [x] Player controller (walk, sprint, crouch, jump, swim, stamina)
- [x] Physics carrying + fragile package (and wet-sensitive / heavy test packages)
- [x] Flood system (3 stages, siren warning, rising water)
- [x] Jal-Pishach with the full AI state machine
- [x] One delivery loop: pick up -> deliver -> get paid, with a quota
- [ ] Playtest on a real Unity install and tune (nothing here has been run yet - see "Known issues")

## Milestone 2 progress (multiplayer core)
- [x] Netcode for GameObjects + Facepunch Steam transport added to `Packages/manifest.json`
- [x] Host / join menu: direct IP (works now) and Steam lobbies (needs Steam running)
- [x] Networked player: owner-authoritative movement, server speed check with snap-back
- [x] Networked packages: server physics, server-side grab / drop / throw, synced condition
- [x] Heavy two-person package (needs two players to lift)
- [x] Networked flood (server timers, clients follow the water height) and delivery / money (server-authoritative)
- [x] Networked Jal-Pishach (server AI, targets the nearest player in the water, tells shown on every client)
- [x] Voice loudness -> monster noise (microphone level, toggle in the in-game panel)
- [x] Proximity voice chat with Vivox (needs the one-time setup in "Voice chat" below)
- [ ] Playtest with 2-4 real players (the M2 test: "can a crew finish a shift without desyncs?")

## Milestone 3 progress (content & variety)
- [x] Climbing (green surfaces, drainpipes, ropes) with stamina drain, mantling, and teammate pull-up
- [x] Wetness: wading/swimming soaks you; soaked = slower and louder; dries faster indoors
- [x] Items: flashlight, rope, plank, radio, decoy, salt pouch, lemon-chilli charm, repair kit, glow sticks (4 inventory slots)
- [x] Depot shop and quota: bank, quota every 3 nights, depot -> shift -> results loop, dawn extraction
- [x] Rafts: three parts, two-person build at a workbench, paddling, 2-rider limit, damage, breaking into parts
- [x] Chudail (voice mimic, backwards footprints, anklet bells; blocked by salt) and Vetala (drops on lit players, possesses packages)
- [ ] Playtest: "Do players use tells and counters, or just run?"

## Milestone 4 progress (signature features)
- [x] Signal Ghosts: dead players fly freely, flicker streetlights (E), ring a bell (right mouse), see monsters, talk only through radios
- [x] Courier ID badge: carry a fallen teammate's badge to a revive shrine (E) to bring them back weakened
- [x] Cursed packages: whisper, hop, feel warm; F opens one for a weighted good or bad outcome (Curse Table asset)
- [x] Customers & notes with rules (knock once, come alone, no lights, before the second rise); bonus for following, trouble for breaking
- [x] Journal (J): tonight's orders with notes, plus lore entries that unlock over nights (saved on the host's PC)
- [x] Body cam: rolling snapshots, auto-flagged moments, best clips shown after the shift, saved as PNG
- [x] Roles: Navigator, Builder, Runner, Lookout (pick in the side panel at the depot)
- [x] Retro / VHS camera look (F10 toggles)
- [ ] Low-poly city art kit (needs an asset pack you choose; the greybox stays for now)
- [ ] Playtest: "Do players share clips and laugh?"

## How multiplayer works (read this first)
- Every scene has a **NetworkManager** object. The single-player test scenes start hosting automatically, so you
  play alone as the host. `City_Greybox` shows a menu instead.
- Nothing networked is placed in the scene. When the shift starts, the **ShiftSpawner** (server) spawns the flood,
  the delivery manager, the packages and the monsters from markers in the scene (`PackageSpawnPoint`,
  `MonsterSpawnPoint`, `PlayerSpawnPoint`). Players are spawned by the NetworkManager.
- Prefabs live in `Assets/_Game/Prefabs`. Every **Night Shift > Create ... Scene** menu item rebuilds them first.
- Server decides: flood, money, orders, package physics and condition, monster AI, player health.
  Owners decide: their own movement (the server snaps them back if they move impossibly fast).
- Late join only before the shift starts: the host presses **START SHIFT** once the crew is in.

## How to open it
1. Open the `NightShiftCouriers` folder in Unity Hub with Unity 6.3 LTS (6000.3.27f1). If you start from a blank URP template instead, copy `Assets/_Game` into it and install the Input System package.
2. Every system has a menu item under **Night Shift** that builds its test scene. Run the one you want, then press Play.
3. If Unity asks to restart for the Input System switch, accept.

| Menu item | Scene | What to test |
|---|---|---|
| Create Test_Player Scene | `Test_Player` | Movement (see below) |
| Create Test_Packages Scene | `Test_Packages` | Grabbing, throwing, fragile damage, floating, wet packages |
| Create Test_Flood Scene | `Test_Flood` | Flood stages, siren warning, wading and swimming, rooftop |
| Create Test_Monster_JalPishach Scene | `Test_Monster_JalPishach` | The water monster, its tells and counters |
| Create City_Greybox Scene | `City_Greybox` | The full Milestone 1 loop |

## Controls
| Action | Keys |
|---|---|
| Move / look | WASD / mouse |
| Sprint | Left Shift (drains stamina) |
| Crouch | Left Ctrl or C |
| Jump | Space (small stamina cost) |
| Swim up / down | Space / Ctrl while in deep water |
| Grab / drop a package | E (aim at it, within 3 m) |
| Throw held package | Left mouse |
| Skip to next flood stage (debug, host only) | N |
| Restart the shift after it ends (host only) | R |
| Free the mouse | Esc (click to re-lock) |
| Grab a wall / rope (climb) | Space while facing a green climbable surface; W/S/A/D to move; Space jumps off; Ctrl lets go |
| Pull up a climbing teammate | Stand above them and press E while looking at them |
| Select item slot | 1 - 4 |
| Use selected item | Right mouse (on a raft: paddle; best with a plank) |
| Pick up item / use shop / raft workbench | E |
| Push-to-talk (if chosen in the side panel) | Hold V |
| Open a cursed package you hold | F |
| Knock on a customer's door | E on the brown door |
| Journal (orders, notes, lore) | J |
| Retro camera effect on/off | F10 |
| As a Signal Ghost | WASD fly, Space/Ctrl up/down, E flicker a streetlight, right mouse ring a bell |

## Test checklists

### Test_Player
- Sprint is faster than walk; stamina bar drains, turns red at 0 and you can't sprint until it recovers past 25.
- Crouch under the low ceiling to the east (x=6); you can't stand up while under it.
- Jump onto the 0.3 m / 0.6 m steps and the 1 m box.
- West puddle (0.5 m deep): you slow to a wade, can't sprint. East pool (1.9 m deep): you swim.

### Test_Packages
- Carrying a heavy crate slows you down. Throwing goes shorter the heavier it is.
- Drop or throw the fragile parcel hard onto the floor or the smash wall: it darkens as it takes damage.
- Light packages float in the pool; the heavy crate sinks. The "Documents" parcel darkens the longer it stays under water.

### Test_Flood
- Water starts hidden below the street, rises to 0.9 m (wading) then 2.2 m (swimming) every 25 s. A siren message shows 6 s before each rise.
- The stairs lead to a rooftop that stays dry in the deepest stage.

### Test_Monster_JalPishach
- It sleeps until the water is deep enough, and its state shows top-right.
- **Tell:** a white V of ripples moves across the surface when it is wandering or hunting.
- Swim in: it hunts you (swimmers are seen). It lunges (0.7 s wind-up), drags you down and does 35 damage. Three hits = respawn.
- **Counters:** the dry pier is safe. Crouch-wading the shallow ledge (west side) is quiet, so it may only investigate. Climbing out of the water makes it lose you.

### City_Greybox (the M1 question: is moving packages through rising water fun alone?)
- Spawn at the depot. Pick up the three packages from the table (E). The HUD shows where the held one goes.
- Cross the canal (z 0..10) by the bridge at x=-4 (safe) or by wading/swimming (fast and dangerous once the water is up).
- Drop a package inside a green zone to deliver it. Payout = base value x condition x speed bonus.
- Quota is 400. When all three are delivered or lost, the result shows; R restarts.

## Testing Milestone 3 (one menu item per system, then Play)
| Menu item | What to check |
|---|---|
| Create Test_Climbing Scene | Climb the green building and drainpipe; stamina drains while holding; you pull up onto the roof at the top. Pick up a rope (E), aim at the top edge of the grey ledge and right-click: climb the rope. Pick up a plank, stand at a rooftop edge and right-click to bridge the gap. With 2 players: one climbs, the other stands on top and presses E on them. |
| Create Test_Items Scene | Press E at the yellow shop terminal; you have 2000 to spend. Try every item: flashlight on/off and battery, decoy bangs, salt ring, charm message, repair kit on the fragile parcel after dropping it, glow sticks. |
| Create Test_Raft Scene | Carry the floaters (blue) and lashing rope into the zone in front of the workbench (the platform is already there). Press E at the bench: it builds in ~6 s (alone counts as enough builders). Stand on the raft, select a plank and right-click to paddle. Ram a post fast to damage it; repair it with the repair kit. A third person on it tips it over. The Jal-Pishach at the far end rams rafts. |
| Create Test_Monster_Chudail Scene | She wanders the streets (bells while moving, footprints pointing backwards). Alone, she attacks. Pour salt (right mouse) and stand in the ring: she cannot reach you. Throw a decoy to pull her away. With 2+ players: stay together and she waits at a distance and copies a teammate's voice (with an echo). |
| Create Test_Monster_Vetala Scene | Walk under the wire with the flashlight ON: it drops on you. Lights off: it ignores you. Carry a package near it: it may possess it (heavier, screams, loses value) - hand it to a teammate to break the spell. The charm hides you from it. |
| Create City_Greybox Scene | The full loop: Host, buy items in the depot, START SHIFT, deliver, build a raft, get back to the depot extraction before dawn, see the results, RETURN TO DEPOT, repeat for three nights to hit the quota. |

## Testing Milestone 4
**Night Shift > Create Test_Ghosts_And_Lore Scene**, then Play. You start in the depot with 1000 money.
- Side panel: change your **Role** (try Lookout / Navigator later in the shift). Press **START SHIFT**; your role item appears in your inventory.
- **J** opens the journal: tonight's order for "Test Door" and the customer's note.
- **Customer rule**: press E on the brown door *once*, then drop the package in the green zone: bonus pay. Knock twice to see what happens.
- **Cursed packages**: hold one of the three on the table: it whispers and the hint says it is warm. Press **F** to open it.
- **Signal Ghost**: walk into the yellow live-wire puddle. You die, drop your badge, and fly as a ghost: E near a streetlight flickers it, right mouse rings a bell, monsters are marked. With 2 players: the living one picks up the badge (E) and presses E on the orange shrine to revive you. Alone: the whole crew is dead, so the shift ends.
- **Body cam**: on the results screen the best clips play under the summary; press "Save highlight clips (PNG)" and open the folder it shows.
- **F10** toggles the retro look.

## Testing multiplayer

### Two copies on one PC (direct IP)
1. Run **Night Shift > Create City_Greybox Scene**, then **File > Build Settings**, add the scene, and build a standalone player
   (or use Unity's Multiplayer Play Mode / ParrelSync to open a second editor).
2. Copy 1: press Play, click **Host game**. Copy 2: type `127.0.0.1`, click **Join by IP**.
3. Host presses **START SHIFT**. Both players should see the packages on the depot table.
4. Checklist:
   - Each player sees the other as an orange capsule, moving smoothly.
   - One player picks up a package (E): the other sees it float and follow. Throwing works.
   - The Heavy Crate (grey, big): one player grabs it and nothing happens ("needs 2 players"). A second player grabs it and it lifts; both move slowly.
   - Deliveries add to the same money total on both screens.
   - The flood rises at the same time for both; the siren warning shows on both.
   - Swim in the canal: the ripples appear for both players, and the monster hunts whoever is nearest in the water.
   - Take three hits: you die and respawn at the depot (only you). Host presses R after the shift ends.
   - **Mic noise** button (right side panel): turn it on, talk near the canal while splashing, and watch the monster react.

### Over the internet (direct IP)
The host forwards UDP port 7777 on their router, and the joiner types the host's public IP.

### Steam lobbies
- Steam must be running. The test App ID is 480 (Valve's "Spacewar"); replace it with your own on `SteamLobbyService` once you have a Steam page.
- You need **two different Steam accounts on two machines** to test (one account cannot join its own lobby).
- Host: **Host Steam lobby**. Joiner: **Find Steam lobbies**, click the lobby. Friends can also use Steam's "Join Game".
- Steam support compiles only when the `com.community.netcode.transport.facepunch` package is installed and on Windows / Mac / Linux. Anywhere else the Steam buttons are replaced by a note and direct IP still works.

## Voice chat (Vivox)
Proximity voice uses **Vivox** (Unity Gaming Services). One positional channel per crew: voices get quieter with distance
and cut off beyond 32 m (tunable on `VoiceChatService`). Players join automatically when they spawn.

**One-time setup** (cannot be done from code, and no keys are stored in this repo):
1. Unity Hub / Editor: sign in, then **Edit > Project Settings > Services** and link the project to a Unity Cloud project.
2. In the Unity Cloud dashboard for that project, open **Vivox** and click **Get started** (free tier).
3. Press Play in `City_Greybox`. The right-hand panel shows `Voice: connected` when it works, or the reason it failed.

**Controls:** the **Mic** button in the right-hand panel cycles *OpenMic -> PushToTalk (hold V) -> Muted*.

**Voice makes noise:** while Vivox is connected, how loudly you speak (Vivox's own speech level) is reported to the server
as noise that monsters can hear. Tune `Vivox Level Scale` on `MicNoiseReporter` if normal speech is too quiet or too loud
for the monster. If Vivox is not set up, the old "Mic noise only" button still measures the raw microphone instead.

**Not done:** voices are not muffled underwater or through walls yet (plan 4.12), and the radio item (long range channel) is Milestone 3.

## Graphics: built-in vs URP
The project opens with Unity's built-in renderer, and the scene builders use the built-in *Standard* shader so nothing shows up pink.
To switch to URP later (plan section 2): **Create > Rendering > URP Asset (with Universal Renderer)**, assign it in
**Project Settings > Graphics** and **Project Settings > Quality**, delete `Assets/_Game/Prefabs/Materials`, then re-run the
**Night Shift > Create ... Scene** menu items. The builders pick the URP shader automatically once URP is active.

## Privacy note: the Chudail's voice mimic
When "Chudail voice mimic" is ON (side panel), short clips (1.5 s) of your voice are sent to the host while you talk, kept in
memory for that session only, and played back by the Chudail to your teammates. Nothing is saved to disk or sent anywhere else.
Turn it OFF in the side panel at any time.

## Body cam files
Saved clips go to `%USERPROFILE%\AppData\LocalLow\DefaultCompany\NightShiftCouriers\BodyCam\<date>` on Windows
(Unity's `Application.persistentDataPath`). They are PNG frames; stitching them into a video can come later.

## Known issues
- All code was written without a Unity editor available, so it has never been compiled or run. Expect to fix small compile or API errors on first open.
- **Package versions are unverified**: the registry was not reachable while writing this. Vivox is pinned to 16.7.0 and Authentication to 3.4.0 (the Vivox calls were written from its documented API without being able to read the docs page; the `Channel3DProperties` constructor argument order is the likeliest thing to need a fix). `com.unity.netcode.gameobjects` is pinned to 2.1.1; if Unity says that version does not exist, pick the newest 2.x in Package Manager. The Facepunch transport is pinned to a git commit and was written for NGO 1.x; if it does not compile against NGO 2.x, update it or tell me the errors.
- If the console shows duplicate network prefab warnings, delete the auto-generated `DefaultNetworkPrefabs` list (the game registers its prefabs in `GameNetworkConfig`).
- Network RPCs use the classic `[ServerRpc]` / `[ClientRpc]` attributes; NGO 2.x may show deprecation warnings for them (harmless).
- Sounds are generated in code (bells, laughter, screams, bangs) as placeholders until the audio pass (M5).
- The radio uses Vivox's "transmit to all channels" mode; it has no static effect yet, and dead players cannot hijack it yet (M4).
- The Chudail walks the navigation map built at shift start; it does not swim, so deep water does not stop it (streets just flood around her).
- Ghost "phone booths" (plan 4.10) are not built; ghosts speak through radios only.
- The retro effect works with Unity's built-in renderer (this project's current setup). If you switch to URP, it needs to be ported to a URP renderer feature.
- One test machine means no real latency: expect to tune the smoothing and the speed check once you play over the internet.
- Climbing and wetness (plan 4.1) are not built yet (M3 / later).
- Water is a flat trigger box; there is no water shader or underwater effect yet.
- Sounds are not implemented, so the Jal-Pishach tells are visual only (ripples).
