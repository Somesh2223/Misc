# Night Shift Couriers — Game Design & Build Plan

> Hand this file to Claude Code. Put it in the root of the Unity project (you can also copy the "Rules for Claude Code" section into `CLAUDE.md`).

---

## 1. Pitch

2–4 friends work as night-shift couriers in a flooded, haunted Indian city during the monsoon. Every night the water rises, routes close, and creatures from folklore hunt anything that makes noise or light. Deliver enough packages before dawn to meet the quota — or the company "lets you go."

**Pillars**
1. **Co-op chaos** — physics, proximity voice, and fragile packages create funny moments.
2. **The flood is the clock** — no on-screen timer; rising water changes the map.
3. **Readable horror** — every monster has clear tells and a counter. Fear comes from teamwork failing, not unfair deaths.
4. **Nobody sits out** — dead players stay useful (and mischievous) as the city's radio.

**Rivals / inspirations:** Lethal Company, R.E.P.O., Content Warning, Peak, Phasmophobia, Deep Rock Galactic, Death Stranding.

---

## 2. Tech Stack

| Area | Choice |
|---|---|
| Engine | Unity 6 (LTS), URP |
| Language | C# |
| Networking | Netcode for GameObjects (NGO) |
| Steam / lobbies | Facepunch.Steamworks + Facepunch transport for NGO |
| Proximity voice | Dissonance Voice Chat (or Vivox) |
| Input | Unity Input System (new) |
| Physics | Unity built-in (PhysX) |
| Art | Low-poly asset packs (Synty-style) + custom retro/grain post-process shader |
| Animation | Mixamo + Unity Animator / Animation Rigging |
| Audio | FMOD or Unity Audio with mixer snapshots |
| Water | URP water shader + simple plane height (no fluid simulation) |
| Backend (post-MVP) | Supabase or Firebase for daily contract leaderboard + shared "left-behind objects" |

---

## 3. Core Loop

1. **Depot (lobby / hub):** buy items, pick up the night's orders, choose roles.
2. **Shift (~15–20 min):** travel through the flooded city on foot and over rooftops, using a rare raft as a shortcut if the crew earns one. Deliver packages to addresses.
3. **Survive:** avoid or counter monsters using tells and items.
4. **Dawn:** return to the depot extraction point. Earn money from deliveries.
5. **Quota check:** every 3 nights the company sets a money quota. Miss it = run over.
6. **Upgrade:** spend money on items and permanent crew perks.

---

## 4. Systems Specification

### 4.1 Player Controller
- First-person, physics-friendly (CharacterController or Rigidbody — pick one and stay consistent).
- Walk, sprint, crouch, jump, swim (in water above waist height).
- **Stamina** (shared by sprinting, climbing, swimming, carrying heavy items).
- **Climbing** (Peak-style): hold on to marked climbable surfaces (walls, pipes, drainpipes), stamina drains while holding. A teammate can grab your hand to pull you up.
- **Wetness** stat: being in water raises it, makes you slower and louder (sloshing), dries over time indoors.
- Networked: owner-authoritative movement with server validation.

### 4.2 Interaction & Physics Carrying (R.E.P.O.-style)
- Packages are physics objects.
- Grab with a "hold beam" (object follows a point in front of camera with spring force).
- **Package types:**
  - **Standard** — one person.
  - **Fragile** — has hit-points; damage from impacts reduces payout. Shows cracks.
  - **Heavy** — two players required (each grabs a handle; carry speed depends on both).
  - **Wet-sensitive** — loses value if submerged.
  - **Cursed** (see 4.9).
- Items can be thrown. Dropped packages float if light, sink if heavy.

### 4.3 Flood System (the clock)
- Global water level rises in **stages** across the shift (e.g. 6 stages = 6 in-game hours).
- Each stage: water plane moves up, some streets become too deep to wade (rooftops or a raft only), some doors/alleys become blocked, new rooftop routes matter.
- Warning signs before each rise: siren, rain intensifies, radio announcement.
- Monster strength/count scales with water stage.
- Final stage (dawn): extraction window. Players not at extraction are lost (lose carried items).
- Server-authoritative; clients interpolate water height.

### 4.4 Rafts (rare, earned — a helper, not the core)
There are **no boats** in the game. Water travel is a rare **shortcut that makes a shift easier**, not the main way to play. Most of a shift happens on foot: wading, climbing, rooftops, ropes, and planks.

**Design rule:** a crew with no raft must still be able to meet the quota. A raft only makes the shift faster and safer.

**Getting a raft (players have to work for it)**
- Each shift has only **1–2 raft spots** on the map (a hidden workbench, a flooded godown, a temple courtyard).
- A raft needs **3 key parts**, scattered in risky places:
  - **Floaters** (sealed plastic drums) — usually in monster areas, like the Brahmarakshas mansion or the drain canal.
  - **Platform** (a door or wooden pallet) — inside buildings, heavy, two-person carry.
  - **Lashing rope** — rare spawn, or bought at the depot for a high price.
- Bring all parts to a raft spot and build together (two players needed, takes several seconds, noisy — can attract monsters).
- Only **one raft per crew** at a time.

**Using a raft**
- Carries 2 players (a third tips it) and a few light packages; a heavy package needs a balanced load.
- Paddled with planks. Slow, wobbly, and splashing makes noise.
- Breaks after a few hits; parts scatter in the water and can be fished out and rebuilt.
- Still limited by the flood: debris, low bridges, and upper-floor or indoor deliveries mean it only shortens routes.
- Rafts don't carry over between shifts.
- Permanent unlock (later): "Sturdy Lashing" perk — rafts take one extra hit.

### 4.5 Items (shop)
| Item | Use |
|---|---|
| Flashlight | Light; attracts some monsters, repels others |
| Rope | Make a climbable line between two points; also needed to build rafts |
| Plank | Bridge small gaps between rooftops |
| Radio | Talk to teammates at long range (not proximity); dead players can hijack |
| Decoy (firecracker / bell) | Makes noise somewhere else |
| Salt pouch | Creates a ward circle some monsters cannot cross |
| Lemon-chilli charm (nimbu-mirchi) | Temporarily makes holder invisible to one monster type |
| Repair kit | Patch raft damage, partially repair fragile packages |
| Body cam | Records clips (see 4.11) — every player has one by default |
| Glow sticks | Mark paths; low light, doesn't attract monsters |

### 4.6 Monsters (5 types for MVP-plus, 3 required for MVP)

All monsters use a shared **AI state machine**: `Idle → Wander → Investigate (sound/light) → Hunt → Attack → Retreat/Cooldown`.
Shared senses: **hearing** (radius scaled by noise level, includes proximity voice volume), **sight** (cone + light level), **water sense** (ripples).
Each monster has **tells** (Phasmophobia-style clues) and a **counter**.

1. **The Jal-Pishach (Water Ghoul)** — *MVP*
   - Lives in deep water. Hunts anything that splashes or swims.
   - **Tells:** V-shaped ripples moving against the current; bubbles; wet slapping sounds.
   - **Behavior:** drags swimmers underwater; can tip rafts if they are loud.
   - **Counter:** stay out of water, go slow (crouch-wade), paddle quietly.

2. **The Chudail (Backwards-Feet Woman)** — *MVP*
   - Walks the streets and rooftops. Mimics teammates' voices (plays back recorded voice-chat snippets from nearby players).
   - **Tells:** footprints point the wrong way; anklet bells (ghungroo) faintly heard; mimicked voices have a slight echo.
   - **Behavior:** lures players away from the group by calling their name / repeating their lines, then attacks isolated players.
   - **Counter:** stay grouped; use radio code words; salt ward circle blocks her.

3. **The Vetala (Hanging Corpse Spirit)** — *MVP*
   - Hangs upside down from trees, wires, and balconies. Possesses carried packages.
   - **Tells:** swaying silhouettes; packages getting suddenly heavier; laughter from above.
   - **Behavior:** if you walk under it with a light on, it drops on you; it can latch onto a package and drain payout or make it scream (noise attracts others).
   - **Counter:** keep lights off near trees/wires; shake off by passing the package to a teammate; lemon-chilli charm hides you from it.

4. **The Brahmarakshas (Lurker in the Old House)** — *post-MVP*
   - Guards specific large buildings (old mansions, temples, libraries). Delivery addresses sometimes sit inside its territory.
   - **Tells:** chanting heard from inside; doors closing by themselves; candles lit.
   - **Behavior:** slow but relentless inside its building; cannot leave the building.
   - **Counter:** speed and route planning; doors can be held shut by two players; decoys work well on it.

5. **The Mud Crawler (Drain Thing)** — *post-MVP*
   - Lives in storm drains and manholes; grabs ankles of players standing still near grates.
   - **Tells:** gurgling drains; grates rattling; mud bubbles.
   - **Behavior:** fast ambush, then retreats into the drain network; pops up at another grate.
   - **Counter:** don't stand still near grates; a teammate can free a grabbed player by pulling (shared interaction).

**Spawning:** a director (like Left 4 Dead's AI Director, simple version) controls monster budget based on water stage, player stress (recent damage, noise), and time since last scare. Never spawn in direct sight of players.

### 4.7 Roles (Deep Rock Galactic-style, optional)
Players pick at depot; each role has one passive and one starting item. Roles help but are not required.
- **Navigator** — sees delivery markers on map from further; starts with radio.
- **Builder** — builds rafts faster, rafts take less damage; starts with repair kit.
- **Runner** — more stamina, faster climbing; starts with rope.
- **Lookout** — spots monster tells from further (highlight); starts with glow sticks.

### 4.8 Customers & Lore
- Each delivery has a customer and a short note ("For Mrs. Iyer, 3rd floor. Do NOT knock twice.").
- Some customers are normal; some are strange (rules to follow, odd requests, nobody answers).
- Lore unlocks over nights through the packages and notes (a journal at the depot fills up).
- Breaking a customer's rule can trigger an event (monster spawn, bonus pay, cursed package).

### 4.9 Cursed Packages ("Don't open it")
- Some packages whisper, move, or are warm.
- Players can choose to **open** them:
  - Good result: rare item, big money, lore reveal.
  - Bad result: monster released, flood stage jumps early, package becomes a screaming noise source.
- Results are random but weighted; the team must decide together (good for streams).

### 4.10 Death & Dead-Player Radio
- On death, player becomes a **Signal Ghost**.
- Signal Ghosts can: possess streetlights (flicker to guide or mislead), speak through radios and phone booths, ring a bell to distract a monster (cooldown), see monster positions.
- Living players can't tell if the ghost is helping or trolling — that's the fun.
- Optional revive: carry the dead player's "courier ID badge" to a shrine/depot to revive at reduced stamina.

### 4.11 Body Cam Clips (Content Warning-style)
- Every player's body cam records a rolling buffer (last ~20 seconds) of their view.
- Key moments (deaths, monster attacks, big drops, cursed package openings) are auto-flagged.
- After each shift: highlight reel screen shows top clips; players can save as MP4 (or GIF) to disk.
- Implementation: render texture buffer at low resolution + encoder plugin (e.g. FFmpeg via native plugin or Unity Recorder alternative for runtime). If runtime video is too hard, start with saving key-frame screenshots and stitch later.

### 4.12 Proximity Voice
- Dissonance with positional audio; volume falls off with distance.
- Voices are muffled underwater and through walls.
- Voice loudness counts as **noise** for monster hearing.
- Radio item = long range channel (with static).
- Voice snippets feed the Chudail's mimic system (store a few recent short clips per player locally on host; never upload).

### 4.13 Persistent Helpers (Death Stranding-style) — *post-MVP*
- Ropes, planks, and glow sticks placed by other crews (from other games) appear in your city at the same spots.
- Players can "like" a helpful object; most-liked objects stay longer.
- Needs backend (Supabase/Firebase): store object type, map position, likes, timestamp.

### 4.14 Daily Weird Contract — *post-MVP*
- One global seeded contract per day (same map seed, same packages, special modifier like "all packages are fragile").
- Leaderboard by money earned and crew size.
- Backend required.

### 4.15 Economy & Progression
- Money per package = base value × condition (damage, wetness) × speed bonus.
- Quota every 3 nights, increasing each cycle.
- Shop at depot sells items (4.5) and lashing rope for rafts.
- Permanent unlocks between runs: cosmetics (raincoats, helmets), new roles, journal lore.
- No battle pass, no paid currency.

### 4.16 City / Level Design
- One hand-built district for MVP: market street, residential lanes, temple square, old mansion, drainage canal, rooftops.
- Built with modular low-poly kit; designed in **height layers** so flood stages close lower routes progressively.
- Place 1–2 raft spots per shift and scatter raft parts in risky areas (monster territory, inside buildings).
- Delivery addresses chosen randomly each shift from a pool of marked doors.
- Later: procedural variation (which alleys are blocked, which buildings open).

### 4.17 Audio & Atmosphere
- Constant rain with intensity tied to flood stage.
- Distant sirens, temple bells, dogs barking, generators humming.
- Monsters each have a signature sound cue (the tells).
- Retro/grainy post-process (VHS-style) to hide rough art and add mood.

### 4.18 UI
- Minimal HUD: stamina bar, carried item, quota progress (small).
- Depot: shop, order board, role selection, journal.
- Pause/settings: audio, voice, graphics, keybinds, push-to-talk toggle.
- Post-shift: earnings summary + highlight clips.

---

## 5. Networking Notes
- Host-client model through Steam lobbies (no dedicated servers needed for MVP).
- Server-authoritative: flood level, monsters, package values, money, quota.
- Client-owned: own player movement (validated), camera, local UI.
- Physics objects: server simulates; clients interpolate. Grab ownership transfers on pickup.
- Late join only at depot (not mid-shift) for MVP.

---

## 6. Milestones

Each milestone ends with a playable build and a short test checklist.

### M1 — Single-player prototype (weeks 1–4)
- Player controller (walk, sprint, crouch, jump, swim, stamina)
- Physics carrying + fragile package
- Flood system (3 stages) on a greybox map
- Jal-Pishach with full AI state machine
- One delivery loop: pick up → deliver → get paid
- ✅ Test: Is moving packages through rising water fun alone?

### M2 — Multiplayer core (weeks 5–8)
- Steam lobby + NGO host/join
- Networked player, packages, flood, monster
- Proximity voice with noise → monster hearing
- Heavy (two-person) packages
- ✅ Test: 2–4 players can finish a full shift without desyncs.

### M3 — Content & variety (weeks 9–12)
- Chudail (voice mimic) and Vetala (package possession)
- Rafts: raft spots, 3 parts, two-person building, breaking
- Items: flashlight, rope, decoy, salt, lemon-chilli charm, repair kit, radio, glow sticks
- Climbing + pull-up assist
- Quota system + depot shop
- ✅ Test: Do players use tells and counters, or just run?

### M4 — Signature features (weeks 13–16)
- Dead-player Signal Ghost system
- Cursed packages
- Customers & notes + journal
- Body cam highlight reel (screenshots first, video later)
- Roles
- Art pass: replace greybox with low-poly city kit; retro post-process
- ✅ Test: Do players share clips and laugh? Record playtests.

### M5 — Polish & demo (weeks 17–24)
- Brahmarakshas and Mud Crawler
- AI Director for spawns
- Audio pass, UI polish, settings
- Steam page, trailer clips, Next Fest demo build
- Post-MVP backlog: persistent helpers, daily contract, more districts

---

## 7. Out of Scope (do not build)
- Open world
- PvP
- Crafting trees
- Battle pass / paid currency
- Realistic fluid simulation
- Dedicated servers (for now)

---

## 8. Rules for Claude Code

1. **Work one system at a time.** Finish, test, and commit before starting the next.
2. **Readable over clever.** Write simple, beginner-friendly C#: clear names, small methods, plain if/else and loops. Avoid heavy abstractions, reflection tricks, or deep inheritance.
3. **Folder structure:**
   ```
   Assets/_Game/
     Scripts/{Player, Packages, Flood, Rafts, Items, Monsters, AI, Network, UI, Audio, Economy, Lore}
     Prefabs/
     Scenes/{Depot, City_Greybox, Test_*}
     ScriptableObjects/{Items, Monsters, Packages, Customers}
   ```
4. **Data in ScriptableObjects.** Item stats, monster stats, package types, customer notes — all tweakable without code changes.
5. **Every system gets a test scene** (`Scenes/Test_Flood`, `Test_Monster_JalPishach`, etc.).
6. **Network from the start of M2.** After M2, every new feature must work in multiplayer before it's done.
7. **Server-authoritative** for anything that affects gameplay results (see section 5).
8. **Use events** (C# events or a simple event bus) for noise, deliveries, deaths, and flood stage changes, so monsters, UI, and audio can listen without tight coupling.
9. **Expose tuning values** in the Inspector with `[SerializeField]` and tooltips.
10. **Ask before adding a new package/plugin** from outside Unity's registry.
11. **Write a short summary** at the end of each task: what was built, how to test it, known issues.

---

## 9. First Prompt to Give Claude Code

> Read GAME_PLAN.md. Set up the Unity folder structure from section 8, then build Milestone 1 step by step, starting with the player controller (section 4.1) in a test scene. Stop after the player controller and tell me how to test it.
