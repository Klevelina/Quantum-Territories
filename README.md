🌾 Quantum Agriculture: Sustainable Territories
Developers: Evelīna Šadurska & Klints Legranžs

Platform: PC / Web (Godot Engine)

📌 Project Overview
Quantum Agriculture is a turn-based strategy game inspired by "Risk," where players compete to cultivate teritories. 

Players must balance aggressive expansion with environmental responsibility. Over-farming and constant territorial disputes can destabilize the ecosystem, leading to a "Quantum Collapse" where fertile land turns into a permanent wasteland.

🧠 Core Concept: The Quantum Dimension
The game utilizes core quantum mechanics to represent environmental health:

Superposition (Contested States): Unclaimed or disputed territories exist in a state of potential. They are neither "thriving" nor "dead" until a player’s actions collapse the state.

Wavefunction Collapse (Resolution): When a territory is captured or a turn ends, the "Quantum State" resolves. Depending on resource investment and stability, the land resolves into one of two outcomes:

✅ Social Good: A thriving Farm (Cows, Crops, Infrastructure).

❌ Quantum Collapse: A permanent Wasteland (Destroyed Dirt) that neither player can use.

🌍 Thematic Focus & Social Good
The game serves as a metaphor for Environmental Stewardship:

Conflict & Over-extraction: Multiple players fighting for the same tile reduces "Global Stability."

Sustainable Cultivation: Successfully stabilizing a region into a farm increases the world's health.

Systemic Failure: If the Global Stability meter reaches zero, the entire archipelago collapses, and all players lose.

🎲 Game Mechanics
🔄 Turn Structure

Allocation: Players place resources to expand into adjacent islands or reinforce existing farms.

Observation: At the end of the round, contested regions "collapse" their quantum state.

Stability Check: The Global Stability meter is updated based on the number of farms vs. the amount of conflict.

⚙️ Player Actions

Cultivate (Expand): Move into an adjacent neutral or contested region.

Reinforce: Strengthen the "Social Good" of a controlled farm to prevent collapse.

Stabilize: Spend extra resources to ensure a contested region resolves as a Farm rather than a Wasteland.

⚖️ Resolution & Stability System

The 50/50 Risk: If a region is highly contested, it has a high probability of Quantum Collapse, replacing the grass and farm layers with Destroyed Dirt.

Global Loss: If players are too aggressive, the Stability Meter (0–20) hits 0, triggering a "Total System Collapse."

🛠️ Technical Implementation (Godot)
We have developed a custom engine logic to handle these transitions:

Memory-Reveal System: We use a "Blueprint" logic where hand-drawn agricultural layouts (Fences, Livestock, Crops) are stored in memory and only revealed when the territory state resolves to "Social Good."

Metadata Mapping: Using Godot’s Custom Data Layers, we have tagged every tile with a territory_id to synchronize the Grass, Farm, and Dirt layers.

Probabilistic State Engine: A GDScript-based randomizer handles the "Quantum Collapse" logic based on player resource input.

🚀 Development Roadmap
Phase 1 (Current): Prototype 6-region map with working "Reveal/Collapse" logic and Stability Meter.

Phase 2: Visual feedback for "Unstable" regions (shaking or color shifting) and improved UI.

Phase 3: Balancing the "Risk vs. Reward" for environmental stability and adding agricultural animations.
