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
- [ ] Proximity voice chat itself (needs a voice plugin - see "Voice chat" below)
- [ ] Playtest with 2-4 real players (the M2 test: "can a crew finish a shift without desyncs?")

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
1. Open the `NightShiftCouriers` folder in Unity Hub with Unity 6 (URP). If you start from a blank URP template instead, copy `Assets/_Game` into it and install the Input System package.
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

## Voice chat (not done yet - needs your decision)
Voice **transmission** needs a plugin and neither can be added from this environment:
- **Dissonance Voice Chat** (Asset Store, paid): best proximity-voice support for Netcode; import it, then add the Dissonance comms object and a `DissonancePlayer` on the player prefab.
- **Vivox** (Unity Gaming Services, free tier): install `com.unity.services.vivox`, link the project to a UGS project, and use positional channels.
Everything on the game side is ready: `MicNoiseReporter` already turns loudness into monster-audible noise, and the NetworkPlayer position can feed the plugin's 3D positioning.

## Known issues
- All code was written without a Unity editor available, so it has never been compiled or run. Expect to fix small compile or API errors on first open.
- **Package versions are unverified**: the registry was not reachable while writing this. `com.unity.netcode.gameobjects` is pinned to 2.1.1; if Unity says that version does not exist, pick the newest 2.x in Package Manager. The Facepunch transport is pinned to a git commit and was written for NGO 1.x; if it does not compile against NGO 2.x, update it or tell me the errors.
- If the console shows duplicate network prefab warnings, delete the auto-generated `DefaultNetworkPrefabs` list (the game registers its prefabs in `GameNetworkConfig`).
- Network RPCs use the classic `[ServerRpc]` / `[ClientRpc]` attributes; NGO 2.x may show deprecation warnings for them (harmless).
- Remote players have no collider, so packages and monsters pass through other players for now.
- One test machine means no real latency: expect to tune the smoothing and the speed check once you play over the internet.
- Climbing and wetness (plan 4.1) are not built yet (M3 / later).
- Water is a flat trigger box; there is no water shader or underwater effect yet.
- Sounds are not implemented, so the Jal-Pishach tells are visual only (ripples).
