# Trailer capture and the Next Fest demo

## Capturing trailer shots
Everything is built in; no extra packages needed.

| Key | What it does |
|---|---|
| F9 | Free camera: leaves your courier where they stand and flies a separate camera |
| WASD / Q / E | Fly / down / up |
| Shift / Ctrl | Faster / slower |
| Mouse wheel | Change the fly speed |
| F1 | Hide every HUD element (and the free camera's label) |
| F10 | Retro / VHS look on or off |
| Esc > Graphics | Field of view, quality, FPS counter |

Tips
- Record with OBS (or Windows Game Bar) at 1080p60. Hide the HUD with F1 first.
- Use a second copy of the game (see "Two copies on one PC" in the README) as the "actor": play the courier in
  one window and fly the free camera in the other.
- For monster shots, run the test scenes (Night Shift > Create Test_Monster_... Scene); each monster is close by.
- The free camera keeps the retro look, so shots match gameplay. Turn it off (F10) for clean screenshots.
- Shot list for a 60-90 s trailer:
  1. Rain on the depot, the shift whistle, four couriers grabbing parcels (5 s)
  2. A fragile parcel thrown across a gap and caught (3 s)
  3. The siren, then the water climbing the market street (6 s)
  4. One tell + counter per monster: ripples, backwards footprints, a lit flashlight under the wire,
     candles in the haveli window and a door holding against a bash, a grate rattling (25 s)
  5. A courier dies; their ghost flickers a streetlight to show the way (6 s)
  6. Opening a cursed package (3 s)
  7. Paddling a raft past the rooftops at dawn (5 s)
  8. The results screen and a body cam clip that went badly (5 s)
  9. Title, "Wishlist now" (5 s)

## Building the Next Fest demo
1. **Night Shift > Create City_Greybox Scene** (so the scene and prefabs are current).
2. **Night Shift > Build > Windows Demo**. The build goes to `Builds/Demo/` next to the `Assets` folder.
3. The demo build:
   - lets a crew play **3 nights** (`BuildInfo.DemoNights`), then shows a thank-you screen in the depot with a
     Wishlist button (opens `BuildInfo.StorePageUrl`),
   - shows a small "DEMO" label in the top-right corner,
   - is otherwise the full game, so multiplayer, voice and settings all work.
4. To try the demo rules without building: **Night Shift > Build > Preview Demo Rules In Editor** (tick it,
   then Play; untick it when you are done).
5. **Night Shift > Build > Windows Full Game (testing)** makes a normal build in `Builds/Full/` for testing.

Before uploading to Steam
- [ ] Play all 3 demo nights with 2+ players over the internet.
- [ ] Check the settings survive a restart (they are saved in the registry via PlayerPrefs).
- [ ] Check the Wishlist button opens the right page.
- [ ] Upload the demo as its own Steam app (Steamworks > "Demo"), not as a branch of the main game.
