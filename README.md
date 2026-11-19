# MouseKeeper prototype

Open the project in Unity 6000.6.4f1 and play `Assets/Scenes/SampleScene.unity`.
The prototype starts automatically in a single loaded scene and uses placeholder
geometry if no configuration is provided.

Move with WASD relative to the isometric camera, aim with the mouse, fire with
left click, use the secondary attack with right click, and reload with R. Press E
near an item to pick it up and 1 or 2 to switch weapons. Weapons recharge over
time; reload fills the equipped weapon after a delay. A third weapon replaces and
drops the selected weapon. Clear rooms to open their exits. Defeating the final
room's placeholder boss opens its exits and creates a vent; stand inside it and
press E to start the next level. You can explore previous rooms before using it.
Your health, money, weapons, and passive items carry over. Each level adds one
enemy per combat room (including the boss room) and multiplies enemy health,
damage, and attack speed by 1.1. Enemy health also gains 10% of its base per
previously cleared combat room, in addition to room-depth scaling. After death,
Enter starts a fresh run.

The minimap shows visited rooms and their connected neighbours. Unvisited rooms
are dark gray, cleared rooms light gray, and the current room white. The boss
room is red whenever it is visible. A visited
room with an uncollected pickup is colored instead: cash green, health yellow,
weapons purple, and passives cyan. If several types remain, the single square
shows the highest priority: weapon, passive, health, then cash.

Rewards appear only after a combat room is cleared. Each room rolls once:
below 25% health there is a 13.33% chance of one health vial, otherwise it
produces cash (one pickup per defeated mouse). No weapons or passives drop from
combat rooms. Enemy cash and health pickups are collected on contact; health
remains available if you are full. Cash adds 1 money and health restores 25
health by default; their amounts are configurable on the vial prefabs.

Each level with at least four rooms has one peaceful shop and one peaceful
treasure room. The treasure room contains one free random weapon or passive.
The shop offers one health vial for $4, one random passive for $8, and one
random weapon for $12, with prices on the floor. Press E near an item to buy it;
shop health vials do not auto-collect. Stock persists when revisiting a room.

The default pickups include a sweeping melee weapon, a raytrace weapon and a
short-lived particle weapon. Raytrace weapons can be automatic or fire once per
click, with multiple spread bullets. Melee and particle secondary attacks are
not implemented yet. Passive pickups grant speed and health, damage the mice
on each room entry, or revive the player once before being consumed. Duplicate
passives stack; only one revival is consumed per lethal hit. Each Room Shock
still applies its full damage independently, so enough copies can defeat even
scaled enemies on room entry.
Damage briefly flashes mice red and displays floating numbers for both mice and
the player. The numbers remain visible through walls.

To configure a run, add a `GameRun` component to a scene object. Its room count,
room dimensions, mouse count, room prefab, mouse prefabs, weapon definitions,
passive item definitions, and cash and health vial prefabs are configurable in
the Inspector. Without a vial prefab, a warning is logged and a placeholder cube
is used. The minimap
uses a `Minimap` component on a canvas panel; scenes without one can still run
without a map. The generated floor, walls, and doors remain in place when using
a room prefab; use the prefab for decorations and obstacles within those bounds.
Add `RoomLayout` to the room prefab and assign hide spots to override the default
corner hiding places.
Mouse prefabs need a `MouseEnemy` and its required `CharacterController`; their
visuals and outfits can differ. The final room uses a larger placeholder mouse.
An optional mouse Animator uses the float `Speed`, bool `Fleeing`, and triggers
`Attack` and `Death`. Its base layer must have a `die` state reached by `Death`;
the mouse is removed after that state finishes. The included rat controller
provides these states and transitions. Mice without an Animator disappear
immediately when defeated.
The included rat material uses the `MouseKeeper/Rat Hit` shader for its flash;
other mouse materials with a base color use a tint instead.

Create weapon and passive assets through **Assets > Create > MouseKeeper**.
Use the weapon and passive item submenus to create the specific placeholder
types. Weapon damage, energy cost, attack interval and other fields are
configurable in the Inspector. Subclass `WeaponDefinition` to override
`PrimaryAttack`, `SecondaryAttack`, `Reload`, `Think`, `OnDamage`, `OnDrop`,
or `OnPickup`; each equipped or dropped weapon retains its own
`WeaponInstance` energy. `OnDamage` runs for each mouse hit, including delayed
particles. Subclass `PassiveItemDefinition` for pickup, room,
death, mouse-kill, and damage hooks. Default stat bonuses are applied on pickup;
duplicate passive pickups stack. Custom ScriptableObject definitions are shared
assets, so per-instance mutable state should live outside the definition.

This is a placeholder prototype, not a finished production build. Mice use
straight-line movement rather than obstacle pathfinding. After surviving a hit,
a mouse flees and recovers up to half its maximum health over three seconds;
it then pursues the player and cannot flee again. The placeholder boss uses the
same behavior. Profile and tune enemy counts in the target build before relying
on hundred-mouse performance.
