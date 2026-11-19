# MouseKeeper prototype

Open the project in Unity 6000.6.4f1 and play `Assets/Scenes/SampleScene.unity`.
The prototype starts automatically in a single loaded scene and uses placeholder
geometry if no configuration is provided.

Move with WASD relative to the isometric camera, aim with the mouse, fire with
left click, use the secondary attack with right click, and reload with R. Press E
near an item to pick it up and 1 or 2 to switch weapons. Weapons recharge over
time; reload fills the equipped weapon after a delay. A third weapon replaces and
drops the selected weapon. Clear rooms to open their exits, defeat the final
room's placeholder boss, and press Enter to restart after victory or death.

To configure a run, add a `GameRun` component to a scene object. Its room count,
room dimensions, mouse count, room prefab, mouse prefabs, weapon definitions,
and passive item definitions are configurable in the Inspector. The generated
floor, walls, and doors remain in place when using a room prefab; use the prefab
for decorations and obstacles within those bounds. Add `RoomLayout` to the room
prefab and assign hide spots to override the default corner hiding places.
Mouse prefabs need a `MouseEnemy` and its required `CharacterController`; their
visuals and outfits can differ. The final room uses a larger placeholder mouse.

Create weapon and passive assets through **Assets > Create > MouseKeeper**.
Subclass `WeaponDefinition` to override `PrimaryAttack`, `SecondaryAttack`,
`Reload`, or `Think`; each equipped or dropped weapon retains its own
`WeaponInstance` energy. Subclass `PassiveItemDefinition` for pickup, room,
death, mouse-kill, and damage hooks. Default stat bonuses are applied on pickup;
duplicate passive pickups stack. Custom ScriptableObject definitions are shared
assets, so per-instance mutable state should live outside the definition.

This is a placeholder prototype, not a finished production build. Mice use
straight-line movement rather than obstacle pathfinding, and the placeholder
boss uses the basic mouse behavior. Profile and tune enemy counts in the target
build before relying on hundred-mouse performance.
