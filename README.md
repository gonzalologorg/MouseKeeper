# MouseKeeper prototype

Open the project in Unity 6000.6.4f1 and play `Assets/Scenes/Planning.unity`.
The build starts in planning. Rooms use the
modular wall, window, pillar, arch and door models configured in
`Assets/Resources/RoomArchitecture.asset`; their floors and some gameplay
objects use primitives with `Assets/Resources/PrimitiveMaterial.mat`.

Right-click a point to move the three agents into a small formation around it.
They choose targets, attack, reload, and collect affordable
pickups autonomously. With a melee weapon selected, an agent closes in and
backs away when the mouse gets too close, staying near attack range when possible.
Fleeing and player movement orders take precedence. When a room is clear,
closed doors to unvisited rooms pulse white; right-click one to enter.
Doors between visited cleared rooms remain open. Right-click a previously
visited cleared room in the world or on the minimap to travel through
connected cleared rooms. Each expedition has three floors, with five to eight
rooms randomly generated on each floor.
Right-click a weapon in the current room to order an available agent to collect
it, even if it is not an upgrade. Right-click it again before collection to
cycle through living agents who can afford it. The weapon is reserved for the
selected agent; a short delay allows cycling even when the agent is nearby.
After defeating the boss and clearing its room, right-click the vent to advance
even if other rooms remain; on the third floor, use the boss-room exit door.
On the first two floors the boss also drops one guaranteed reward: a health
vial reserved for the living agent with the lowest health percentage if any
survivor is below 50%, otherwise a random passive item. Normal room rewards
are separate. When the whole squad falls, press Enter to continue to planning;
the expedition is settled once before the message appears.
The HUD shows the percentage of rooms visited and cleared on the current floor,
as well as the percentage of floors completed in the expedition.
Use **1**, **2**, **3** or the bottom-right buttons to select a formation:
concentrate on one shared target, keep distance while firing, or attack each
agent's nearest enemy. The nearest-enemy formation is the default. Right-clicking
a rat switches to shared focus on that rat; hiding temporarily redirects the
squad to one visible target until it returns. Fleeing, rescues and explicit
movement or pickup orders retain priority over formation movement.

The HUD shows each agent's passive items as icons. Each passive definition can
provide its own `icon` sprite; otherwise the icons in
`Assets/Resources/HudIcons.png` are placeholders. The inhaler, spiked cover and
both balances also have dedicated defaults in `Assets/Resources/ItemIcons`;
assigned sprites override them. Hover an icon for its name. When an agent
approaches a named weapon or passive pickup, its icon appears beside the name.

Planning offers 15 distinct agents, including four free, modestly skilled recruits.
The roster shows agents whose individual fee is within the available funds;
missing paid agents remain visible but cannot be selected. The four free
recruits remain selectable even while counted as missing. Total listed fees plus
assigned cash must fit the player's funds, although fees are paid from expedition
proceeds afterward. Weapons with no available copies are hidden.
Select three distinct available agents, select a squad slot and then a weapon
from the inventory panel, and optionally allocate cash for purchases;
stars show health, movement speed and attack speed on a 1–5 scale.
Mouse Blasters are unlimited; choosing or retaining an unavailable weapon
automatically equips a Mouse Blaster instead. The random planning button selects
three available agents and random weapons within the inventory limits, and clears
cash allocations. Recovered equipment returns to the
inventory, including the equipped weapons of agents lost on the expedition.
Weapons left on the floor are not recovered. If all three agents carry Mouse
Blasters when room stock is generated, no Mouse Blaster is generated there.
The inventory, funds and agent availability are saved locally with PlayerPrefs.
If the game closes during an expedition, planning offers to restart it from
the beginning or abandon it; abandoning recovers the initial equipment but
marks the squad as missing. Cash assigned to an abandoned expedition is lost.
The planning scene uses UI Toolkit: edit `Assets/Resources/Planning.uxml` in
UI Builder, `Planning.uss` for styling, and `PlanningPanel.asset` for panel
settings. `PlanningAgentRow.uxml` and `PlanningWeaponCard.uxml` define the
repeated roster and weapon entries. Keep the named controls used by
`PlanningController`. Weapons appear as a wrapping grid of icon tiles showing
damage and range; hovering a tile displays its name. Select a squad slot to see its agent, weapons, and
upgrades in the right column. Agent assets can optionally supply a planning
portrait and description; otherwise a colored silhouette and a description
based on their stats are shown.

The full contract pays $50. A completely cleared floor has no deduction.
For a floor exited early, the cleared percentage divided by three is deducted
from the full contract: leaving at 60% deducts 20% of the full contract.
For example, floors exited at 60%, 80%, and 100% yield 53.33% of the contract,
rounded to $27. Unfinished floors during a defeat contribute nothing. Collected
cash carried by survivors (including unspent assigned cash) is added separately.
Living agents are paid
in squad order, up to the available proceeds: their listed fee multiplied by
`1 + greed / 10`, rounded up. Any remainder goes to the player's funds; the
unallocated funds do not cover a shortfall; free recruits take no fee.
Agents fallen at extraction or defeat count as missing. Each completed or
abandoned expedition recovers one previously missing agent, oldest first.
Six simultaneous missing agents end the campaign; the game-over screen offers
a fresh campaign. Completing all three floors awards each surviving squad agent
one individual upgrade point; defeat or abandonment awards none. Spend each
point on that agent's personality statistics (maximum 10); each
spent point also grants +10% base maximum health and +5% base movement speed.
Death immediately resets that agent's upgrades, including if revived during
the expedition. Unspent individual points are saved with the campaign. Shared
points from the previous version remain available to any selected agent; an
agent's own points are spent first. The bottom of planning explains each stat;
placeholder textures are in `Assets/Resources/StatIcons`.
In Unity, open **Window > MouseKeeper > Editor de escuadrón** to create and edit
agent ScriptableObject assets. Configure each asset's name, visual prefab or
placeholder color, health, movement and attack speed, personality and mission
fee, and whether they are a free recruit. Expedition weapons are assigned in planning. Add new assets to the `agents` array of
`Assets/Resources/ExpeditionCatalog.asset` to offer them in planning. The
catalog's weapon array controls the inventory and available loot. Asset names
must be unique and should not be renamed or removed once a campaign uses them;
reordering and adding assets preserves saved identities. Assets
can also be created via **Assets > Create > MouseKeeper > Agente**.

Debug shortcuts are available only while running in the Unity Editor, never in
a player build. In planning: **F1** resets the campaign, **F2** adds $20,
**F3** adds one of each finite weapon (the Mouse Blaster is unlimited), and
**F4** gives every agent five individual upgrade points. These changes are saved.
During an expedition: **F1** heals living agents, **F2/F3** add/subtract five
karma, **F4** clears the current wave, **F5** regenerates the current floor,
and **F6** downs a random living agent even if they have a revival passive.
Restarting a floor keeps the squad's current equipment, health, cash, karma and
fallen agents, and does not award floor credit.
Use the **ES / EN** button in planning to switch Spanish and English; the
selection is saved separately from campaign progress. Edit
`Assets/Resources/GameText.json` to translate UI text, item names and default
dialogue lines. Entries use their source text as `key`, an `english` translation
and, optionally, a `spanish` translation when the source is not Spanish. Keep
format placeholders such as `{0}` in each translation. Existing saved results
without structured localization data remain in their original language.
Each agent asset has a **Diálogos localizables** list. Assign a JSON `TextAsset`
and language name (`Spanish` or `English`); the game selects the chosen
language and falls back to `Spanish`. Default Spanish lines are translated
through `GameText.json` when no English dialogue file is assigned. The 15 default
agents have Spanish examples in `Assets/Data/Dialogues/Spanish`. Each JSON file
contains arrays named `roomCleared`, `injured`, `purchase`, `healed`,
`bossDefeated`, and `allyDied`. Every array needs nonempty, distinct lines of
at most ten words. Lines are selected without repetition per event and floor;
if a mandatory event exhausts its lines, selection starts another cycle without
immediately repeating the last line.

Each survivor has a 20% chance to comment when a combat room is cleared and a
50% chance after buying an item. Every actual heal is voiced, serious injury
(25% health or less) is voiced once per floor, one survivor comments on the
boss, and living teammates comment one and two seconds after an ally falls.
Lines appear above the agent with a typewriter effect and remain for three
seconds once fully written. Concurrent mandatory lines do not overwrite each
other. Picking up a weapon or passive always announces its display name using
the same presentation. These explicit chances and guaranteed events take precedence over the
general suggestion of about 30% chatter.

Emoji-style bubbles last two seconds: anger appears on eligible high-karma
teammates (above 7) when an agent with negative karma takes contested loot;
fury appears when an agent at 25% health or less has at least two mice within
three units; sadness marks a teammate's fall, and fear follows when two agents
are down. Happiness marks a boss defeated without any agent falling during the
expedition. Desire appears after an agent buys two or more items. States are
queued rather than replacing one another, and a continuing fury condition
does not repeatedly trigger until it ends and recurs.
Agent prefabs should contain only visuals, oriented forward along +Z and sized
for the agent's character controller; the game creates the controller and
behavior separately. The raid uses independent copies of their profiles.
Agents with movement speed below 4 receive bonus maximum health, scaling from
no bonus at speed 4 to 150% extra health at speed 2. The planner displays the
effective health and uses it for health stars; the asset's base health is unchanged.

Personality sliders run from 1 to 10: bravery lowers the health threshold for
fleeing, while agents still fire at enemies in range; accuracy reduces aim error,
with poor accuracy increasingly costly at long range; agility reduces fatigue and reload time;
distrust can make a fleeing agent shelter behind a teammate; and teamwork
helps determine which agent rescues an endangered ally. Pickup priority goes to the eligible
agent with the highest karma. A lower-karma agent may add 200% of their greed
(rounded down) to their claim once, allowing them to take an item ahead of the leader; they
cannot use that advantage again until their karma catches up with the highest
living teammate. Ties favor higher actual karma, then proximity.
Health costs one karma; money costs one, two above 10 karma, or three above 15
karma, based on the collector's karma before pickup. Passive items cost three;
weapon pickups do not change anyone's karma. Greed adds `floor(max(0, greed - 5) / 2)` to the collector's
karma cost. Each successful pickup gives every other living agent one karma.
When only one agent remains alive, that agent prioritizes floor money regardless
of karma and collecting money does not change their karma. The last survivor
also moves 25% faster, including during the existing open-door travel boost.
An agent cannot take a health vial if their health percentage is more than
25 percentage points above any living teammate's. Starting money and equipment
do not count. Karma lasts
for the expedition and is shown in the HUD. During combat, an agent without a
movement order can approach an ally with karma of -1 or less and push them
toward an enemy; higher distrust makes pushes stronger and more frequent.
Below -4 karma, each living teammate fires one damaging reprimand shot, subject
to movement orders, fear, weapon readiness and line of sight. They may fire
again if the offender's karma recovers to -4 and then falls below it again.
Below -7 karma, an agent cannot collect cash, but may still pick up other items.
Above 7 karma, an agent has priority for cash and seeks it before other pickups.
Above 3 karma, an agent has priority for weapons and will equip even a weaker
one, provided they can afford it. To prevent endless swaps, an agent does not
automatically pick up the same individual weapon twice in an expedition.
Fallen agents do not gain karma from pickups while incapacitated.
If at least two living agents are at or below 40% health and each was attacked
by a nearby, non-fleeing mouse within the last two seconds, the available agent
with the most teamwork approaches the most injured of them and pushes them
1.5 units away from their attacker. Rescuers wait five seconds before trying
again; movement and weapon pickup orders take priority.
A fleeing agent above 20% health
has a bravery/10 chance to obey each right-click order and overcome fear until
the fight ends or health becomes critical. At 20% or less, a frightened agent
continues fleeing, except that the last living agent always obeys a movement
order. The squad loses when every agent dies. A fallen agent returns with 50%
health when advancing to the next floor, retaining equipment, money and passives.
When the squad enters or is in a shop, an agent with at least $10 and greed
below 7 automatically spends $10 of their own money to revive a fallen teammate
at 50% health. Multiple revivals are possible while eligible agents can afford them.
Agents still fallen at the end of the expedition become missing instead.
Each agent has independent health, money, one weapon, and passives.
Their name appears above them, with a health ring on the floor: green above
60%, yellow above 30%, and red at 30% or below. A cyan silhouette reveals
agents hidden behind walls. The indicators disappear when an agent falls and
return when they are revived.
Rooms vary in width and depth, with windows and occasional pillars. Doors
close during combat and open after the room is cleared. The camera frames the
current room, with inactive rooms darkened, and the squad walks between them without a screen
fade. During door-to-door travel and in cleared rooms with an open door, living
agents move at the same speed: 150% of the fastest living agent's current
movement speed, without fatigue. Normal individual speed and fatigue apply in
combat and when no door is open. Combat mice appear
one at a time, preferring positions far from the
nearest agent; room-entry effects trigger after the entire wave has appeared. Weapons
and passive items show their name when approached. Pickups float and rotate, and
pickups and vents have a softly pulsing light.
Surviving agents' health, money, weapons, and passive items carry between floors. On clearing a combat room, the living agent with the lowest health percentage
recovers 5% of their maximum health; peaceful rooms do not trigger this healing.
Each floor adds one
enemy per combat room (including the boss room), before room-size scaling, and multiplies enemy health,
damage, and attack speed by 1.1, in addition to room-depth scaling. After the
whole squad dies, the expedition is settled and planning resumes.
Relative to the configured default room area, rooms at or below 80% of that
area spawn 60% of the usual enemies, rooms at or above 120% spawn 200%, and
other rooms spawn 100%. Counts are rounded to the nearest integer, with at least
one enemy for a nonzero wave. Boss rooms retain exactly one boss; additional
enemies can include rare variants. On floor two, each non-boss spawn has a 20%
chance for each of the jumping, tanky and speedy rats. Three 35% chances would
exceed 100%, so on floor three each variant has an equal one-third chance.
Jumping rats have 50% more health and damage and
leap every second, alternating between living agents when possible.
They aim at the agent's position when the leap starts, allowing the agent to
dodge. Tanky rats have double health, half speed, and 20% less attack damage
than ordinary rats. Speedy rats have twice the speed and half the health.
The usual floor and room-depth scaling still applies. Only normal rats flee
and recover after being hit; all special variants continue fighting.
Right-click a rat to focus the squad's attacks on it until it is defeated or
the squad leaves the room. Agents can still flee or help endangered teammates;
if the rat hides temporarily, they attack other visible targets until it returns.

The minimap shows the relative dimensions and positions of visited rooms and
their connected neighbours. Unvisited rooms
are dark gray, cleared rooms light gray, and the current room white. The boss
room is red whenever it is visible. A visited
room with an uncollected pickup is colored instead: cash green, health yellow,
weapons purple, and passives cyan. If several types remain, the single square
shows the highest priority: weapon, passive, health, then cash.

Rewards appear only after a combat room is cleared. Each room rolls once:
if any survivor is below 25% health there is a 13.33% chance of one health vial, otherwise it
produces cash (one pickup per defeated mouse). No weapons or passives drop from
combat rooms. Agents collect rewards autonomously; health
remains available to injured agents. Cash adds 1 money and health restores 25
health by default; their amounts are configurable on the vial prefabs.

Floors one and three have a peaceful treasure room; floor two has a peaceful
shop. The treasure room contains one free random weapon or passive.
The shop offers one health vial for $3, one random passive for $6, and one
random weapon for $9, with prices on the floor. Agents buy affordable items
with their own money. Stock persists when revisiting a room.

The default pickups include a sweeping melee weapon, a raytrace weapon and a
short-lived particle weapon, and a Pulse Blaster whose secondary attack damages
every mouse in the room. Raytrace weapons can be automatic or fire once per
click, with multiple spread bullets. Melee and particle secondary attacks are
not implemented yet. Passive pickups grant speed and health, damage the mice
on each room entry, revive the player once before being consumed, or retaliate
against all mice when the player takes damage. Duplicate
passives stack; only one revival is consumed per lethal hit. Each Room Shock
still applies its full damage independently, so enough copies can defeat even
scaled enemies on room entry.
Mouse maximum health is doubled, including the boss and configured prefabs.
Most weapon damage is reduced by 30% before rounding; gas and shotgun damage
use their stated values, and passive damage is unchanged.
Damage is rounded up to a whole number before applying it or invoking damage
hooks. Damage briefly flashes mice red and displays floating numbers for both mice and
the player. The numbers remain visible through walls.

The `GameRun` component configures base room dimensions, architecture asset,
mouse count and spawn interval, room prefab, mouse prefabs,
passive item definitions, and cash and health vial prefabs are configurable in
the Inspector. Without a vial prefab, a warning is logged and a placeholder cube
is used. The minimap
uses a `Minimap` component on a canvas panel; scenes without one can still run
without a map. The generated floor, modular walls, and doors remain in place when using a room
prefab; use the prefab for decorations and obstacles within the smallest room.
Architecture replacements must match the imported modules: walls and windows
are about 5.28 units wide, arches 3.9 units wide with a 2.86-unit opening, and
door leaves 1.3 units wide with their pivot at one edge.
Add `RoomLayout` to the room prefab and assign hide spots to override the default
corner hiding places.
Mouse prefabs need a `MouseEnemy` and its required `CharacterController`; their
visuals and outfits can differ. The final room uses a larger placeholder mouse.
An optional mouse Animator uses the float `Speed`, bool `Fleeing`, and triggers
`Attack` and `Death`. Its base layer must have a `die` state reached by `Death`;
the mouse is removed after that state finishes. The included rat controller
provides these states and transitions. Mice without an Animator disappear
immediately when defeated.
The included rat material uses URP Lit with emission enabled for its flash.
Other mouse materials with a base color use a tint, with an emission boost when
their emission keyword is enabled.

Create weapon and passive assets through **Assets > Create > MouseKeeper**.
Use the weapon and passive item submenus to create definitions. Weapon assets
are in `Assets/Data/Weapons`; add them to the weapons array of
`Assets/Resources/ExpeditionCatalog.asset` to include them in loot and recovered
inventory. New weapons have no copies until found. Each weapon has an assignable sprite
icon, display name, damage, energy cost, attack interval and other stats in the
Inspector. Subclass `WeaponDefinition` to override
`PrimaryAttack`, `SecondaryAttack`, `Reload`, `Think`, `OnDamage`, `OnDrop`,
or `OnPickup`; each equipped or dropped weapon retains its own
`WeaponInstance` energy. `OnDamage` runs for each mouse hit, including delayed
particles. For new weapon behavior, create a subclass, implement the relevant
attack hook, spend energy through `WeaponInstance.TrySpend`, then create and
register an asset in the catalog. `SpecialWeaponDefinition` demonstrates
gas, knockback, decoys, shotgun hits and melee control.
Subclass `PassiveItemDefinition` for pickup, room,
death, mouse-kill, damage, and attacker-aware hooks. Set `ConsumeOnPickup` for
instant effects; otherwise add the definition to `GameRun.passiveItems` in the
scene, or to `EnsureDefaultItems` for the default set. Default stat bonuses are applied on pickup;
duplicate passive pickups stack. Custom ScriptableObject definitions are shared
assets, so per-instance mutable state should live outside the definition.

The gas grenade throws a cloud for three seconds (20 damage per second within
its area). The low-energy blower pushes rats without damage. Cheese attracts
rats until they destroy its 50 health. The low-energy shotgun fires three
projectiles for 10 damage each, at most one hit per rat per shot. The whip
strikes one rat, makes it retreat for two seconds and has a three-second attack
cooldown. New models and icons can be assigned on their weapon assets.

The inhaler activates once per room at 25% health or less, triples movement
speed for three seconds and restores 30% maximum health over that time. The
spiked cover returns 25% of damage to the attacking rat. Karma balance resets
the squad's karma; health balance shares living agents' current health equally,
redistributing any excess when a member reaches maximum health. Both balances
are consumed immediately and do not revive fallen agents.

Each weapon definition also accepts a visual model prefab or imported FBX in
`Model Prefab`, plus local position, rotation and scale adjustments. The same
model is shown on the agent and on its floating pickup; leave the field empty
to use the placeholder. Alternatively assign `Model Mesh` and its material;
the mesh takes precedence over the prefab, and uses the primitive material
when no material is assigned. Models should contain visuals only: their
colliders are disabled by the game. Melee attacks swing the equipped model.

Scripts are grouped under `Assets/Scripts` into `Dungeon`, `Gameplay`, `Items`
(including `Passive`), `NPC`, `Presentation`, and `Weapons`. The `GameRun`
partial files separate dungeon construction, progression, combat, items, and UI.
Run `ExpeditionSessionTests`, `SquadBehaviorTests`, `SquadRecoveryTests` and `BalanceAndCameraTests` from the Unity Test
Runner's EditMode tab.

This is a placeholder prototype, not a finished production build. Mice use
straight-line movement, and agents use local obstacle avoidance rather than
full pathfinding; complex room prefabs can obstruct them. After surviving a hit,
a normal mouse flees and recovers up to half its maximum health over three seconds;
it then pursues the player and cannot flee again. The placeholder boss uses the
same behavior. Profile and tune enemy counts in the target build before relying
on hundred-mouse performance.
