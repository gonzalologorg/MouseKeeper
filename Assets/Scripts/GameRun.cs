using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MouseKeeper
{
    public sealed class GameRun : MonoBehaviour
    {
        public GameObject roomPrefab;
        public MouseEnemy[] mousePrefabs;
        public WeaponDefinition[] weapons;
        public PassiveItemDefinition[] passiveItems;
        public GameObject cashVialPrefab;
        public GameObject healthVialPrefab;
        [Min(2)] public int roomCount = 9;
        [Min(0)] public int micePerRoom = 5;
        public float roomWidth = 18f;
        public float roomDepth = 14f;

        public bool Finished { get; private set; }
        public Transform CurrentRoom => currentRoom != null ? currentRoom.root.transform : null;
        readonly List<Room> rooms = new List<Room>();
        readonly List<Object> generatedAssets = new List<Object>();
        readonly List<Object> levelAssets = new List<Object>();
        static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        Room currentRoom;
        Room bossRoom;
        PlayerController player;
        Camera viewCamera;
        Minimap minimap;
        DamageIndicators damageIndicators;
        float transitionDelay;
        string message;
        float messageUntil;
        int clearedCombatRooms;
        int levelIndex;

        enum RoomKind { Combat, Shop, Treasure }

        sealed class Room
        {
            public Vector2Int position;
            public RoomKind kind;
            public readonly Room[] neighbours = new Room[4];
            public readonly GameObject[] doors = new GameObject[4];
            public readonly List<Vector3> defeatPositions = new List<Vector3>();
            public GameObject root;
            public Collider floor;
            public Collider vent;
            public MouseEnemy boss;
            public bool bossDefeated;
            public int enemies;
            public bool visited;
            public bool cleared;
            public int depth;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterBootstrap()
        {
            SceneManager.sceneLoaded -= Bootstrap;
            SceneManager.sceneLoaded += Bootstrap;
        }

        static void Bootstrap(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single || FindFirstObjectByType<GameRun>() != null) return;
            new GameObject("MouseKeeper Run").AddComponent<GameRun>();
        }

        void Start()
        {
            roomWidth = Mathf.Max(8f, roomWidth);
            roomDepth = Mathf.Max(8f, roomDepth);
            viewCamera = Camera.main;
            if (viewCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                viewCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }
            viewCamera.orthographic = true;
            viewCamera.orthographicSize = 11f;
            viewCamera.transform.rotation = Quaternion.Euler(50f, 45f, 0f);
            damageIndicators = gameObject.AddComponent<DamageIndicators>();
            damageIndicators.Initialize(viewCamera);
            CreatePlayer();
            EnsureDefaultItems();
            minimap = FindAnyObjectByType<Minimap>();
            GenerateMaze();
            EnterRoom(rooms[0], -1);
        }

        void CreatePlayer()
        {
            var root = new GameObject("Player");
            var controller = root.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.height = 1.8f;
            controller.radius = 0.4f;
            Visual(root.transform, PrimitiveType.Capsule, new Vector3(0f, 0.9f, 0f),
                new Vector3(0.7f, 0.9f, 0.7f), new Color(0.2f, 0.55f, 1f));
            Visual(root.transform, PrimitiveType.Cube, new Vector3(0f, 0.8f, 0.55f),
                new Vector3(0.2f, 0.2f, 0.65f), Color.yellow);
            player = root.AddComponent<PlayerController>();
            player.Initialize(this, viewCamera);
        }

        void EnsureDefaultItems()
        {
            if (weapons == null || weapons.Length == 0)
            {
                var basic = ScriptableObject.CreateInstance<RaytraceWeaponDefinition>();
                basic.displayName = "Mouse Blaster";
                var melee = ScriptableObject.CreateInstance<MeleeWeaponDefinition>();
                melee.displayName = "Mouse Sweeper";
                var particles = ScriptableObject.CreateInstance<ParticleWeaponDefinition>();
                particles.displayName = "Mouse Flamer";
                generatedAssets.Add(basic);
                generatedAssets.Add(melee);
                generatedAssets.Add(particles);
                weapons = new WeaponDefinition[] { basic, melee, particles };
            }
            if (passiveItems == null || passiveItems.Length == 0)
            {
                var shoes = ScriptableObject.CreateInstance<StatPassiveItemDefinition>();
                shoes.displayName = "Running Shoes";
                var roomDamage = ScriptableObject.CreateInstance<RoomDamagePassiveItemDefinition>();
                roomDamage.displayName = "Room Shock";
                var revival = ScriptableObject.CreateInstance<RevivePassiveItemDefinition>();
                revival.displayName = "Spare Life";
                generatedAssets.Add(shoes);
                generatedAssets.Add(roomDamage);
                generatedAssets.Add(revival);
                passiveItems = new PassiveItemDefinition[] { shoes, roomDamage, revival };
            }
            if (weapons[0] != null) player.Equip(new WeaponInstance(weapons[0]));
        }

        void GenerateMaze()
        {
            var occupied = new Dictionary<Vector2Int, Room>();
            var start = new Room { position = Vector2Int.zero };
            rooms.Add(start);
            occupied.Add(start.position, start);
            int desired = Mathf.Clamp(roomCount, 2, 100);
            while (rooms.Count < desired)
            {
                Room parent = rooms[Random.Range(0, rooms.Count)];
                int first = Random.Range(0, 4);
                for (int i = 0; i < 4; i++)
                {
                    int direction = (first + i) % 4;
                    Vector2Int position = parent.position + Directions[direction];
                    if (occupied.ContainsKey(position)) continue;
                    var room = new Room { position = position, depth = parent.depth + 1 };
                    parent.neighbours[direction] = room;
                    room.neighbours[(direction + 2) % 4] = parent;
                    rooms.Add(room);
                    occupied.Add(position, room);
                    break;
                }
            }
            bossRoom = start;
            foreach (Room room in rooms)
                if (room.depth > bossRoom.depth) bossRoom = room;
            var specialRooms = new List<Room>(rooms);
            specialRooms.Remove(start);
            specialRooms.Remove(bossRoom);
            if (specialRooms.Count < 2)
                Debug.LogWarning("At least four rooms are needed for both a shop and a treasure room.", this);
            if (specialRooms.Count > 0)
            {
                int index = Random.Range(0, specialRooms.Count);
                specialRooms[index].kind = RoomKind.Shop;
                specialRooms.RemoveAt(index);
            }
            if (specialRooms.Count > 0)
                specialRooms[Random.Range(0, specialRooms.Count)].kind = RoomKind.Treasure;
        }

        void EnterRoom(Room room, int entryDirection)
        {
            if (currentRoom != null)
            {
                player.RoomExit();
                currentRoom.root.SetActive(false);
            }
            currentRoom = room;
            if (!room.visited) BuildRoom(room);
            room.root.SetActive(true);
            Vector3 position = Vector3.zero;
            if (entryDirection >= 0)
            {
                Vector2Int direction = Directions[entryDirection];
                position = new Vector3(direction.x * (roomWidth / 2f - 2f), 0.05f,
                    direction.y * (roomDepth / 2f - 2f));
            }
            CharacterController controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = position;
            controller.enabled = true;
            transitionDelay = 0.5f;
            player.RoomEnter();
            RefreshMinimap();
        }

        void BuildRoom(Room room)
        {
            room.visited = true;
            room.root = new GameObject((room.kind == RoomKind.Combat ? "Room " : room.kind + " Room ") + room.position);
            room.root.transform.SetParent(transform);
            if (roomPrefab != null) Instantiate(roomPrefab, room.root.transform);
            room.floor = Block(room.root.transform, "Floor", new Vector3(0f, -0.5f, 0f),
                new Vector3(roomWidth, 1f, roomDepth), new Color(0.24f, 0.25f, 0.28f)).GetComponent<Collider>();
            for (int i = 0; i < 4; i++) BuildWall(room, i);
            if (room == rooms[0])
            {
                room.cleared = true;
                UpdateDoors(room);
                return;
            }
            if (room.kind != RoomKind.Combat)
            {
                room.cleared = true;
                UpdateDoors(room);
                if (room.kind == RoomKind.Shop) PopulateShop();
                else PopulateTreasureRoom();
                return;
            }
            int count = (room == bossRoom ? 1 : Mathf.Max(0, micePerRoom)) + levelIndex;
            RoomLayout layout = room.root.GetComponentInChildren<RoomLayout>();
            for (int i = 0; i < count; i++)
            {
                Vector3 hide = new Vector3(i % 2 == 0 ? -roomWidth / 2f + 1.5f : roomWidth / 2f - 1.5f,
                    0f, i % 4 < 2 ? -roomDepth / 2f + 1.5f : roomDepth / 2f - 1.5f);
                if (layout != null && layout.hideSpots != null && layout.hideSpots.Length > 0)
                {
                    Transform spot = layout.hideSpots[i % layout.hideSpots.Length];
                    if (spot != null) hide = spot.position;
                }
                SpawnMouse(room, hide, room == bossRoom && i == 0);
            }
            room.cleared = room.enemies == 0;
            UpdateDoors(room);
        }

        void BuildWall(Room room, int direction)
        {
            bool horizontal = direction == 0 || direction == 2;
            float length = horizontal ? roomWidth : roomDepth;
            float edge = (horizontal ? roomDepth : roomWidth) / 2f;
            float sign = direction < 2 ? 1f : -1f;
            Vector3 center = horizontal ? new Vector3(0f, 1f, edge * sign) : new Vector3(edge * sign, 1f, 0f);
            if (room.neighbours[direction] == null)
            {
                Block(room.root.transform, "Wall", center,
                    horizontal ? new Vector3(length + 1f, 2f, 1f) : new Vector3(1f, 2f, length + 1f), Color.gray);
                return;
            }
            float section = (length - 3f) / 2f;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 offset = horizontal ? new Vector3(side * (length + 3f) / 4f, 0f, 0f)
                    : new Vector3(0f, 0f, side * (length + 3f) / 4f);
                Block(room.root.transform, "Wall", center + offset,
                    horizontal ? new Vector3(section, 2f, 1f) : new Vector3(1f, 2f, section), Color.gray);
            }
            room.doors[direction] = Block(room.root.transform, "Locked Door", center,
                horizontal ? new Vector3(3f, 2f, 1f) : new Vector3(1f, 2f, 3f), Color.red);
        }

        void UpdateDoors(Room room)
        {
            foreach (GameObject door in room.doors)
                if (door != null) door.SetActive(!CanLeave(room));
        }

        bool CanLeave(Room room) => room.cleared || (room == bossRoom && room.bossDefeated);

        void SpawnMouse(Room room, Vector3 hide, bool boss)
        {
            MouseEnemy mouse;
            GameObject root;
            if (!boss && mousePrefabs != null && mousePrefabs.Length > 0)
            {
                MouseEnemy prefab = mousePrefabs[Random.Range(0, mousePrefabs.Length)];
                if (prefab == null)
                {
                    Debug.LogError("Mouse prefab list contains an empty entry.", this);
                    return;
                }
                mouse = Instantiate(prefab);
                root = mouse.gameObject;
            }
            else
            {
                root = new GameObject(boss ? "Boss Mouse" : "Mouse");
                var controller = root.AddComponent<CharacterController>();
                controller.center = new Vector3(0f, 0.5f, 0f);
                controller.height = 1f;
                controller.radius = 0.35f;
                Visual(root.transform, PrimitiveType.Capsule, new Vector3(0f, 0.5f, 0f),
                    boss ? new Vector3(1.1f, 0.8f, 1.1f) : new Vector3(0.55f, 0.5f, 0.55f),
                    boss ? new Color(0.7f, 0.2f, 0.2f) : new Color(0.55f, 0.45f, 0.4f));
                mouse = root.AddComponent<MouseEnemy>();
            }
            root.transform.SetParent(room.root.transform);
            root.transform.position = RandomPointInRoom();
            Physics.SyncTransforms();
            CharacterController mouseController = root.GetComponent<CharacterController>();
            float bottom = mouseController.bounds.min.y;
            root.transform.position += Vector3.up * (room.floor.bounds.max.y + 0.02f - bottom);
            if (boss) { mouse.maxHealth = 200f; mouse.attackDamage = 15f; mouse.movementSpeed = 3f; }
            float levelMultiplier = Mathf.Pow(1.1f, levelIndex);
            mouse.maxHealth *= levelMultiplier;
            mouse.attackDamage *= levelMultiplier;
            mouse.attackInterval /= levelMultiplier;
            mouse.Initialize(this, player, hide, Mathf.Max(1, room.depth));
            if (boss) room.boss = mouse;
            room.enemies++;
        }

        public Vector3 RandomPointInRoom()
        {
            return new Vector3(Random.Range(-roomWidth / 2f + 2f, roomWidth / 2f - 2f), 0.05f,
                Random.Range(-roomDepth / 2f + 2f, roomDepth / 2f - 2f));
        }

        public void MouseDefeated(MouseEnemy mouse)
        {
            currentRoom.defeatPositions.Add(currentRoom.root.transform.InverseTransformPoint(mouse.transform.position));
            currentRoom.enemies = Mathf.Max(0, currentRoom.enemies - 1);
            if (mouse == currentRoom.boss)
            {
                currentRoom.bossDefeated = true;
                SpawnVent(currentRoom);
                UpdateDoors(currentRoom);
            }
            if (currentRoom.enemies != 0) return;
            currentRoom.cleared = true;
            SpawnRoomRewards(currentRoom);
            clearedCombatRooms++;
            UpdateDoors(currentRoom);
            RefreshMinimap();
        }

        public void RefreshMinimap()
        {
            if (minimap == null) return;
            foreach (Room room in rooms)
            {
                bool visible = room.visited;
                if (!visible)
                    foreach (Room neighbour in room.neighbours)
                        if (neighbour != null && neighbour.visited) { visible = true; break; }
                if (!visible) continue;

                PickupKind kind = PickupKind.None;
                if (room.visited && room.root != null)
                    foreach (ItemPickup pickup in room.root.GetComponentsInChildren<ItemPickup>(true))
                        if (pickup.Available && (int)pickup.Kind > (int)kind) kind = pickup.Kind;

                Color color = room == bossRoom ? Color.red :
                    kind == PickupKind.Weapon ? new Color(0.6f, 0.2f, 0.8f) :
                    kind == PickupKind.Passive ? Color.cyan : kind == PickupKind.Health ? Color.yellow :
                    kind == PickupKind.Money ? Color.green : room == currentRoom ? Color.white :
                    room.cleared ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.25f, 0.25f, 0.25f);
                minimap.SetRoom(room.position, color);
            }
            minimap.LayoutRooms();
        }

        public void DamageMiceInCurrentRoom(float amount, PlayerController attacker)
        {
            if (currentRoom == null || currentRoom.root == null || amount <= 0f) return;
            foreach (MouseEnemy mouse in currentRoom.root.GetComponentsInChildren<MouseEnemy>())
                mouse.TakeDamage(amount, attacker);
        }

        public void ShowDamage(Vector3 position, float amount, bool isPlayer)
        {
            damageIndicators.Show(position, amount, isPlayer);
        }

        static float HealthDropChance(float healthFraction)
        {
            return healthFraction < 0.25f ? 20f * 2f / 3f : 0f;
        }

        void SpawnRoomRewards(Room room)
        {
            if (room.defeatPositions.Count == 0) return;
            float roll = Random.Range(0, 1000000) * 0.0001f;
            if (roll < HealthDropChance(player.Health / player.MaxHealth))
            {
                Vector3 position = RewardPosition(room.defeatPositions[Random.Range(0, room.defeatPositions.Count)]);
                SpawnVial(healthVialPrefab, position, true, true);
            }
            else
            {
                foreach (Vector3 defeatPosition in room.defeatPositions)
                    SpawnVial(cashVialPrefab, RewardPosition(defeatPosition), false, true);
            }
            room.defeatPositions.Clear();
        }

        Vector3 RewardPosition(Vector3 defeatPosition)
        {
            return new Vector3(
                Mathf.Clamp(defeatPosition.x, -roomWidth / 2f + 1.5f, roomWidth / 2f - 1.5f),
                0.3f,
                Mathf.Clamp(defeatPosition.z, -roomDepth / 2f + 1.5f, roomDepth / 2f - 1.5f));
        }

        T RandomDefinition<T>(T[] definitions) where T : Object
        {
            int count = 0;
            foreach (T definition in definitions)
                if (definition != null) count++;
            if (count == 0)
            {
                Debug.LogError($"No {typeof(T).Name} is configured for a reward.", this);
                return null;
            }
            int selected = Random.Range(0, count);
            foreach (T definition in definitions)
                if (definition != null && selected-- == 0) return definition;
            return null;
        }

        void PopulateShop()
        {
            Vector3 healthPosition = new Vector3(-2.25f, 0.3f, 0f);
            Vector3 passivePosition = new Vector3(0f, 0.3f, 0f);
            Vector3 weaponPosition = new Vector3(2.25f, 0.3f, 0f);
            SetShopPrice(SpawnVial(healthVialPrefab, healthPosition, true, false), healthPosition, 4);
            PassiveItemDefinition passive = RandomDefinition(passiveItems);
            if (passive != null) SetShopPrice(SpawnPassivePickup(passive, passivePosition), passivePosition, 8);
            WeaponDefinition weapon = RandomDefinition(weapons);
            if (weapon != null) SetShopPrice(SpawnWeaponPickup(new WeaponInstance(weapon), weaponPosition), weaponPosition, 12);
        }

        void PopulateTreasureRoom()
        {
            Vector3 position = new Vector3(0f, 0.3f, 0f);
            if (Random.Range(0, 2) == 0)
            {
                WeaponDefinition weapon = RandomDefinition(weapons);
                if (weapon != null) SpawnWeaponPickup(new WeaponInstance(weapon), position);
            }
            else
            {
                PassiveItemDefinition passive = RandomDefinition(passiveItems);
                if (passive != null) SpawnPassivePickup(passive, position);
            }
        }

        void SetShopPrice(ItemPickup pickup, Vector3 position, int price)
        {
            if (pickup == null) return;
            var label = new GameObject("Price $" + price);
            label.transform.SetParent(currentRoom.root.transform, false);
            label.transform.localPosition = new Vector3(position.x, 0.02f, position.z - 0.9f);
            label.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            TextMesh text = label.AddComponent<TextMesh>();
            text.text = "$" + price;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 64;
            text.characterSize = 0.08f;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                Debug.LogError("The built-in font for shop prices is unavailable.", this);
            else
                label.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
            pickup.SetPrice(price, label);
        }

        ItemPickup SpawnVial(GameObject prefab, Vector3 position, bool isHealth, bool automatic)
        {
            GameObject item;
            if (prefab != null)
            {
                item = Instantiate(prefab, currentRoom.root.transform);
                item.transform.localPosition = new Vector3(position.x, 0f, position.z);
                if ((isHealth && item.GetComponent<HealthVial>() == null) ||
                    (!isHealth && item.GetComponent<MoneyVial>() == null) || item.GetComponent<ItemPickup>() == null)
                {
                    Debug.LogError("Vial prefab is missing its vial or ItemPickup component.", prefab);
                    Destroy(item);
                    return null;
                }
            }
            else
            {
                Debug.LogWarning($"No {(isHealth ? "health" : "cash")} vial prefab configured; using a placeholder.", this);
                item = Block(currentRoom.root.transform, isHealth ? "Health Vial" : "Cash Vial", position,
                    Vector3.one * 0.55f, isHealth ? Color.yellow : Color.green);
                item.GetComponent<Collider>().isTrigger = true;
                if (isHealth) item.AddComponent<HealthVial>();
                else item.AddComponent<MoneyVial>();
                item.AddComponent<ItemPickup>();
            }
            ItemPickup pickup = item.GetComponent<ItemPickup>();
            if (automatic)
            {
                Collider trigger = item.GetComponent<Collider>();
                if (trigger == null || !trigger.enabled || !trigger.isTrigger)
                {
                    Debug.LogError("An automatic vial pickup requires an enabled trigger collider.", item);
                    Destroy(item);
                    return null;
                }
                pickup.SetAutomaticCollection();
            }
            return pickup;
        }

        public ItemPickup SpawnWeaponPickup(WeaponInstance weapon, Vector3 position)
        {
            GameObject item = Block(currentRoom.root.transform, "Weapon Pickup", position,
                new Vector3(0.55f, 0.55f, 0.55f), Color.yellow);
            Destroy(item.GetComponent<Collider>());
            ItemPickup pickup = item.AddComponent<ItemPickup>();
            pickup.weapon = weapon;
            RefreshMinimap();
            return pickup;
        }

        ItemPickup SpawnPassivePickup(PassiveItemDefinition passive, Vector3 position)
        {
            GameObject item = Block(currentRoom.root.transform, "Passive Pickup", position,
                new Vector3(0.55f, 0.55f, 0.55f), Color.cyan);
            Destroy(item.GetComponent<Collider>());
            ItemPickup pickup = item.AddComponent<ItemPickup>();
            pickup.passive = passive;
            return pickup;
        }

        GameObject Block(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent);
            block.transform.localPosition = position;
            block.transform.localScale = scale;
            SetColor(block, color);
            return block;
        }

        void Visual(Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            GameObject visual = GameObject.CreatePrimitive(type);
            visual.transform.SetParent(parent);
            visual.transform.localPosition = position;
            visual.transform.localScale = scale;
            SetColor(visual, color);
            Destroy(visual.GetComponent<Collider>());
        }

        void SetColor(GameObject target, Color color)
        {
            Material material = target.GetComponent<Renderer>().material;
            material.color = color;
            if (currentRoom != null) levelAssets.Add(material);
            else generatedAssets.Add(material);
        }

        void OnDestroy()
        {
            foreach (Object asset in generatedAssets)
                if (asset != null) Destroy(asset);
            foreach (Object asset in levelAssets)
                if (asset != null) Destroy(asset);
        }

        void SpawnVent(Room room)
        {
            var vent = new GameObject("Next Level Vent");
            vent.transform.SetParent(room.root.transform, false);
            Visual(vent.transform, PrimitiveType.Cube, new Vector3(0f, 0.06f, 0f),
                new Vector3(1.8f, 0.12f, 1.8f), new Color(0.45f, 0.45f, 0.5f));
            for (int i = -2; i <= 2; i++)
                Visual(vent.transform, PrimitiveType.Cube, new Vector3(i * 0.3f, 0.13f, 0f),
                    new Vector3(0.12f, 0.03f, 1.4f), new Color(0.12f, 0.12f, 0.14f));
            BoxCollider trigger = vent.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = Vector3.up;
            trigger.size = new Vector3(2f, 2f, 2f);
            room.vent = trigger;
        }

        bool CanUseVent()
        {
            return !Finished && currentRoom == bossRoom && currentRoom != null && currentRoom.vent != null &&
                currentRoom.vent.bounds.Contains(player.transform.position + Vector3.up * 0.5f);
        }

        public bool TryUseVent()
        {
            if (!CanUseVent()) return false;
            StartNextLevel();
            return true;
        }

        void StartNextLevel()
        {
            player.RoomExit();
            foreach (Room room in rooms)
            {
                if (room.root == null) continue;
                room.root.SetActive(false);
                Destroy(room.root);
            }
            foreach (Object asset in levelAssets)
                if (asset != null) Destroy(asset);
            levelAssets.Clear();
            rooms.Clear();
            currentRoom = null;
            bossRoom = null;
            if (minimap != null) minimap.ClearRooms();
            levelIndex++;
            Finished = false;
            message = null;
            GenerateMaze();
            EnterRoom(rooms[0], -1);
        }

        void Update()
        {
            if (Finished)
            {
                if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }
            transitionDelay -= Time.deltaTime;
            if (transitionDelay > 0f || currentRoom == null || !CanLeave(currentRoom)) return;
            Vector3 position = player.transform.position;
            int direction = -1;
            if (position.z > roomDepth / 2f && Mathf.Abs(position.x) < 1.5f) direction = 0;
            else if (position.x > roomWidth / 2f && Mathf.Abs(position.z) < 1.5f) direction = 1;
            else if (position.z < -roomDepth / 2f && Mathf.Abs(position.x) < 1.5f) direction = 2;
            else if (position.x < -roomWidth / 2f && Mathf.Abs(position.z) < 1.5f) direction = 3;
            if (direction >= 0 && currentRoom.neighbours[direction] != null)
                EnterRoom(currentRoom.neighbours[direction], (direction + 2) % 4);
        }

        void LateUpdate()
        {
            if (player != null && viewCamera != null)
                viewCamera.transform.position = player.transform.position - viewCamera.transform.forward * 20f;
        }

        public void EndRun()
        {
            Finished = true;
        }

        public void ShowMessage(string text)
        {
            message = text;
            messageUntil = Time.time + 2f;
        }

        void OnGUI()
        {
            if (player == null) return;
            GUI.Label(new Rect(15f, Screen.height - 65f, 260f, 25f),
                $"Health: {player.Health:0} / {player.MaxHealth:0}");
            GUI.Label(new Rect(15f, Screen.height - 45f, 260f, 25f), $"Money: {player.Money}");
            GUI.Label(new Rect(15f, Screen.height - 25f, 260f, 25f), $"Level: {levelIndex + 1}");
            for (int i = 0; i < player.Weapons.Length; i++)
            {
                WeaponInstance weapon = player.Weapons[i];
                string text = weapon == null ? "Empty" : $"{weapon.Definition.displayName} ({weapon.Energy:0.0}/{weapon.Definition.maxEnergy:0})";
                GUI.Label(new Rect(Screen.width - 270f, Screen.height - 85f + i * 22f, 255f, 25f),
                    $"{(player.SelectedWeapon == i ? ">" : " ")} {i + 1}: {text}");
            }
            GUI.Label(new Rect(Screen.width - 270f, 15f, 250f, 25f), "Passive items:");
            for (int i = 0; i < player.Passives.Count; i++)
                GUI.Label(new Rect(Screen.width - 270f, 37f + i * 20f, 250f, 22f), player.Passives[i].displayName);
            GUI.Label(new Rect(15f, Screen.height - 105f, 450f, 25f), "WASD move | Mouse aim/fire | E pick up | 1/2 switch | R reload");
            if (currentRoom != null && !currentRoom.cleared)
                GUI.Label(new Rect(15f, Screen.height - 130f, 250f, 25f), $"Mice remaining: {currentRoom.enemies}");
            if (CanUseVent())
                GUI.Label(new Rect(Screen.width / 2f - 85f, Screen.height - 70f, 170f, 25f), "E: Enter vent");
            if (!string.IsNullOrEmpty(message) && Time.time < messageUntil)
                GUI.Label(new Rect(Screen.width / 2f - 140f, Screen.height - 95f, 280f, 25f), message);
            if (Finished)
                GUI.Box(new Rect(Screen.width / 2f - 130f, Screen.height / 2f - 40f, 260f, 80f),
                    "You died\nPress Enter to restart");
        }
    }
}
