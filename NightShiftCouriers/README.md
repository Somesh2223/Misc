# Night Shift Couriers

Unity 6 (URP) project. Design: see `GAME_PLAN.md`.

## Milestone 1 progress
- [x] Folder structure (plan section 8)
- [x] Player controller (walk, sprint, crouch, jump, swim, stamina)
- [ ] Physics carrying + fragile package
- [ ] Flood system
- [ ] Jal-Pishach
- [ ] Delivery loop

## Testing the player controller
1. Open this folder (`NightShiftCouriers`) in Unity Hub with Unity 6 (URP project; if you start from a blank URP template, copy `Assets/_Game` into it and make sure the Input System package is installed).
2. Menu: **Night Shift > Create Test_Player Scene** (creates `Assets/_Game/Scenes/Test_Player/Test_Player.unity`).
3. If Unity asks to restart for the Input System switch, accept, then open the scene.
4. Press Play.

| Action | Keys |
|---|---|
| Move / look | WASD / mouse |
| Sprint | Left Shift (drains stamina) |
| Crouch | Left Ctrl or C |
| Jump | Space (small stamina cost) |
| Swim up / down | Space / Ctrl while in deep water |
| Free the mouse | Esc (click to re-lock) |

Checklist:
- Sprint is faster than walk; stamina bar drains, turns red at 0 and you can't sprint until it recovers past 25.
- Crouch under the low ceiling to the east (x=6); you can't stand up while under it.
- Jump onto the 0.3 m / 0.6 m steps and the 1 m box.
- West puddle (0.5 m deep): you slow to a wade, can't sprint.
- East pool (1.9 m deep): you swim in your look direction, stamina drains while moving, you float to the surface when idle, steps on the pool's west side let you climb out.
- HUD bottom-left shows state, water depth, speed.

Tuning values are in the Inspector on `PlayerController` and `PlayerStamina`.

## Not built yet (from section 4.1)
Climbing, wetness, and networking (Milestone 2/3).
