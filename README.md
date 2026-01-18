# Small Ambitions ☕

[![Language](https://img.shields.io/badge/Language-C%23-1864ab?style=flat-square&logo=csharp&logoColor=white&labelColor=212529)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![Unity](https://img.shields.io/badge/Unity-6.3-1864ab?style=flat-square&logo=unity&logoColor=white&labelColor=212529)](https://unity.com/)

**A small office simulation where autonomous employees balance ambition, energy, and patience.**

Employees decide what to do on their own. They work when ambition demands it, grab coffee when energy is critically low, and wait patiently if someone else is hogging the coffee machine. When priorities shift, they abandon their current task and immediately replan.

No schedules. No scripts. No hard-coded routines. Just motives, decisions, and a surprising amount of IK blending.

---

## ▶️ How to Run

- Unity **6.3**
- Open the project
- Load either `MainOffice` or `StressTest`
- Press Play

All required packages are built-in Unity packages. No third-party nonsense to configure.

---

## ⭐️ Scenes

### `MainOffice`

- 2 autonomous NPCs
- 2 desks + computers
- 1 shared coffee machine

This is the main demo scene. Everything interesting happens here.

### `StressTest`

- 60 NPCs
- 60 desks / computers
- 6 shared coffee machines

Used to validate decision-making, contention handling, and performance under load. Also fun to watch.

---

## 🧠 Autonomy & Motives

NPCs are driven by **continuous, float-based motives** that decay over time and influence decision-making.

Implemented motives:

- **Ambition** - Need to work
- **Energy** - Need for coffee

### How Decisions Actually Work

NPCs select interactions using a weighted scoring system:

1. Each interaction defines how it affects motives (e.g., "Coffee restores +0.5 energy/second")
2. Each motive calculates its urgency (0 → 1) based on current value
3. Interactions that help urgent motives score higher
4. Highest-scoring interaction wins

This is intentionally **not threshold-based**. There's no "if energy < 20, get coffee" check. Motives slide continuously, priorities shift naturally, and behavior emerges from that.

### Interruption

When any motive drops below 10% (becomes critical, adjustable), the NPC immediately interrupts their current interaction. They don't just stop mid-action; they jump to the interaction's exit steps, clean up properly (IK weights blend back down, posture resets), and then re-evaluate what to do next.

This required careful state management. You can't just yank an NPC out of an interaction halfway through an IK blend without things exploding, so the exit steps handle all the cleanup before the NPC moves on.

---

## 🧩 Behavior Graphs (Unity 6)

High-level NPC logic is orchestrated using **Unity Behavior Graphs**.

The graph uses three custom action nodes written in C#:

- **AcquireAutonomyTarget** - Evaluates all available interactions, scores them against current motive urgency, handles slot reservation failures
- **NavigateToTarget** - Wraps NavMeshAgent with proper cleanup, converts agent to obstacle during interaction to prevent crowding
- **ExecuteInteraction** - Steps through interaction phases, blends IK weights, applies motive modifiers, handles interruption signals

I use Behavior Graphs for **orchestration**, not as a dumping ground for logic. The graph shows the flow; the code does the work.

---

## 🦾 Animation & IK

Uses **Unity Animation Rigging** with multiple IK chains running simultaneously:

- **Two Bone IK Constraint** for both hands (independent control)
- **Multi-Position Constraint** for pelvis (seated posture)

### Per-Step IK Control

Each interaction step defines target IK weights and blend durations. Weights default to 0 (natural animation), blend up during specific steps, and reset cleanly on exit.

Once the NPC navigates to the coffee machine (handled by `NavigateToTarget`), here's what happens:

1. **Reach for cup** - Right hand blends from 0 → 1 over 0.5s, locking smoothly to the cup's position
2. **Grab cup** - Cup parents to the IK root, drinking animation plays while right hand blends back down (1 → 0 over 0.5s) so the animation takes over
3. **Wait** - Step duration matches the animation length. Nothing fancy, just letting the animation do its thing
4. **Move cup back** - Right hand blends back up (0 → 1 over 0.5s) to return the cup precisely
5. **Place cup** - Detach from IK root and snap back to original position (one designer-friendly checkbox)
6. **Resume** - Right hand blends down (1 → 0 over 0.5s), returning to natural animation

The trick is the blend choreography. IK takes control when precision matters (reaching, placing), then hands control back to the animation when the motion needs to feel natural (drinking). Getting this to work smoothly while also handling mid-interaction interrupts was... an experience.

_Oh, and **scaling matters**. I'm using Kenney assets, and their sizes are all over the place. So I had to scale the character, which caused all sorts of fun bugs to happen. Turns out IK rigs don't love it when you mess with scale. Who knew?_

---

## 🔧 Interaction System

Interactions are entirely data-driven and defined as ScriptableObjects.

This was a deliberate choice:

- Designer-friendly authoring (no code changes to add new interactions)
- Runtime-safe data
- Easy iteration

Each interaction consists of:

- **Start / Loop / Exit** step lists
- Per-step one-shot animation clips (played on specific layers)
- IK weight targets and blend durations
- Continuous motive rate modifiers (e.g., "Restores +0.5 energy/second")
- Required primary and optional ambient slots

### Smart Objects & Slots

Smart Objects expose:

- Supported interactions
- Typed slots (stand position, hands, pelvis, etc.)
- Slot policies (`SingleUse`, `MultiUse`)

Slots are reserved **before navigation** to prevent contention. If two NPCs want the same coffee machine, one reserves it, and the other picks something else. If execution fails for any reason, slots are released and the NPC re-evaluates immediately.

---

## 🚶 Navigation & Multi-Agent Behavior

NPCs use `NavMeshAgent` for pathfinding.

While interacting:

- Movement is locked
- The agent temporarily becomes a `NavMeshObstacle`
- Other NPCs route around them naturally

This prevents crowding and deadlocks. NPCs don't pile up at the coffee machine or clip through each other at desks. Or NPCs standing in front of your computer screen forever because they got stuck...

---

## 🧪 Debugging & Profiling

An **editor-only debug overlay** (visible in Scene view, not Game view) allows inspecting selected NPCs in real-time:

- Live motive values and urgency
- Interaction candidates and scores
- Current interaction and phase
- Reserved Smart Objects

The overlay only renders for NPCs selected in the Hierarchy. It's a development tool, not part of the runtime UI.

### Performance

During stress testing with 60 NPCs, I noticed short frame spikes caused by synchronized decision-making. Every NPC was re-evaluating at the same time, which tanked the frame rate for a few milliseconds.

I fixed this by **staggering and budgeting AI decision evaluation** across frames. Verified the fix using the Unity Profiler. Besides improving performance, this also reduced behavioral synchrony and made agents feel more natural. NPCs don't all decide to get coffee at the exact same moment anymore.

---

## 💡 Design Decisions I'm Happy With

**Behavior Graphs as Orchestration, Not Logic**  
The graph is deliberately simple. Complex decisions live in testable C# nodes, not tangled graph spaghetti. This keeps the graph readable and the code debuggable.

**Continuous Motives Over Thresholds**  
NPCs continuously evaluate urgency and naturally prioritize what's needed most. Interruption only happens when a motive becomes critical (drops below 10%), but the scoring system is always working in the background.

**Data-Driven Interactions**  
Designers can author new interactions without touching code. This was critical for rapid iteration. Want to add a "read newspaper" interaction? Create a ScriptableObject, define the steps, done.

**Interruption With Clean Exits**  
NPCs respond to critical needs immediately, but they don't just vanish from what they're doing. They execute exit steps to clean up state properly before moving on.

---

## 🛠️ Development Process

Built over a few weeks using:

- **Bezi AI** - Unity-specific help, debugging those annoying bugs that take hours to find just to realize you forgot to uncheck one setting
- **Cursor** - Used throughout the project for AI-assisted coding, though I ran out of free tokens pretty quickly. Missed the tooling you get with proper IDEs like Visual Studio or Rider (syntax highlighting, refactoring tools, that sort of thing)
- **GitHub Copilot** - Autocomplete and code suggestions, used it when I ran out of free tokens for Cursor. They both have a similar feature set
- **ChatGPT** - Planning the interaction system architecture (I adapted almost everything, but it helped plant the seed)

AI tools were genuinely helpful throughout this project. Bezi was especially valuable because it's specifically built for Unity. It helped me find bugs that would've taken hours to track down manually, like that one time I spent way too long debugging IK issues only to realize the problem was asset scaling. Good times.

---

## 🎯 What I Learned

This project was built to strengthen a few specific areas:

**Unity Behavior Graphs** - I'd never used them before. Now I understand how to use them effectively as an orchestration layer instead of cramming all logic into visual scripting hell. Keeping it simple with just three custom nodes made the system way more maintainable.

**IK & Animation Blending** - Coordinating multiple IK chains with animation layers and handling interruption cleanly turned out to be way more complex than I expected. I spent more time than I'd like to admit debugging why hands would snap to weird positions during blend transitions. The Multi-Position Constraint for seated posture was particularly finicky. Also, turns out scaling your character model breaks IK in creative ways. Thanks, Kenney assets.

**Profiling & Optimization** - The synchronized decision-making issue taught me to actually use the Unity Profiler properly instead of just guessing where performance problems are. Staggering AI evaluation was a simple fix once I identified the spike.

**AI-Assisted Development** - I used Bezi, Cursor, and Copilot daily to learn what tools are out there and how they compare. Bezi was a genuine surprise; it's built specifically for Unity and worked incredibly well for tracking down obscure bugs. Cursor has excellent project-wide context understanding, which is really valuable when you need that broader view that ChatGPT can't provide. Copilot has similar context awareness of your entire solution and integrates better with my existing IDE workflow, though it's a bit slower than Cursor. Each tool has its place depending on what you need.

---

That's it. The project is small and focused, but it works well and demonstrates the core concepts I wanted to learn.
