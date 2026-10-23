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
| Skip to next flood stage (debug) | N |
| Restart after the shift ends | R |
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

## Known issues
- All code was written without a Unity editor available, so it has never been compiled or run. Expect to fix small compile or API errors on first open.
- Climbing and wetness (plan 4.1) are not built yet (M3 / later).
- Water is a flat trigger box; there is no water shader or underwater effect yet.
- Everything is single-player; networking starts in Milestone 2. Heavy two-person packages are an M2 feature.
- Sounds are not implemented, so the Jal-Pishach tells are visual only (ripples).
