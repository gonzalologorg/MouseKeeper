using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MouseKeeper
{
    public sealed partial class GameRun : MonoBehaviour
    {
        public GameObject roomPrefab;
        RoomLayout[] roomLayouts;
        public EnemySpawnPool enemySpawnPool;
        public WeaponDefinition[] weapons;
        public PassiveItemDefinition[] passiveItems;
        public GameObject cashVialPrefab;
        public GameObject healthVialPrefab;
        public GameObject[] agentModels;
        public AgentDefinition[] squad;
        public RoomArchitecture roomArchitecture;
        [Min(0)] public int micePerRoom = 5;
        [Min(0.01f)] public float mouseSpawnInterval = 0.35f;
        public float roomWidth = 18f;
        public float roomDepth = 14f;

        public bool Finished { get; private set; }
        public bool IsTransitioning { get; private set; }
        internal bool IsBoss(MouseEnemy mouse) => bossRoom != null && bossRoom.boss == mouse;
        public Transform CurrentRoom => currentRoom != null ? currentRoom.root.transform : null;
        readonly List<Room> rooms = new List<Room>();
        readonly List<PlayerController> agents = new List<PlayerController>();
        readonly List<Object> generatedAssets = new List<Object>();
        readonly List<Object> levelAssets = new List<Object>();
        static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        Room currentRoom;
        Room bossRoom;
        PlayerController player;
        Camera viewCamera;
        Vector3 cameraFocus;
        float cameraDistance = 20f;
        float cameraAspect;
        Minimap minimap;
        DamageIndicators damageIndicators;
        HexBackground hexBackground;
        SquadIndicators squadIndicators;
        Texture2D hudIcons;
        float transitionDelay;
        string message;
        float messageUntil;
        int clearedCombatRooms;
        int miceDefeated;
        double score = 1000;
        float scoreElapsed;
        int levelIndex;
        int fullyClearedFloors;
        float completedFloorCredit;
        int widthPhase;
        int depthPhase;

        enum RoomKind { Combat, Shop, Treasure, Corridor }

        sealed class Room
        {
            public Vector2Int position;
            public Vector3 center;
            public float width;
            public float depthSize;
            public RoomKind kind;
            public readonly Room[] neighbours = new Room[4];
            public readonly RoomDoor[] doors = new RoomDoor[4];
            public readonly Transform[] walls = new Transform[4];
            public readonly Dictionary<Renderer, bool> wallRendererStates = new Dictionary<Renderer, bool>();
            public readonly List<Vector3> defeatPositions = new List<Vector3>();
            public GameObject root;
            public Collider floor;
            public Collider vent;
            public RoomDoor exitDoor;
            public int exitDirection = -1;
            public MouseEnemy boss;
            public int bossDamageThresholds;
            public int pendingBossRats;
            public Vector3 bossSpawnOrigin;
            public bool bossDefeated;
            public int enemies;
            public bool visited;
            public bool cleared;
            public bool spawning;
            public int spawnGeneration;
            public bool hasPillars;
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
            if (mode != LoadSceneMode.Single || scene.name != "SampleScene" || FindFirstObjectByType<GameRun>() != null) return;
            new GameObject("MouseKeeper Run").AddComponent<GameRun>();
        }

        void Start()
        {
            if (!ExpeditionSession.Initialize()) { enabled = false; return; }
            if (!ExpeditionSession.Active)
            {
                SceneManager.LoadScene("Planning");
                return;
            }
            weapons = ExpeditionSession.Catalog.weapons;
            if (enemySpawnPool == null) enemySpawnPool = ExpeditionSession.Catalog.enemySpawnPool;
            if (enemySpawnPool == null || enemySpawnPool.boss == null)
            {
                Debug.LogError("El pool de enemigos o su jefe no están configurados.", this);
                enabled = false;
                return;
            }
            for (int floor = 0; floor < ExpeditionSession.FloorCount; floor++)
                if (enemySpawnPool.Choose(floor, 0f) == null)
                {
                    Debug.LogError("El pool no tiene enemigos con peso positivo para el piso " + (floor + 1) + ".", this);
                    enabled = false;
                    return;
                }
            squad = new AgentDefinition[ExpeditionSession.SquadSize];
            for (int i = 0; i < squad.Length; i++)
                squad[i] = ExpeditionSession.Catalog.agents[ExpeditionSession.Data.selectedAgents[i]];
            roomWidth = Mathf.Max(8f, roomWidth);
            roomDepth = Mathf.Max(8f, roomDepth);
            if (roomArchitecture == null) roomArchitecture = Resources.Load<RoomArchitecture>("RoomArchitecture");
            roomLayouts = Resources.LoadAll<RoomLayout>("RoomLayouts");
            if (roomArchitecture == null || !roomArchitecture.IsValid())
            {
                Debug.LogError("Room architecture is missing required wall, window, pillar, arch or door models.", this);
                enabled = false;
                return;
            }
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
            hexBackground = gameObject.AddComponent<HexBackground>();
            hexBackground.Initialize(viewCamera);
            damageIndicators = gameObject.AddComponent<DamageIndicators>();
            damageIndicators.Initialize(viewCamera);
            squadIndicators = gameObject.AddComponent<SquadIndicators>();
            squadIndicators.Initialize(viewCamera);
            hudIcons = Resources.Load<Texture2D>("HudIcons");
            if (hudIcons == null || hudIcons.width != 512 || hudIcons.height != 64)
            {
                Debug.LogError("El atlas de iconos del HUD falta o tiene dimensiones incorrectas.", this);
                hudIcons = null;
            }
            EnsureDefaultItems();
            bool hasWeapon = false;
            foreach (WeaponDefinition weapon in weapons)
                if (weapon != null) { hasWeapon = true; break; }
            if (!hasWeapon)
            {
                Debug.LogError("At least one agent weapon must be configured.", this);
                enabled = false;
                return;
            }
            if (ExpeditionSession.AutoRun)
                UnityEngine.Random.InitState(ExpeditionSession.AutoRunSeedFor(ExpeditionSession.AutoRunMapSeed, 0, ExpeditionSession.AutoRunWorker));
            if (!LoadAgentProfiles())
            {
                enabled = false;
                return;
            }
            CreateSquad();
            InitializeBalanceTelemetry();
            minimap = FindAnyObjectByType<Minimap>();
            BuildExpeditionFloor();
            if (IntroSequence.ConsumeArrival()) StartCoroutine(EnterFromIntro(rooms[0]));
            else EnterRoom(rooms[0], -1);
        }

        void CreateSquad()
        {
            var autoRunLuck = ExpeditionSession.AutoRun
                ? new System.Random(ExpeditionSession.AutoRunSeedFor(ExpeditionSession.AutoRunMapSeed, 0, 100 + ExpeditionSession.AutoRunWorker))
                : null;
            for (int i = 0; i < profiles.Length; i++)
            {
                AgentProfile profile = profiles[i];
                int luckUpgrade = ExpeditionSession.Data.progress[ExpeditionSession.Data.selectedAgents[i]].luck;
                profile.luck = Mathf.Min(10, (autoRunLuck?.Next(1, 11) ?? UnityEngine.Random.Range(1, 11)) + luckUpgrade);
                AgentDefinition definition = squad != null && squad.Length > 0 ? squad[i] : null;
                var root = new GameObject(profile.name);
                var controller = root.AddComponent<CharacterController>();
                controller.center = new Vector3(0f, 0.9f, 0f);
                controller.height = 1.8f;
                controller.radius = 0.4f;
                GameObject model = definition != null ? definition.modelPrefab :
                    profile.model >= 3 && agentModels != null && profile.model - 3 < agentModels.Length
                        ? agentModels[profile.model - 3] : null;
                if (model != null)
                    Instantiate(model, root.transform);
                else
                {
                    Color[] colors = { new Color(0.2f, 0.55f, 1f), new Color(0.2f, 0.8f, 0.45f), new Color(1f, 0.55f, 0.2f) };
                    Visual(root.transform, PrimitiveType.Capsule, new Vector3(0f, 0.9f, 0f),
                        new Vector3(0.7f, 0.9f, 0.7f),
                        definition != null ? definition.placeholderColor : colors[Mathf.Clamp(profile.model, 0, 2)]);
                }
                root.AddComponent<AgentOcclusionIndicator>().Initialize();
                PlayerController agent = root.AddComponent<PlayerController>();
                agent.Initialize(this, profile);
                if (definition != null) agent.SetVoicePitch(definition.VoicePitch);
                agent.SetSpecialty(ExpeditionSession.Data.specialties != null ? ExpeditionSession.Data.specialties[i] : Specialty.None);
                WeaponDefinition startingWeapon = ExpeditionSession.Active
                    ? ExpeditionSession.Catalog.weapons[ExpeditionSession.Data.selectedWeapons[i]]
                    : definition != null && definition.startingWeapon != null
                    ? definition.startingWeapon : weapons[Mathf.Clamp(profile.weapon, 0, weapons.Length - 1)];
                if (startingWeapon == null)
                {
                    Debug.LogWarning("The selected starting weapon is empty; using the first configured weapon.", this);
                    foreach (WeaponDefinition candidate in weapons)
                        if (candidate != null) { startingWeapon = candidate; break; }
                }
                agent.Equip(new WeaponInstance(startingWeapon, profile.attackSpeed, profile.agility));
                if (ExpeditionSession.Active && ExpeditionSession.Data.selectedMoney[i] > 0)
                    agent.AddMoney(ExpeditionSession.Data.selectedMoney[i]);
                agents.Add(agent);
                squadIndicators.Register(agent);
                AgentDialogue dialogue = root.AddComponent<AgentDialogue>();
                dialogue.Initialize(agent, definition, squadIndicators);
                agent.SetDialogue(dialogue);
            }
            player = agents[0];
            for (int i = 0; i < agents.Count; i++)
                for (int j = 0; j < i; j++)
                    Physics.IgnoreCollision(agents[i].GetComponent<CharacterController>(), agents[j].GetComponent<CharacterController>());
        }

        void EnsureDefaultItems()
        {
            if (weapons == null || weapons.Length == 0)
            {
                var basic = ScriptableObject.CreateInstance<RaytraceWeaponDefinition>();
                basic.displayName = "Mouse Blaster";
                var melee = ScriptableObject.CreateInstance<MeleeWeaponDefinition>();
                melee.displayName = "Mouse Sweeper";
                melee.tags = WeaponTags.DPS | WeaponTags.Melee;
                var particles = ScriptableObject.CreateInstance<ParticleWeaponDefinition>();
                particles.displayName = "Mouse Flamer";
                particles.tags = WeaponTags.DPS | WeaponTags.Fire;
                var pulse = ScriptableObject.CreateInstance<PulseWeaponDefinition>();
                pulse.displayName = "Pulse Blaster";
                generatedAssets.Add(basic);
                generatedAssets.Add(melee);
                generatedAssets.Add(particles);
                generatedAssets.Add(pulse);
                weapons = new WeaponDefinition[] { basic, melee, particles, pulse };
            }
            if (passiveItems == null || passiveItems.Length == 0)
            {
                var shoes = ScriptableObject.CreateInstance<StatPassiveItemDefinition>();
                shoes.displayName = "Running Shoes";
                var roomDamage = ScriptableObject.CreateInstance<RoomDamagePassiveItemDefinition>();
                roomDamage.displayName = "Room Shock";
                var revival = ScriptableObject.CreateInstance<RevivePassiveItemDefinition>();
                revival.displayName = "Spare Life";
                var retaliation = ScriptableObject.CreateInstance<RetaliationPassiveItemDefinition>();
                retaliation.displayName = "Static Charm";
                var inhaler = ScriptableObject.CreateInstance<InhalerPassiveItemDefinition>();
                inhaler.displayName = "Inhalador";
                var spikes = ScriptableObject.CreateInstance<SpikedCoverPassiveItemDefinition>();
                spikes.displayName = "Cubierta de pinchos";
                var karmaBalance = ScriptableObject.CreateInstance<KarmaBalancePassiveItemDefinition>();
                karmaBalance.displayName = "Balanza de karma";
                var healthBalance = ScriptableObject.CreateInstance<HealthBalancePassiveItemDefinition>();
                healthBalance.displayName = "Balanza de vida";
                generatedAssets.Add(shoes);
                generatedAssets.Add(roomDamage);
                generatedAssets.Add(revival);
                generatedAssets.Add(retaliation);
                generatedAssets.Add(inhaler);
                generatedAssets.Add(spikes);
                generatedAssets.Add(karmaBalance);
                generatedAssets.Add(healthBalance);
                passiveItems = new PassiveItemDefinition[] { shoes, roomDamage, revival, retaliation, inhaler, spikes, karmaBalance, healthBalance };
            }
            var allPassives = new List<PassiveItemDefinition>(passiveItems);
            foreach (ReactivePassiveItemDefinition passive in Resources.LoadAll<ReactivePassiveItemDefinition>("PassiveItems"))
                if (passive != null && !allPassives.Contains(passive)) allPassives.Add(passive);
            passiveItems = allPassives.ToArray();
        }

    }
}
