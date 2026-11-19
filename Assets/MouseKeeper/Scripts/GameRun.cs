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
        [Min(2)] public int roomCount = 9;
        [Min(0)] public int micePerRoom = 5;
        public float roomWidth = 18f;
        public float roomDepth = 14f;

        public bool Finished { get; private set; }
        readonly List<Room> rooms = new List<Room>();
        readonly List<Object> generatedAssets = new List<Object>();
        static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        Room currentRoom;
        Room bossRoom;
        PlayerController player;
        Camera viewCamera;
        bool won;
        float transitionDelay;

        sealed class Room
        {
            public Vector2Int position;
            public readonly Room[] neighbours = new Room[4];
            public readonly GameObject[] doors = new GameObject[4];
            public GameObject root;
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
            CreatePlayer();
            EnsureDefaultItems();
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
                var basic = ScriptableObject.CreateInstance<WeaponDefinition>();
                basic.displayName = "Mouse Blaster";
                var heavy = ScriptableObject.CreateInstance<WeaponDefinition>();
                heavy.displayName = "Heavy Blaster";
                heavy.damage = 35f;
                heavy.attackInterval = 0.5f;
                heavy.maxEnergy = 8f;
                generatedAssets.Add(basic);
                generatedAssets.Add(heavy);
                weapons = new[] { basic, heavy };
            }
            if (passiveItems == null || passiveItems.Length == 0)
            {
                var shoes = ScriptableObject.CreateInstance<PassiveItemDefinition>();
                shoes.displayName = "Running Shoes";
                generatedAssets.Add(shoes);
                passiveItems = new[] { shoes };
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
        }

        void BuildRoom(Room room)
        {
            room.visited = true;
            room.root = new GameObject("Room " + room.position);
            room.root.transform.SetParent(transform);
            if (roomPrefab != null) Instantiate(roomPrefab, room.root.transform);
            Block(room.root.transform, "Floor", new Vector3(0f, -0.5f, 0f),
                new Vector3(roomWidth, 1f, roomDepth), new Color(0.24f, 0.25f, 0.28f));
            for (int i = 0; i < 4; i++) BuildWall(room, i);
            if (room == rooms[0])
            {
                room.cleared = true;
                UpdateDoors(room);
                return;
            }
            int count = room == bossRoom ? 1 : Mathf.Max(0, micePerRoom);
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
                SpawnMouse(room, hide, room == bossRoom);
            }
            room.cleared = room.enemies == 0;
            UpdateDoors(room);
            if (room != bossRoom)
            {
                if (weapons.Length > 1 && weapons[1] != null)
                    SpawnWeaponPickup(new WeaponInstance(weapons[1]), new Vector3(-2f, 0.3f, 0f));
                if (passiveItems.Length > 0 && passiveItems[0] != null)
                    SpawnPassivePickup(passiveItems[0], new Vector3(2f, 0.3f, 0f));
            }
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
                if (door != null) door.SetActive(!room.cleared);
        }

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
            if (boss) { mouse.maxHealth = 200f; mouse.attackDamage = 15f; mouse.movementSpeed = 3f; }
            mouse.Initialize(this, player, hide, Mathf.Max(1, room.depth));
            room.enemies++;
        }

        public Vector3 RandomPointInRoom()
        {
            return new Vector3(Random.Range(-roomWidth / 2f + 2f, roomWidth / 2f - 2f), 0.05f,
                Random.Range(-roomDepth / 2f + 2f, roomDepth / 2f - 2f));
        }

        public void MouseDefeated(MouseEnemy mouse)
        {
            currentRoom.enemies = Mathf.Max(0, currentRoom.enemies - 1);
            if (currentRoom.enemies != 0) return;
            currentRoom.cleared = true;
            UpdateDoors(currentRoom);
            if (currentRoom == bossRoom) EndRun(true);
        }

        public void SpawnWeaponPickup(WeaponInstance weapon, Vector3 position)
        {
            GameObject item = Block(currentRoom.root.transform, "Weapon Pickup", position,
                new Vector3(0.55f, 0.55f, 0.55f), Color.yellow);
            Destroy(item.GetComponent<Collider>());
            item.AddComponent<ItemPickup>().weapon = weapon;
        }

        void SpawnPassivePickup(PassiveItemDefinition passive, Vector3 position)
        {
            GameObject item = Block(currentRoom.root.transform, "Passive Pickup", position,
                new Vector3(0.55f, 0.55f, 0.55f), Color.cyan);
            Destroy(item.GetComponent<Collider>());
            item.AddComponent<ItemPickup>().passive = passive;
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
            generatedAssets.Add(material);
        }

        void OnDestroy()
        {
            foreach (Object asset in generatedAssets)
                if (asset != null) Destroy(asset);
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
            if (transitionDelay > 0f || currentRoom == null || !currentRoom.cleared) return;
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

        public void EndRun(bool victory)
        {
            Finished = true;
            won = victory;
        }

        void OnGUI()
        {
            if (player == null) return;
            GUI.Label(new Rect(15f, Screen.height - 65f, 260f, 25f),
                $"Health: {player.Health:0} / {player.MaxHealth:0}");
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
            GUI.Label(new Rect(15f, 15f, 450f, 25f), "WASD move | Mouse aim/fire | E pick up | 1/2 switch | R reload");
            if (currentRoom != null && !currentRoom.cleared)
                GUI.Label(new Rect(15f, 40f, 250f, 25f), $"Mice remaining: {currentRoom.enemies}");
            if (Finished)
                GUI.Box(new Rect(Screen.width / 2f - 130f, Screen.height / 2f - 40f, 260f, 80f),
                    (won ? "Victory!" : "You died") + "\nPress Enter to restart");
        }
    }
}
