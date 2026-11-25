using System;
using System.Collections.Generic;
using System.Reflection;
using MouseKeeper;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;
using Object = UnityEngine.Object;

public sealed class SquadRecoveryTests
{
    [Serializable] sealed class BalanceTestReport
    {
        public string outcome;
        public float duration_game_seconds, duration_real_seconds;
        public BalanceTestAgent[] agents;
        public BalanceTestRoom[] rooms;
    }
    [Serializable] sealed class BalanceTestAgent
    {
        public int attacks, initial_money, money_spent;
        public float healing_received;
    }
    [Serializable] sealed class BalanceTestRoom { public float duration_game_seconds, duration_real_seconds; }
    const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    GameObject runObject;
    GameObject cameraObject;
    GameRun run;
    DamageIndicators indicators;
    SquadIndicators squadIndicators;
    WeaponDefinition weapon;
    readonly List<GameObject> objects = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        runObject = new GameObject("Test run");
        cameraObject = new GameObject("Test camera");
        run = runObject.AddComponent<GameRun>();
        indicators = runObject.AddComponent<DamageIndicators>();
        Camera camera = cameraObject.AddComponent<Camera>();
        indicators.Initialize(camera);
        squadIndicators = runObject.AddComponent<SquadIndicators>();
        squadIndicators.Initialize(camera);
        typeof(GameRun).GetField("damageIndicators", PrivateInstance).SetValue(run, indicators);
        typeof(GameRun).GetField("squadIndicators", PrivateInstance).SetValue(run, squadIndicators);
        weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
    }

    [TearDown]
    public void TearDown()
    {
        var canvas = (Canvas)typeof(DamageIndicators).GetField("canvas", PrivateInstance).GetValue(indicators);
        if (canvas != null) Object.DestroyImmediate(canvas.gameObject);
        var squadCanvas = (Canvas)typeof(SquadIndicators).GetField("canvas", PrivateInstance).GetValue(squadIndicators);
        if (squadCanvas != null) Object.DestroyImmediate(squadCanvas.gameObject);
        foreach (GameObject obj in objects)
            if (obj != null) Object.DestroyImmediate(obj);
        objects.Clear();
        Object.DestroyImmediate(weapon);
        Object.DestroyImmediate(runObject);
        Object.DestroyImmediate(cameraObject);
    }

    PlayerController CreateAgent(string name, Vector3 position, AgentProfile profile)
    {
        var root = new GameObject(name);
        root.transform.position = position;
        objects.Add(root);
        var agent = root.AddComponent<PlayerController>();
        agent.Initialize(run, profile);
        agent.Equip(new WeaponInstance(weapon));
        var agents = (List<PlayerController>)typeof(GameRun).GetField("agents", PrivateInstance).GetValue(run);
        agents.Add(agent);
        return agent;
    }

    [Test]
    public void FallenAgentSharesMoneyAndDropsOffensiveWeaponForBiggestUpgrade()
    {
        object room = CreateRoom();
        GameObject roomRoot = (GameObject)room.GetType().GetField("root").GetValue(room);
        PlayerController fallen = CreateAgent("Fallen", Vector3.zero, new AgentProfile());
        PlayerController first = CreateAgent("First", Vector3.right, new AgentProfile());
        PlayerController second = CreateAgent("Second", Vector3.right * 2f, new AgentProfile());
        var strong = ScriptableObject.CreateInstance<WeaponDefinition>();
        try
        {
            strong.tags = WeaponTags.DPS;
            strong.damage = weapon.damage * 3f;
            fallen.Equip(new WeaponInstance(strong));
            fallen.AddMoney(5);
            fallen.DebugKill();
            Assert.That(first.Money, Is.EqualTo(3));
            Assert.That(second.Money, Is.EqualTo(2));
            Assert.That(fallen.Money, Is.Zero);
            Assert.That(fallen.Weapons[0], Is.Null);
            ItemPickup dropped = Array.Find(roomRoot.GetComponentsInChildren<ItemPickup>(), item => item.IsFallenWeapon);
            Assert.That(dropped, Is.Not.Null);
            Assert.That(run.PickupOwner(dropped), Is.SameAs(first));
            strong.tags |= WeaponTags.Support;
            Assert.That(run.PickupOwner(dropped), Is.Null);
        }
        finally { Object.DestroyImmediate(strong); }
    }

    [Test]
    public void BalanceReportPersistsAgentActionsAndRoomTimes()
    {
        string directory = System.IO.Path.Combine(Application.temporaryCachePath, "BalanceTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            CreateRoom();
            PlayerController agent = CreateAgent("Test", Vector3.zero, new AgentProfile());
            agent.AddMoney(5);
            typeof(GameRun).GetMethod("InitializeBalanceTelemetry", PrivateInstance).Invoke(run, new object[] { directory });
            typeof(GameRun).GetMethod("TickBalanceTelemetry", PrivateInstance).Invoke(run, new object[] { 2.5f, 2f });
            typeof(PlayerController).GetProperty("Health").SetValue(agent, agent.MaxHealth - 10f);
            agent.Heal(4f, source: "test");
            Assert.That(agent.TrySpendMoney(2), Is.True);
            Assert.That(agent.Weapons[0].TrySpend(1f, 0.5f), Is.True);
            typeof(GameRun).GetMethod("FinishBalanceTelemetry", PrivateInstance).Invoke(run, new object[] { "completed" });
            string path = (string)typeof(GameRun).GetField("balancePath", PrivateInstance).GetValue(run);
            Assert.That(System.IO.File.Exists(path), Is.True);
            string json = System.IO.File.ReadAllText(path);
            BalanceTestReport report = JsonUtility.FromJson<BalanceTestReport>(json);
            Assert.That(report.outcome, Is.EqualTo("completed"));
            Assert.That(report.duration_game_seconds, Is.EqualTo(2.5f));
            Assert.That(report.duration_real_seconds, Is.EqualTo(2f));
            Assert.That(report.agents[0].initial_money, Is.EqualTo(5));
            Assert.That(report.agents[0].money_spent, Is.EqualTo(2));
            Assert.That(report.agents[0].attacks, Is.EqualTo(1));
            Assert.That(report.agents[0].healing_received, Is.EqualTo(4f));
            Assert.That(report.rooms[0].duration_game_seconds, Is.EqualTo(2.5f));
            Assert.That(report.rooms[0].duration_real_seconds, Is.EqualTo(2f));
            StringAssert.Contains("\"weapon_activation\"", json);
            StringAssert.Contains("\"heal\"", json);
        }
        finally
        {
            if (System.IO.Directory.Exists(directory)) System.IO.Directory.Delete(directory, true);
        }
    }

    [Test]
    public void AutoRunPrioritizesMoneyOverOtherEligibleLoot()
    {
        object room = CreateRoom();
        room.GetType().GetField("cleared").SetValue(room, true);
        GameObject roomRoot = (GameObject)room.GetType().GetField("root").GetValue(room);
        PlayerController agent = CreateAgent("Collector", Vector3.zero, new AgentProfile());
        CreateAgent("Teammate", Vector3.left * 3f, new AgentProfile());
        typeof(PlayerController).GetProperty("Karma").SetValue(agent, 5);
        var moneyObject = new GameObject("Cash");
        moneyObject.transform.SetParent(roomRoot.transform);
        moneyObject.transform.position = Vector3.right * 5f;
        moneyObject.AddComponent<MoneyVial>();
        ItemPickup cash = moneyObject.AddComponent<ItemPickup>();
        var weaponObject = new GameObject("Better weapon");
        weaponObject.transform.SetParent(roomRoot.transform);
        WeaponDefinition better = ScriptableObject.CreateInstance<WeaponDefinition>();
        better.damage = weapon.damage * 4f;
        ItemPickup weaponPickup = weaponObject.AddComponent<ItemPickup>();
        weaponPickup.weapon = new WeaponInstance(better);
        FieldInfo autoRun = typeof(ExpeditionSession).GetField("<AutoRun>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        bool previous = (bool)autoRun.GetValue(null);
        try
        {
            autoRun.SetValue(null, false);
            Assert.That(run.SelectPickup(agent), Is.SameAs(weaponPickup));
            autoRun.SetValue(null, true);
            Assert.That(run.SelectPickup(agent), Is.SameAs(cash));
        }
        finally { autoRun.SetValue(null, previous); Object.DestroyImmediate(better); }
    }

    [Test]
    public void RecoveryCanKeepOrCancelTheAgentsMoveOrder()
    {
        PlayerController agent = CreateAgent("Stuck", Vector3.zero, new AgentProfile());
        agent.SetDestination(Vector3.right * 5f);
        MethodInfo recover = typeof(PlayerController).GetMethod("RecoverNavigationPosition", PrivateInstance);
        recover.Invoke(agent, new object[] { Vector3.right, false });
        Assert.That(agent.transform.position, Is.EqualTo(Vector3.right));
        Assert.That(agent.ReachedDestination, Is.False);
        recover.Invoke(agent, new object[] { Vector3.right * 2f, true });
        Assert.That(agent.ReachedDestination, Is.True);
    }

    [Test]
    public void MedicGetsHealthPickupUnlessAnAllyIsCritical()
    {
        object room = CreateRoom();
        GameObject roomRoot = (GameObject)room.GetType().GetField("root").GetValue(room);
        PlayerController medic = CreateAgent("Medic", Vector3.zero, new AgentProfile());
        PlayerController ally = CreateAgent("Ally", Vector3.right, new AgentProfile());
        medic.SetSpecialty(Specialty.Medic);
        typeof(PlayerController).GetProperty("Health").SetValue(medic, medic.MaxHealth * 0.6f);
        typeof(PlayerController).GetProperty("Health").SetValue(ally, ally.MaxHealth * 0.5f);
        var healthObject = new GameObject("Health");
        objects.Add(healthObject);
        healthObject.transform.SetParent(roomRoot.transform);
        healthObject.AddComponent<HealthVial>();
        ItemPickup pickup = healthObject.AddComponent<ItemPickup>();
        typeof(ItemPickup).GetMethod("Awake", PrivateInstance).Invoke(pickup, null);
        Assert.That(run.PickupOwner(pickup), Is.SameAs(medic));
        typeof(PlayerController).GetProperty("Health").SetValue(ally, ally.MaxHealth * 0.2f);
        Assert.That(run.PickupOwner(pickup), Is.SameAs(ally));
    }

    [Test]
    public void PurchaseAndPickupScoresAreAddedTogether()
    {
        FieldInfo score = typeof(GameRun).GetField("score", PrivateInstance);
        Assert.That(score.GetValue(run), Is.EqualTo(1000));
        run.RecordPickupScore(PickupKind.Passive, 6);
        run.RecordPickupScore(PickupKind.Weapon, 0);
        run.RecordPickupScore(PickupKind.Health, 3);
        Assert.That(score.GetValue(run), Is.EqualTo(1170));
    }

    [Test]
    public void CriticalDamageNumbersAreRedAndNormalNumbersAreWhite()
    {
        indicators.Show(Vector3.zero, 10f, false);
        indicators.Show(Vector3.zero, 15f, false, true);
        var numbers = (System.Collections.IList)typeof(DamageIndicators).GetField("indicators", PrivateInstance).GetValue(indicators);
        FieldInfo color = numbers[0].GetType().GetField("color");
        Assert.That(color.GetValue(numbers[0]), Is.EqualTo(Color.white));
        Assert.That(color.GetValue(numbers[1]), Is.EqualTo(Color.red));
    }

    object CreateRoom()
    {
        Type type = typeof(GameRun).GetNestedType("Room", BindingFlags.NonPublic);
        object room = Activator.CreateInstance(type, true);
        var root = new GameObject("Test room");
        objects.Add(root);
        type.GetField("root").SetValue(room, root);
        type.GetField("width").SetValue(room, 18f);
        type.GetField("depthSize").SetValue(room, 14f);
        typeof(GameRun).GetField("currentRoom", PrivateInstance).SetValue(run, room);
        return room;
    }

    void ClearRoom(object room)
    {
        typeof(GameRun).GetField("currentRoom", PrivateInstance).SetValue(run, room);
        typeof(GameRun).GetMethod("CompleteCombatRoom", PrivateInstance).Invoke(run, new[] { room });
    }

    [Test]
    public void LootChangesKarmaForCollectorAndLivingTeammates()
    {
        PlayerController collector = CreateAgent("Collector", Vector3.zero, new AgentProfile());
        PlayerController teammate = CreateAgent("Teammate", Vector3.right * 2f, new AgentProfile());
        PlayerController fallen = CreateAgent("Fallen", Vector3.left * 2f, new AgentProfile());
        fallen.TakeDamage(10000f);
        collector.NotifyLootCollected();
        collector.NotifyLootCollected();
        Assert.That(collector.Karma, Is.EqualTo(-2));
        Assert.That(teammate.Karma, Is.EqualTo(2));
        Assert.That(fallen.Karma, Is.Zero);
        teammate.NotifyLootCollected();
        Assert.That(collector.Karma, Is.EqualTo(-1));
        Assert.That(teammate.Karma, Is.EqualTo(1));
    }

    [Test]
    public void ExpeditionEditorDebugHealsAdjustsKarmaAndKillsOneLivingAgent()
    {
        CreateRoom();
        PlayerController first = CreateAgent("First", Vector3.zero, new AgentProfile());
        PlayerController second = CreateAgent("Second", Vector3.right * 2f, new AgentProfile());
        first.TakeDamage(20f);
        MethodInfo debug = typeof(GameRun).GetMethod("ApplyEditorDebug", PrivateInstance);
        debug.Invoke(run, new object[] { 1 });
        Assert.That(first.Health, Is.EqualTo(first.MaxHealth));
        debug.Invoke(run, new object[] { 2 });
        Assert.That(first.Karma, Is.EqualTo(5));
        Assert.That(second.Karma, Is.EqualTo(5));
        debug.Invoke(run, new object[] { 3 });
        Assert.That(first.Karma, Is.Zero);
        Assert.That(second.Karma, Is.Zero);
        var revival = ScriptableObject.CreateInstance<RevivePassiveItemDefinition>();
        try
        {
            first.AddPassive(revival);
            second.AddPassive(revival);
            debug.Invoke(run, new object[] { 6 });
            Assert.That((first.Dead ? 1 : 0) + (second.Dead ? 1 : 0), Is.EqualTo(1));
        }
        finally { Object.DestroyImmediate(revival); }
    }

    [Test]
    public void EditorDebugClearsEvenWhenRoomWaveIsStillSpawning()
    {
        object room = CreateRoom();
        PlayerController agent = CreateAgent("Agent", Vector3.zero, new AgentProfile());
        typeof(GameRun).GetField("player", PrivateInstance).SetValue(run, agent);
        var root = (GameObject)room.GetType().GetField("root").GetValue(room);
        var ratObject = new GameObject("Rat");
        ratObject.transform.SetParent(root.transform);
        var rat = ratObject.AddComponent<MouseEnemy>();
        rat.Initialize(run, agent, Vector3.zero, 1);
        room.GetType().GetField("spawning").SetValue(room, true);
        room.GetType().GetField("enemies").SetValue(room, 5);
        LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
        typeof(GameRun).GetMethod("ApplyEditorDebug", PrivateInstance).Invoke(run, new object[] { 4 });
        Assert.That((bool)room.GetType().GetField("spawning").GetValue(room), Is.False);
        Assert.That((int)room.GetType().GetField("enemies").GetValue(room), Is.Zero);
        Assert.That((bool)room.GetType().GetField("cleared").GetValue(room), Is.True);
    }

    [TestCase(1, 0.8f, 10f)]
    [TestCase(10, 2f, 3f)]
    public void NegativeKarmaTriggersPushTowardEnemyWithDistrustScaling(int distrust, float distance, float cooldown)
    {
        CreateRoom();
        PlayerController collector = CreateAgent("Collector", Vector3.zero, new AgentProfile());
        PlayerController teammate = CreateAgent("Teammate", Vector3.left, new AgentProfile { distrust = distrust });
        var enemyObject = new GameObject("Enemy");
        objects.Add(enemyObject);
        enemyObject.transform.position = Vector3.right * 5f;
        MouseEnemy enemy = enemyObject.AddComponent<MouseEnemy>();
        MethodInfo push = typeof(PlayerController).GetMethod("TryPushLootHoarder", PrivateInstance);
        MethodInfo move = typeof(PlayerController).GetMethod("MoveDuringPush", PrivateInstance);

        Assert.That(run.SelectLootHoarder(teammate), Is.Null);
        Assert.That((bool)push.Invoke(teammate, new object[] { enemy, 0.1f }), Is.False);
        run.RecordLoot(collector);
        Assert.That(run.SelectLootHoarder(teammate), Is.SameAs(collector));
        Assert.That((bool)push.Invoke(teammate, new object[] { enemy, 0.1f }), Is.True);
        Assert.That(collector.BeingPushed, Is.True);
        Assert.That(run.SelectLootHoarder(teammate), Is.Null);
        Assert.That((float)typeof(PlayerController).GetField("pushCooldown", PrivateInstance).GetValue(teammate),
            Is.EqualTo(cooldown).Within(0.001f));
        Assert.That((bool)move.Invoke(collector, new object[] { 0.4f }), Is.True);
        Assert.That(collector.transform.position.x, Is.EqualTo(distance).Within(0.02f));
        Assert.That(collector.BeingPushed, Is.False);
        Assert.That((bool)push.Invoke(teammate, new object[] { enemy, 0.1f }), Is.False);
    }

    [Test]
    public void ReprimandTargetsOnlyBelowMinusFourAndResetsAfterRecovery()
    {
        PlayerController collector = CreateAgent("Collector", Vector3.zero, new AgentProfile());
        PlayerController teammate = CreateAgent("Teammate", Vector3.left, new AgentProfile());
        for (int i = 0; i < 4; i++) run.RecordLoot(collector);
        Assert.That(collector.Karma, Is.EqualTo(-4));
        Assert.That(run.SelectReprimandTarget(teammate), Is.Null);
        run.RecordLoot(collector);
        Assert.That(run.SelectReprimandTarget(teammate), Is.SameAs(collector));
        var reprimanded = (HashSet<PlayerController>)typeof(PlayerController).GetField("reprimanded", PrivateInstance).GetValue(teammate);
        reprimanded.Add(collector);
        Assert.That(run.SelectReprimandTarget(teammate), Is.Null);
        run.RecordLoot(teammate);
        Assert.That(collector.Karma, Is.EqualTo(-4));
        run.RecordLoot(collector);
        Assert.That(run.SelectReprimandTarget(teammate), Is.SameAs(collector));
    }

    [Test]
    public void GreedyAgentsLoseMoreKarmaForLoot()
    {
        PlayerController greedy = CreateAgent("Greedy", Vector3.zero, new AgentProfile { greed = 10 });
        PlayerController modest = CreateAgent("Modest", Vector3.right, new AgentProfile { greed = 1 });
        run.RecordLoot(greedy);
        Assert.That(greedy.Karma, Is.EqualTo(-3));
        run.RecordLoot(modest);
        Assert.That(modest.Karma, Is.Zero);
        Assert.That(greedy.Karma, Is.EqualTo(-2));
    }

    [Test]
    public void VeryLowKarmaBlocksCashButNotOtherPickups()
    {
        object room = CreateRoom();
        GameObject roomRoot = (GameObject)room.GetType().GetField("root").GetValue(room);
        PlayerController collector = CreateAgent("Collector", Vector3.zero, new AgentProfile { greed = 10 });
        PlayerController teammate = CreateAgent("Teammate", Vector3.right, new AgentProfile { greed = 1 });
        var cashObject = new GameObject("Cash");
        objects.Add(cashObject);
        cashObject.transform.SetParent(roomRoot.transform);
        cashObject.AddComponent<MoneyVial>();
        ItemPickup cash = cashObject.AddComponent<ItemPickup>();
        typeof(ItemPickup).GetMethod("Awake", PrivateInstance).Invoke(cash, null);
        typeof(PlayerController).GetProperty("Karma").SetValue(collector, -7);
        Assert.That(collector.Karma, Is.EqualTo(-7));
        Assert.That(cash.CanPickUp(collector), Is.True);
        Assert.That(run.PickupOwner(cash), Is.SameAs(collector));
        typeof(PlayerController).GetProperty("Karma").SetValue(collector, -8);
        Assert.That(collector.Karma, Is.EqualTo(-8));
        Assert.That(cash.CanPickUp(collector), Is.False);
        cash.PickUp(collector);
        Assert.That(collector.Money, Is.Zero);
        Assert.That(cash.Available, Is.True);
        Assert.That(run.PickupOwner(cash), Is.SameAs(teammate));

        var healthObject = new GameObject("Health");
        objects.Add(healthObject);
        healthObject.AddComponent<HealthVial>();
        ItemPickup health = healthObject.AddComponent<ItemPickup>();
        typeof(ItemPickup).GetMethod("Awake", PrivateInstance).Invoke(health, null);
        collector.TakeDamage(1f);
        Assert.That(health.CanPickUp(collector), Is.True);
        run.RecordLoot(teammate);
        Assert.That(collector.Karma, Is.EqualTo(-7));
        Assert.That(cash.CanPickUp(collector), Is.True);
    }

    [Test]
    public void LastSurvivorCollectsCashDespiteNegativeKarmaWithoutLosingKarma()
    {
        object room = CreateRoom();
        room.GetType().GetField("cleared").SetValue(room, true);
        PlayerController survivor = CreateAgent("Survivor", Vector3.zero, new AgentProfile { greed = 10 });
        PlayerController teammate = CreateAgent("Teammate", Vector3.right, new AgentProfile());
        var cashObject = new GameObject("Cash");
        objects.Add(cashObject);
        cashObject.transform.SetParent(((GameObject)room.GetType().GetField("root").GetValue(room)).transform);
        cashObject.AddComponent<MoneyVial>();
        ItemPickup cash = cashObject.AddComponent<ItemPickup>();
        typeof(ItemPickup).GetMethod("Awake", PrivateInstance).Invoke(cash, null);
        typeof(PlayerController).GetProperty("Karma").SetValue(survivor, -20);
        Assert.That(cash.CanPickUp(survivor), Is.False);
        teammate.TakeDamage(10000f);
        Assert.That(run.SelectPickup(survivor), Is.SameAs(cash));
        LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
        cash.PickUp(survivor);
        Assert.That(survivor.Money, Is.EqualTo(1));
        Assert.That(survivor.Karma, Is.EqualTo(-20));
    }

    [Test]
    public void LastSurvivorMovesTwentyFivePercentFasterInCombatAndTravel()
    {
        CreateRoom();
        PlayerController survivor = CreateAgent("Survivor", Vector3.zero, new AgentProfile { speed = 4f });
        PlayerController teammate = CreateAgent("Teammate", Vector3.forward * 3f, new AgentProfile { speed = 4f });
        MethodInfo move = typeof(PlayerController).GetMethod("MoveTowards", PrivateInstance);
        move.Invoke(survivor, new object[] { Vector3.right * 10f, 0.1f });
        float normal = survivor.transform.position.x;
        teammate.TakeDamage(10000f);
        survivor.transform.position = Vector3.zero;
        Physics.SyncTransforms();
        typeof(PlayerController).GetField("stamina", PrivateInstance).SetValue(survivor, 1f);
        move.Invoke(survivor, new object[] { Vector3.right * 10f, 0.1f });
        Assert.That(survivor.transform.position.x, Is.EqualTo(normal * 1.25f).Within(0.001f));
        survivor.transform.position = Vector3.zero;
        Physics.SyncTransforms();
        typeof(GameRun).GetProperty("IsTransitioning").SetValue(run, true);
        move.Invoke(survivor, new object[] { Vector3.right * 10f, 0.1f });
        Assert.That(survivor.transform.position.x, Is.EqualTo(4f * 1.5f * 1.25f * 0.1f).Within(0.001f));
    }

    [TestCase(0.49f, true)]
    [TestCase(0.5f, false)]
    public void BossRewardDependsOnLowestLivingHealth(float fraction, bool expectHealth)
    {
        object room = CreateRoom();
        GameObject roomRoot = (GameObject)room.GetType().GetField("root").GetValue(room);
        PlayerController weakest = CreateAgent("Weakest", Vector3.zero, new AgentProfile());
        PlayerController healthier = CreateAgent("Healthier", Vector3.right, new AgentProfile { greed = 10 });
        typeof(PlayerController).GetProperty("Health").SetValue(weakest, weakest.MaxHealth * fraction);
        typeof(PlayerController).GetProperty("Health").SetValue(healthier, healthier.MaxHealth * 0.75f);
        typeof(PlayerController).GetProperty("Karma").SetValue(healthier, 100);
        var passive = ScriptableObject.CreateInstance<StatPassiveItemDefinition>();
        var prefab = new GameObject("Health prefab");
        objects.Add(prefab);
        prefab.AddComponent<BoxCollider>().isTrigger = true;
        prefab.AddComponent<HealthVial>();
        prefab.AddComponent<ItemPickup>();
        run.healthVialPrefab = prefab;
        run.passiveItems = new PassiveItemDefinition[] { passive };
        try
        {
            if (!expectHealth) LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
            typeof(GameRun).GetMethod("SpawnBossReward", PrivateInstance).Invoke(run, new object[] { Vector3.zero });
            ItemPickup[] rewards = roomRoot.GetComponentsInChildren<ItemPickup>();
            Assert.That(rewards.Length, Is.EqualTo(1));
            typeof(ItemPickup).GetMethod("Awake", PrivateInstance).Invoke(rewards[0], null);
            Assert.That(rewards[0].Kind, Is.EqualTo(expectHealth ? PickupKind.Health : PickupKind.Passive));
            if (expectHealth)
            {
                Assert.That(rewards[0].CanPickUp(healthier), Is.False);
                Assert.That(rewards[0].CanPickUp(weakest), Is.True);
                Assert.That(run.PickupOwner(rewards[0]), Is.SameAs(weakest));
            }
        }
        finally { Object.DestroyImmediate(passive); }
    }

    [TestCase(0, true)]
    [TestCase(1, true)]
    [TestCase(2, false)]
    public void OnlyEarlierFloorBossesDropTheGuaranteedReward(int floor, bool dropsReward)
    {
        object room = CreateRoom();
        GameObject roomRoot = (GameObject)room.GetType().GetField("root").GetValue(room);
        PlayerController agent = CreateAgent("Agent", Vector3.zero, new AgentProfile());
        typeof(PlayerController).GetProperty("Health").SetValue(agent, agent.MaxHealth * 0.49f);
        var prefab = new GameObject("Health prefab");
        objects.Add(prefab);
        prefab.AddComponent<BoxCollider>().isTrigger = true;
        prefab.AddComponent<HealthVial>();
        prefab.AddComponent<ItemPickup>();
        run.healthVialPrefab = prefab;
        var bossObject = new GameObject("Boss");
        bossObject.transform.SetParent(roomRoot.transform);
        MouseEnemy boss = bossObject.AddComponent<MouseEnemy>();
        room.GetType().GetField("boss").SetValue(room, boss);
        room.GetType().GetField("enemies").SetValue(room, 2);
        room.GetType().GetField("spawning").SetValue(room, true);
        typeof(GameRun).GetField("levelIndex", PrivateInstance).SetValue(run, floor);
        if (dropsReward)
            for (int i = 0; i < 6; i++)
                LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
        run.MouseDefeated(boss);
        Assert.That(roomRoot.GetComponentsInChildren<ItemPickup>().Length, Is.EqualTo(dropsReward ? 1 : 0));
    }

    [Test]
    public void GreedCanClaimOnceBeforeTheHighestKarmaAgentTakesTheRest()
    {
        object room = CreateRoom();
        room.GetType().GetField("cleared").SetValue(room, true);
        GameObject roomRoot = (GameObject)room.GetType().GetField("root").GetValue(room);
        PlayerController greedy = CreateAgent("Greedy", Vector3.right * 4f, new AgentProfile { greed = 8 });
        PlayerController virtuous = CreateAgent("Virtuous", Vector3.zero, new AgentProfile { greed = 1 });
        var cashObject = new GameObject("Cash");
        objects.Add(cashObject);
        cashObject.transform.SetParent(roomRoot.transform);
        cashObject.transform.position = Vector3.right * 4f;
        cashObject.AddComponent<MoneyVial>();
        ItemPickup cash = cashObject.AddComponent<ItemPickup>();
        typeof(ItemPickup).GetMethod("Awake", PrivateInstance).Invoke(cash, null);
        var healthObject = new GameObject("Health");
        objects.Add(healthObject);
        healthObject.transform.SetParent(roomRoot.transform);
        healthObject.AddComponent<HealthVial>();
        ItemPickup health = healthObject.AddComponent<ItemPickup>();
        typeof(ItemPickup).GetMethod("Awake", PrivateInstance).Invoke(health, null);
        virtuous.TakeDamage(1f);

        typeof(PlayerController).GetProperty("Karma").SetValue(greedy, 7);
        typeof(PlayerController).GetProperty("Karma").SetValue(virtuous, 14);
        Assert.That(run.PickupOwner(cash), Is.SameAs(greedy));
        Assert.That(run.SelectPickup(virtuous), Is.SameAs(health));
        Assert.That(run.IsGreedClaim(cash, greedy), Is.True);
        run.RecordLoot(greedy, 1, true);
        for (int i = 0; i < 5; i++)
        {
            Assert.That(run.PickupOwner(cash), Is.SameAs(virtuous));
            Assert.That(run.SelectPickup(virtuous), Is.SameAs(cash));
            run.RecordLoot(virtuous);
        }
        Assert.That(run.PickupOwner(cash), Is.SameAs(greedy));
        virtuous.TakeDamage(10000f);
        Assert.That(run.PickupOwner(cash), Is.SameAs(greedy));
    }

    [Test]
    public void HighKarmaClaimsWeaponsEvenWithoutAnUpgradeAndAvoidsRepeatedSwaps()
    {
        object room = CreateRoom();
        room.GetType().GetField("cleared").SetValue(room, true);
        GameObject roomRoot = (GameObject)room.GetType().GetField("root").GetValue(room);
        PlayerController virtuous = CreateAgent("Virtuous", Vector3.zero, new AgentProfile { greed = 1 });
        PlayerController greedy = CreateAgent("Greedy", Vector3.right, new AgentProfile { greed = 10 });
        typeof(PlayerController).GetProperty("Karma").SetValue(greedy, -16);
        var weak = ScriptableObject.CreateInstance<WeaponDefinition>();
        try
        {
            weak.damage = weapon.damage * 0.5f;
            greedy.Weapons[0] = new WeaponInstance(weak);
            var item = new GameObject("Weapon");
            objects.Add(item);
            item.transform.SetParent(roomRoot.transform);
            item.transform.position = Vector3.right * 3f;
            ItemPickup pickup = item.AddComponent<ItemPickup>();
            pickup.weapon = new WeaponInstance(weapon);
            var healthObject = new GameObject("Health");
            objects.Add(healthObject);
            healthObject.transform.SetParent(roomRoot.transform);
            healthObject.AddComponent<HealthVial>();
            ItemPickup health = healthObject.AddComponent<ItemPickup>();
            typeof(ItemPickup).GetMethod("Awake", PrivateInstance).Invoke(health, null);
            virtuous.TakeDamage(1f);
            typeof(PlayerController).GetProperty("Karma").SetValue(virtuous, 3);
            Assert.That(run.PickupOwner(pickup), Is.SameAs(greedy));
            typeof(PlayerController).GetProperty("Karma").SetValue(virtuous, 4);
            Assert.That(virtuous.WantsWeapon(pickup.weapon), Is.False);
            Assert.That(run.PickupOwner(pickup), Is.SameAs(virtuous));
            Assert.That(run.SelectPickup(virtuous), Is.SameAs(pickup));

            pickup.SetPrice(5, null);
            Assert.That(run.PickupOwner(pickup), Is.Null);
            virtuous.AddMoney(5);
            Assert.That(run.PickupOwner(pickup), Is.SameAs(virtuous));
            WeaponInstance previous = virtuous.Weapons[0];
            virtuous.Equip(pickup.weapon);
            Assert.That(virtuous.CanAutoEquip(previous), Is.False);
            ItemPickup dropped = Array.Find(roomRoot.GetComponentsInChildren<ItemPickup>(), p => p.weapon == previous);
            Assert.That(dropped, Is.Not.Null);
            Assert.That(run.PickupOwner(dropped), Is.SameAs(greedy));
            Assert.That(virtuous.CanAutoEquip(pickup.weapon), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(weak);
        }
    }

    [Test]
    public void WeaponsDoNotAffectKarmaAndPassivesCostThree()
    {
        PlayerController collector = CreateAgent("Collector", Vector3.zero, new AgentProfile());
        PlayerController teammate = CreateAgent("Teammate", Vector3.right, new AgentProfile());
        collector.NotifyLootCollected(PickupKind.Weapon);
        Assert.That(collector.Karma, Is.Zero);
        Assert.That(teammate.Karma, Is.Zero);
        collector.NotifyLootCollected(PickupKind.Passive);
        collector.NotifyLootCollected(PickupKind.Health);
        collector.NotifyLootCollected(PickupKind.Money);
        Assert.That(collector.Karma, Is.EqualTo(-5));
        Assert.That(teammate.Karma, Is.EqualTo(3));
    }

    [Test]
    public void BalanceItemsResetKarmaAndEqualizeHealthWithoutHealingFromNothing()
    {
        PlayerController first = CreateAgent("First", Vector3.zero, new AgentProfile { health = 100, speed = 4 });
        PlayerController second = CreateAgent("Second", Vector3.right, new AgentProfile { health = 100, speed = 4 });
        PlayerController third = CreateAgent("Third", Vector3.left, new AgentProfile { health = 100, speed = 4 });
        first.TakeDamage(60);
        second.TakeDamage(30);
        first.NotifyLootCollected();
        var karma = ScriptableObject.CreateInstance<KarmaBalancePassiveItemDefinition>();
        var health = ScriptableObject.CreateInstance<HealthBalancePassiveItemDefinition>();
        try
        {
            karma.OnPickUp(first);
            Assert.That(first.Karma, Is.Zero);
            Assert.That(second.Karma, Is.Zero);
            Assert.That(third.Karma, Is.Zero);
            health.OnPickUp(first);
            Assert.That(first.Health, Is.EqualTo(70).Within(0.001f));
            Assert.That(second.Health, Is.EqualTo(70).Within(0.001f));
            Assert.That(third.Health, Is.EqualTo(70).Within(0.001f));
            Assert.That(karma.ConsumeOnPickup && health.ConsumeOnPickup, Is.True);
        }
        finally { Object.DestroyImmediate(karma); Object.DestroyImmediate(health); }
    }

    [Test]
    public void ShotgunHitsTheSameRatAtMostOnce()
    {
        CreateRoom();
        PlayerController agent = CreateAgent("Agent", Vector3.zero, new AgentProfile { speed = 4 });
        var enemyObject = new GameObject("Wide rat");
        objects.Add(enemyObject);
        enemyObject.transform.position = Vector3.forward * 3f;
        var enemy = enemyObject.AddComponent<MouseEnemy>();
        enemy.GetComponent<CharacterController>().radius = 1.5f;
        enemy.Initialize(run, agent, Vector3.zero, 1);
        Physics.SyncTransforms();
        var shotgun = ScriptableObject.CreateInstance<ShotgunWeaponDefinition>();
        shotgun.damage = 15f;
        try
        {
            float before = (float)typeof(MouseEnemy).GetField("health", PrivateInstance).GetValue(enemy);
            shotgun.PrimaryAttack(agent, new WeaponInstance(shotgun));
            float after = (float)typeof(MouseEnemy).GetField("health", PrivateInstance).GetValue(enemy);
            Assert.That(before - after, Is.EqualTo(15f));
        }
        finally { Object.DestroyImmediate(shotgun); }
    }

    [Test]
    public void SpikedCoverOnlyDamagesTheAttackingRat()
    {
        CreateRoom();
        PlayerController agent = CreateAgent("Agent", Vector3.zero, new AgentProfile { speed = 4 });
        var enemyObject = new GameObject("Attacker");
        objects.Add(enemyObject);
        var enemy = enemyObject.AddComponent<MouseEnemy>();
        enemy.Initialize(run, agent, Vector3.zero, 1);
        var spikes = ScriptableObject.CreateInstance<SpikedCoverPassiveItemDefinition>();
        try
        {
            agent.AddPassive(spikes);
            float before = (float)typeof(MouseEnemy).GetField("health", PrivateInstance).GetValue(enemy);
            agent.TakeDamage(20f, enemy);
            Assert.That((float)typeof(MouseEnemy).GetField("health", PrivateInstance).GetValue(enemy), Is.EqualTo(before - 5f));
            agent.TakeDamage(20f);
            Assert.That((float)typeof(MouseEnemy).GetField("health", PrivateInstance).GetValue(enemy), Is.EqualTo(before - 5f));
        }
        finally { Object.DestroyImmediate(spikes); }
    }

    [Test]
    public void BlowerPushesAndDealsDamageAndWhipRetreatsWithFixedCooldown()
    {
        CreateRoom();
        PlayerController agent = CreateAgent("Agent", Vector3.zero, new AgentProfile { speed = 4, attackSpeed = 2 });
        var enemyObject = new GameObject("Rat");
        objects.Add(enemyObject);
        enemyObject.transform.position = Vector3.forward * 2f;
        var enemy = enemyObject.AddComponent<MouseEnemy>();
        enemy.Initialize(run, agent, Vector3.zero, 1);
        enemyObject.transform.SetParent(run.CurrentRoom, true);
        Physics.SyncTransforms();
        var blower = ScriptableObject.CreateInstance<BlowerWeaponDefinition>();
        var whipDefinition = ScriptableObject.CreateInstance<WhipWeaponDefinition>();
        try
        {
            blower.range = 5f;
            blower.damage = 10f;
            blower.attackInterval = 0.6f;
            float initialHealth = (float)typeof(MouseEnemy).GetField("health", PrivateInstance).GetValue(enemy);
            blower.PrimaryAttack(agent, new WeaponInstance(blower, 2f));
            Assert.That(enemy.transform.position.z, Is.GreaterThan(2f));
            Assert.That((float)typeof(MouseEnemy).GetField("health", PrivateInstance).GetValue(enemy), Is.EqualTo(initialHealth - 3f).Within(0.001f));

            enemy.transform.position = Vector3.forward * 2f;
            Physics.SyncTransforms();
            whipDefinition.range = 2.5f;
            whipDefinition.attackInterval = 3f;
            var whip = new WeaponInstance(whipDefinition, 2f);
            whipDefinition.PrimaryAttack(agent, whip);
            Assert.That((float)typeof(MouseEnemy).GetField("retreatRemaining", PrivateInstance).GetValue(enemy), Is.EqualTo(2f));
            whip.Tick(2.9f, 0f);
            Assert.That(whip.TrySpend(1f, 3f), Is.False);
            whip.Tick(0.11f, 0f);
            Assert.That(whip.TrySpend(1f, 3f), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(blower);
            Object.DestroyImmediate(whipDefinition);
        }
    }

    [TestCase(10, 9)]
    [TestCase(11, 9)]
    [TestCase(15, 13)]
    [TestCase(16, 13)]
    public void MoneyKarmaCostUsesKarmaBeforePickup(int initial, int expected)
    {
        PlayerController collector = CreateAgent("Collector", Vector3.zero, new AgentProfile());
        PlayerController teammate = CreateAgent("Teammate", Vector3.right, new AgentProfile());
        typeof(PlayerController).GetProperty("Karma").SetValue(collector, initial);
        collector.NotifyLootCollected(PickupKind.Money);
        Assert.That(collector.Karma, Is.EqualTo(expected));
        Assert.That(teammate.Karma, Is.EqualTo(1));
    }

    [Test]
    public void GreedHasAnIncreasedButSingleUseLootAdvantage()
    {
        object room = CreateRoom();
        GameObject root = (GameObject)room.GetType().GetField("root").GetValue(room);
        PlayerController greedy = CreateAgent("Greedy", Vector3.zero, new AgentProfile { greed = 6 });
        PlayerController leader = CreateAgent("Leader", Vector3.right, new AgentProfile { greed = 1 });
        typeof(PlayerController).GetProperty("Karma").SetValue(greedy, 7);
        typeof(PlayerController).GetProperty("Karma").SetValue(leader, 14);
        var cashObject = new GameObject("Cash");
        objects.Add(cashObject);
        cashObject.transform.SetParent(root.transform);
        cashObject.AddComponent<MoneyVial>();
        ItemPickup cash = cashObject.AddComponent<ItemPickup>();
        typeof(ItemPickup).GetMethod("Awake", PrivateInstance).Invoke(cash, null);
        Assert.That(run.PickupOwner(cash), Is.SameAs(greedy));
        greedy.NotifyLootCollected(PickupKind.Money, run.IsGreedClaim(cash, greedy));
        Assert.That(run.PickupOwner(cash), Is.SameAs(leader));
    }

    [Test]
    public void ClearingCombatHealsOnlyTheMostInjuredLivingAgentOnce()
    {
        object room = CreateRoom();
        PlayerController weakest = CreateAgent("Weakest", Vector3.zero, new AgentProfile { health = 200f });
        PlayerController other = CreateAgent("Other", Vector3.right, new AgentProfile { health = 100f });
        PlayerController fallen = CreateAgent("Fallen", Vector3.left, new AgentProfile());
        typeof(PlayerController).GetProperty("Health").SetValue(weakest, 60f);
        typeof(PlayerController).GetProperty("Health").SetValue(other, 50f);
        fallen.TakeDamage(10000f);
        ClearRoom(room);
        Assert.That(weakest.Health, Is.EqualTo(70f));
        Assert.That(other.Health, Is.EqualTo(50f));
        Assert.That(fallen.Dead, Is.True);
        ClearRoom(room);
        Assert.That(weakest.Health, Is.EqualTo(70f));
    }

    [Test]
    public void ShopMedkitUsesOnlyTheShortfallAndDoesNotChangeKarma()
    {
        object room = CreateRoom();
        Type type = room.GetType();
        Transform root = ((GameObject)type.GetField("root").GetValue(room)).transform;
        type.GetField("kind").SetValue(room, Enum.Parse(type.GetField("kind").FieldType, "Shop"));
        PlayerController buyer = CreateAgent("Buyer", Vector3.zero, new AgentProfile());
        PlayerController first = CreateAgent("First", Vector3.right, new AgentProfile());
        PlayerController second = CreateAgent("Second", Vector3.left, new AgentProfile());
        buyer.TakeDamage(75f);
        buyer.AddMoney(1);
        first.AddMoney(1);

        var kit = new GameObject("Medkit");
        kit.transform.SetParent(root);
        kit.AddComponent<HealthVial>();
        var pickup = kit.AddComponent<ItemPickup>();
        typeof(ItemPickup).GetMethod("Awake", PrivateInstance).Invoke(pickup, null);
        pickup.SetPrice(3, null);

        Assert.That(run.PickupOwner(pickup), Is.Null);
        pickup.PickUp(buyer);
        Assert.That(buyer.Money, Is.EqualTo(1));
        Assert.That(first.Money, Is.EqualTo(1));
        Assert.That(pickup.Available, Is.True);

        second.AddMoney(4);
        Assert.That(run.PickupOwner(pickup), Is.SameAs(buyer));
        LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
        pickup.PickUp(buyer);
        Assert.That(buyer.Health, Is.EqualTo(50f));
        Assert.That(buyer.Money, Is.Zero);
        Assert.That(first.Money, Is.Zero);
        Assert.That(second.Money, Is.EqualTo(3));
        Assert.That(buyer.Karma, Is.Zero);
        Assert.That(first.Karma, Is.Zero);
        Assert.That(second.Karma, Is.Zero);
    }

    [Test]
    public void ShopRevivalRequiresAVisitedShopAnAffordableBuyerAndGreedBelowSeven()
    {
        object room = CreateRoom();
        Type type = room.GetType();
        type.GetField("kind").SetValue(room, Enum.Parse(type.GetField("kind").FieldType, "Shop"));
        PlayerController buyer = CreateAgent("Buyer", Vector3.zero, new AgentProfile { greed = 6 });
        PlayerController greedy = CreateAgent("Greedy", Vector3.right, new AgentProfile { greed = 7 });
        PlayerController fallen = CreateAgent("Fallen", Vector3.left, new AgentProfile());
        WeaponInstance equipped = fallen.Weapons[0];
        fallen.AddMoney(3);
        fallen.TakeDamage(10000f);
        greedy.AddMoney(30);
        buyer.AddMoney(9);
        MethodInfo buy = typeof(GameRun).GetMethod("TryBuyShopRevival", PrivateInstance);
        Assert.That((bool)buy.Invoke(run, null), Is.False);
        type.GetField("visited").SetValue(room, true);
        Assert.That((bool)buy.Invoke(run, null), Is.False);
        Assert.That(greedy.Money, Is.EqualTo(30));
        buyer.AddMoney(1);
        Assert.That((bool)buy.Invoke(run, null), Is.True);
        Assert.That(buyer.Money, Is.Zero);
        Assert.That(fallen.Dead, Is.False);
        Assert.That(fallen.Health, Is.EqualTo(fallen.MaxHealth * 0.5f));
        Assert.That(fallen.Money, Is.EqualTo(3));
        Assert.That(fallen.Weapons[0], Is.SameAs(equipped));
        Assert.That((bool)buy.Invoke(run, null), Is.False);
    }

    [Test]
    public void RescueRequiresTwoRecentlyAttackedInjuredAgentsAndUsesTeamwork()
    {
        object room = CreateRoom();
        GameObject root = (GameObject)room.GetType().GetField("root").GetValue(room);
        PlayerController victim = CreateAgent("Victim", Vector3.zero, new AgentProfile { teamwork = 3 });
        PlayerController injured = CreateAgent("Injured", Vector3.forward * 4f, new AgentProfile { teamwork = 4 });
        PlayerController rescuer = CreateAgent("Rescuer", Vector3.right, new AgentProfile { teamwork = 9 });
        typeof(PlayerController).GetProperty("Health").SetValue(victim, 25f);
        typeof(PlayerController).GetProperty("Health").SetValue(injured, 40f);
        var mouseObject = new GameObject("Victim attacker");
        objects.Add(mouseObject);
        mouseObject.transform.SetParent(root.transform);
        mouseObject.transform.position = Vector3.left;
        MouseEnemy mouse = mouseObject.AddComponent<MouseEnemy>();
        var otherObject = new GameObject("Other attacker");
        objects.Add(otherObject);
        otherObject.transform.SetParent(root.transform);
        otherObject.transform.position = injured.transform.position + Vector3.left;
        MouseEnemy other = otherObject.AddComponent<MouseEnemy>();
        mouse.Initialize(run, victim, Vector3.left * 3f, 1);
        other.Initialize(run, injured, injured.transform.position + Vector3.left * 3f, 1);
        Physics.SyncTransforms();
        MethodInfo mouseUpdate = typeof(MouseEnemy).GetMethod("Update", PrivateInstance);
        mouseUpdate.Invoke(mouse, null);
        Assert.That(mouse.RecentlyAttacked(victim), Is.True);
        Assert.That(run.TrySelectRescue(rescuer, out _, out _), Is.False);
        mouseUpdate.Invoke(other, null);
        Assert.That(other.RecentlyAttacked(injured), Is.True);
        FieldInfo lastTime = typeof(MouseEnemy).GetField("lastAttackAt", PrivateInstance);
        lastTime.SetValue(other, Time.time - 2.01f);
        Assert.That(run.TrySelectRescue(rescuer, out _, out _), Is.False);
        lastTime.SetValue(other, Time.time);
        float injuredHealth = injured.Health;
        typeof(PlayerController).GetProperty("Health").SetValue(injured, injured.MaxHealth * 0.41f);
        Assert.That(run.TrySelectRescue(rescuer, out _, out _), Is.False);
        typeof(PlayerController).GetProperty("Health").SetValue(injured, injuredHealth);
        rescuer.SetDestination(Vector3.right * 5f);
        Assert.That(run.TrySelectRescue(rescuer, out _, out _), Is.False);
        rescuer.CancelMoveOrder();
        Assert.That(run.TrySelectRescue(injured, out _, out _), Is.False);
        Assert.That(run.TrySelectRescue(rescuer, out PlayerController selected, out MouseEnemy threat), Is.True);
        Assert.That(selected, Is.SameAs(victim));
        Assert.That(threat, Is.SameAs(mouse));
        MethodInfo rescue = typeof(PlayerController).GetMethod("TryRescueAlly", PrivateInstance);
        Assert.That((bool)rescue.Invoke(rescuer, new object[] { selected, threat, 0.1f }), Is.True);
        Assert.That(victim.BeingPushed, Is.True);
        Vector3 velocity = (Vector3)typeof(PlayerController).GetField("pushVelocity", PrivateInstance).GetValue(victim);
        Assert.That(Vector3.Dot(velocity, victim.transform.position - mouse.transform.position), Is.GreaterThan(0f));
        Assert.That((float)typeof(PlayerController).GetField("rescueCooldown", PrivateInstance).GetValue(rescuer),
            Is.EqualTo(5f));
        Assert.That(run.TrySelectRescue(rescuer, out _, out _), Is.False);
        typeof(PlayerController).GetField("pushRemaining", PrivateInstance).SetValue(victim, 0f);
        typeof(PlayerController).GetField("rescueCooldown", PrivateInstance).SetValue(rescuer, 0f);
        weapon.range = 0f;
        typeof(PlayerController).GetMethod("Update", PrivateInstance).Invoke(rescuer, null);
        Assert.That(victim.BeingPushed, Is.True);
    }

    [Test]
    public void HealthPickupRespectsTwentyFivePercentagePointDifference()
    {
        CreateRoom();
        PlayerController healthier = CreateAgent("Healthier", Vector3.zero, new AgentProfile());
        PlayerController injured = CreateAgent("Injured", Vector3.right, new AgentProfile());
        var healthObject = new GameObject("Health");
        objects.Add(healthObject);
        healthObject.AddComponent<HealthVial>();
        ItemPickup health = healthObject.AddComponent<ItemPickup>();
        typeof(ItemPickup).GetMethod("Awake", PrivateInstance).Invoke(health, null);
        healthier.TakeDamage(20f);
        injured.TakeDamage(45f);
        Assert.That(health.CanPickUp(healthier), Is.True);
        injured.TakeDamage(1f);
        Assert.That(health.CanPickUp(healthier), Is.False);
        Assert.That(health.CanPickUp(injured), Is.True);
        injured.TakeDamage(10000f);
        Assert.That(health.CanPickUp(healthier), Is.True);
    }

    [Test]
    public void HealthComparisonKeepsTheBoundaryWithFractionalHealth()
    {
        CreateRoom();
        PlayerController healthier = CreateAgent("Healthier", Vector3.zero, new AgentProfile { health = 157 });
        PlayerController injured = CreateAgent("Injured", Vector3.right, new AgentProfile { health = 157 });
        typeof(PlayerController).GetProperty("Health").SetValue(healthier, healthier.MaxHealth * 0.8f);
        typeof(PlayerController).GetProperty("Health").SetValue(injured, injured.MaxHealth * 0.55f);
        Assert.That(run.CanTakeHealth(healthier), Is.True);
        typeof(PlayerController).GetProperty("Health").SetValue(injured, injured.MaxHealth * 0.54f);
        Assert.That(run.CanTakeHealth(healthier), Is.False);
    }

    [Test]
    public void WeaponOrdersCycleReserveAndAllowReequipping()
    {
        object room = CreateRoom();
        GameObject roomRoot = (GameObject)room.GetType().GetField("root").GetValue(room);
        PlayerController first = CreateAgent("First", Vector3.zero, new AgentProfile());
        PlayerController second = CreateAgent("Second", Vector3.right, new AgentProfile());
        PlayerController third = CreateAgent("Third", Vector3.left, new AgentProfile());
        var item = new GameObject("Weapon");
        objects.Add(item);
        item.transform.SetParent(roomRoot.transform);
        ItemPickup pickup = item.AddComponent<ItemPickup>();
        pickup.weapon = first.Weapons[0];
        Assert.That(first.CanAutoEquip(pickup.weapon), Is.False);
        Assert.That(run.AssignWeaponPickup(pickup), Is.True);
        Assert.That(run.OrderedWeaponFor(first), Is.SameAs(pickup));
        Assert.That(run.PickupOwner(pickup), Is.SameAs(first));
        Assert.That(run.IsGreedClaim(pickup, first), Is.False);
        Assert.That(run.AssignWeaponPickup(pickup), Is.True);
        Assert.That(run.OrderedWeaponFor(first), Is.Null);
        Assert.That(run.OrderedWeaponFor(second), Is.SameAs(pickup));
        Assert.That(run.PickupOwner(pickup), Is.SameAs(second));
        Assert.That(run.AssignWeaponPickup(pickup), Is.True);
        Assert.That(run.OrderedWeaponFor(third), Is.SameAs(pickup));
        third.TakeDamage(10000f);
        Assert.That(run.OrderedWeaponFor(third), Is.Null);
        Assert.That(run.AssignWeaponPickup(pickup), Is.True);
        Assert.That(run.OrderedWeaponFor(first), Is.SameAs(pickup));
        run.WeaponPickupCompleted(pickup);
        Assert.That(run.OrderedWeaponFor(first), Is.Null);
        pickup.SetPrice(5, null);
        second.AddMoney(5);
        Assert.That(run.AssignWeaponPickup(pickup), Is.True);
        Assert.That(run.OrderedWeaponFor(second), Is.SameAs(pickup));
        Assert.That(second.TrySpendMoney(5), Is.True);
        Assert.That(run.OrderedWeaponFor(second), Is.Null);
    }

    [Test]
    public void DoorTravelUsesFastestLivingSpeedWithoutFatigue()
    {
        object room = CreateRoom();
        PlayerController slow = CreateAgent("Slow", Vector3.zero, new AgentProfile { speed = 2f });
        PlayerController fast = CreateAgent("Fast", Vector3.forward * 3f, new AgentProfile { speed = 6f });
        PlayerController fallen = CreateAgent("Fallen", Vector3.back * 3f, new AgentProfile { speed = 8f });
        fallen.TakeDamage(10000f);
        Assert.That(run.SquadTravelSpeed, Is.EqualTo(9f));
        typeof(GameRun).GetProperty("IsTransitioning").SetValue(run, true);
        typeof(PlayerController).GetField("stamina", PrivateInstance).SetValue(slow, 0.35f);
        MethodInfo move = typeof(PlayerController).GetMethod("MoveTowards", PrivateInstance);
        move.Invoke(slow, new object[] { slow.transform.position + Vector3.right * 10f, 0.5f });
        move.Invoke(fast, new object[] { fast.transform.position + Vector3.right * 10f, 0.5f });
        Assert.That(slow.transform.position.x, Is.EqualTo(4.5f).Within(0.02f));
        Assert.That(fast.transform.position.x, Is.EqualTo(4.5f).Within(0.02f));
        Assert.That((float)typeof(PlayerController).GetField("stamina", PrivateInstance).GetValue(slow), Is.EqualTo(0.35f));
        typeof(GameRun).GetProperty("IsTransitioning").SetValue(run, false);
        var doorObject = new GameObject("Open door");
        objects.Add(doorObject);
        RoomDoor door = doorObject.AddComponent<RoomDoor>();
        door.SetOpen(true);
        ((RoomDoor[])room.GetType().GetField("doors").GetValue(room))[0] = door;
        room.GetType().GetField("cleared").SetValue(room, true);
        Assert.That(run.UsesSquadTravelSpeed, Is.True);
        move.Invoke(slow, new object[] { slow.transform.position + Vector3.right * 10f, 0.5f });
        Assert.That(slow.transform.position.x, Is.EqualTo(9f).Within(0.02f));
        room.GetType().GetField("cleared").SetValue(room, false);
        Assert.That(run.UsesSquadTravelSpeed, Is.False);
        move.Invoke(slow, new object[] { slow.transform.position + Vector3.right * 10f, 0.5f });
        Assert.That(slow.transform.position.x, Is.LessThan(9.5f));
    }

    [Test]
    public void FallenAgentsReturnOnFloorTransitionWithTheirEquipment()
    {
        PlayerController first = CreateAgent("First", Vector3.zero, new AgentProfile { health = 160f });
        PlayerController second = CreateAgent("Second", Vector3.right * 2f, new AgentProfile());
        CreateAgent("Survivor", Vector3.left * 2f, new AgentProfile());
        WeaponInstance equipped = first.Weapons[0];
        first.AddMoney(3);
        first.TakeDamage(1000f);
        Assert.That(first.Dead, Is.True);
        Assert.That(first.gameObject.activeSelf, Is.False);

        object firstRoom = CreateRoom();
        ClearRoom(firstRoom);
        Assert.That(first.Dead, Is.True);
        second.TakeDamage(1000f);
        ClearRoom(firstRoom);
        ClearRoom(CreateRoom());
        Assert.That(first.Dead && second.Dead, Is.True);
        typeof(GameRun).GetMethod("ReturnDownedAgents", PrivateInstance).Invoke(run, null);
        Assert.That(first.Dead, Is.False);
        Assert.That(first.gameObject.activeSelf, Is.True);
        Assert.That(first.GetComponent<CharacterController>().enabled, Is.True);
        Assert.That(first.Health, Is.EqualTo(80f));
        Assert.That(first.Money, Is.EqualTo(3));
        Assert.That(first.Weapons[0], Is.SameAs(equipped));
        Assert.That(second.Dead, Is.False);
        Assert.That(second.Health, Is.EqualTo(50f));
    }

    [Test]
    public void FloorRequiresEveryRoomVisitedAndCleared()
    {
        object first = CreateRoom();
        object boss = CreateRoom();
        var rooms = (System.Collections.IList)typeof(GameRun).GetField("rooms", PrivateInstance).GetValue(run);
        rooms.Add(first);
        rooms.Add(boss);
        Type type = first.GetType();
        type.GetField("visited").SetValue(first, true);
        type.GetField("cleared").SetValue(first, true);
        type.GetField("visited").SetValue(boss, true);
        type.GetField("cleared").SetValue(boss, true);
        type.GetField("bossDefeated").SetValue(boss, true);
        typeof(GameRun).GetField("bossRoom", PrivateInstance).SetValue(run, boss);
        var cleared = typeof(GameRun).GetMethod("FloorCleared", PrivateInstance);
        var progress = typeof(GameRun).GetMethod("FloorProgress", PrivateInstance);
        Assert.That(cleared.Invoke(run, null), Is.True);
        Assert.That(progress.Invoke(run, null), Is.EqualTo("Piso 1/3 | Limpieza: 2/2 (100%) | Pisos completos: 1/3 (33%)"));
        type.GetField("visited").SetValue(first, false);
        Assert.That(cleared.Invoke(run, null), Is.False);
        Assert.That(progress.Invoke(run, null), Is.EqualTo("Piso 1/3 | Limpieza: 1/2 (50%) | Pisos completos: 0/3 (0%)"));
        type.GetField("visited").SetValue(first, true);
        type.GetField("cleared").SetValue(first, false);
        Assert.That(cleared.Invoke(run, null), Is.False);
        typeof(GameRun).GetField("levelIndex", PrivateInstance).SetValue(run, 2);
        typeof(GameRun).GetField("fullyClearedFloors", PrivateInstance).SetValue(run, 2);
        Assert.That(progress.Invoke(run, null), Is.EqualTo("Piso 3/3 | Limpieza: 1/2 (50%) | Pisos completos: 2/3 (67%)"));
        type.GetField("cleared").SetValue(first, true);
        Assert.That(progress.Invoke(run, null), Is.EqualTo("Piso 3/3 | Limpieza: 2/2 (100%) | Pisos completos: 3/3 (100%)"));
    }

    [Test]
    public void UnvisitedDoorsStayClosedAndGlowUntilTheRoomIsVisited()
    {
        object first = CreateRoom();
        object next = CreateRoom();
        Type type = first.GetType();
        var doorRoot = new GameObject("Test door");
        var model = new GameObject("Door model");
        objects.Add(doorRoot);
        objects.Add(model);
        RoomDoor door = doorRoot.AddComponent<RoomDoor>();
        door.Initialize(model, model);
        ((Array)type.GetField("doors").GetValue(first)).SetValue(door, 0);
        ((Array)type.GetField("neighbours").GetValue(first)).SetValue(next, 0);
        type.GetField("visited").SetValue(first, true);
        type.GetField("cleared").SetValue(first, true);
        type.GetField("cleared").SetValue(next, true);
        typeof(GameRun).GetField("currentRoom", PrivateInstance).SetValue(run, first);
        MethodInfo update = typeof(GameRun).GetMethod("UpdateDoors", PrivateInstance);
        FieldInfo open = typeof(RoomDoor).GetField("open", PrivateInstance);
        var hint = (GameObject)typeof(RoomDoor).GetField("hint", PrivateInstance).GetValue(door);
        update.Invoke(run, new[] { first });
        Assert.That(open.GetValue(door), Is.False);
        Assert.That(hint.activeSelf, Is.True);
        Assert.That(hint.GetComponentInChildren<Light>().color, Is.EqualTo(Color.white));
        type.GetField("visited").SetValue(next, true);
        update.Invoke(run, new[] { first });
        Assert.That(open.GetValue(door), Is.True);
        Assert.That(hint.activeSelf, Is.False);
        type.GetField("cleared").SetValue(first, false);
        update.Invoke(run, new[] { first });
        Assert.That(open.GetValue(door), Is.False);
        Assert.That(hint.activeSelf, Is.False);
    }

    [Test]
    public void BossVentAllowsLeavingAnIncompleteFloorAfterTheBossRoomIsCleared()
    {
        object boss = CreateRoom();
        object unvisited = CreateRoom();
        Type type = boss.GetType();
        var rooms = (System.Collections.IList)typeof(GameRun).GetField("rooms", PrivateInstance).GetValue(run);
        rooms.Add(boss);
        rooms.Add(unvisited);
        var ventObject = new GameObject("Vent");
        objects.Add(ventObject);
        var vent = ventObject.AddComponent<BoxCollider>();
        vent.isTrigger = true;
        vent.size = Vector3.one * 4f;
        type.GetField("vent").SetValue(boss, vent);
        type.GetField("bossDefeated").SetValue(boss, true);
        type.GetField("visited").SetValue(boss, true);
        type.GetField("cleared").SetValue(boss, true);
        typeof(GameRun).GetField("bossRoom", PrivateInstance).SetValue(run, boss);
        typeof(GameRun).GetField("currentRoom", PrivateInstance).SetValue(run, boss);
        CreateAgent("Survivor", Vector3.zero, new AgentProfile());
        Physics.SyncTransforms();
        Assert.That(typeof(GameRun).GetMethod("FloorCleared", PrivateInstance).Invoke(run, null), Is.False);
        Assert.That(typeof(GameRun).GetMethod("CanUseVent", PrivateInstance).Invoke(run, null), Is.True);
        type.GetField("bossDefeated").SetValue(boss, false);
        Assert.That(typeof(GameRun).GetMethod("CanUseVent", PrivateInstance).Invoke(run, null), Is.False);
    }

    [Test]
    public void ClearedRoomRouteRejectsPathsThroughUnclearedRooms()
    {
        object first = CreateRoom();
        object middle = CreateRoom();
        object destination = CreateRoom();
        Type type = first.GetType();
        foreach (object room in new[] { first, middle, destination })
        {
            type.GetField("visited").SetValue(room, true);
            type.GetField("cleared").SetValue(room, true);
        }
        ((Array)type.GetField("neighbours").GetValue(first)).SetValue(middle, 0);
        ((Array)type.GetField("neighbours").GetValue(middle)).SetValue(first, 2);
        ((Array)type.GetField("neighbours").GetValue(middle)).SetValue(destination, 0);
        ((Array)type.GetField("neighbours").GetValue(destination)).SetValue(middle, 2);
        typeof(GameRun).GetField("currentRoom", PrivateInstance).SetValue(run, first);
        MethodInfo route = typeof(GameRun).GetMethod("TravelClearedRoute", PrivateInstance);
        var available = (System.Collections.IEnumerator)route.Invoke(run, new[] { destination });
        Assert.That(available.MoveNext(), Is.True);
        Assert.That(available.Current, Is.InstanceOf<System.Collections.IEnumerator>());
        type.GetField("cleared").SetValue(middle, false);
        var blocked = (System.Collections.IEnumerator)route.Invoke(run, new[] { destination });
        Assert.That(blocked.MoveNext(), Is.False);
        Assert.That(typeof(GameRun).GetField("message", PrivateInstance).GetValue(run),
            Is.EqualTo("No hay un camino despejado hasta esa sala."));
    }

    [Test]
    public void MinimapResolvesTheClickedRoom()
    {
        var canvasObject = new GameObject("Map canvas", typeof(RectTransform), typeof(Canvas));
        objects.Add(canvasObject);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var mapObject = new GameObject("Map", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Minimap));
        mapObject.transform.SetParent(canvasObject.transform, false);
        mapObject.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 200f);
        Minimap map = mapObject.GetComponent<Minimap>();
        map.SetRoom(Vector2Int.zero, Vector2.zero, Vector2.one * 8f, Color.white);
        map.SetRoom(Vector2Int.right, Vector2.right * 20f, Vector2.one * 8f, Color.white);
        Canvas.ForceUpdateCanvases();
        map.LayoutRooms();
        Vector2 click = RectTransformUtility.WorldToScreenPoint(null, mapObject.transform.GetChild(1).position);
        Assert.That(map.ContainsScreenPoint(click), Is.True);
        Assert.That(map.TryGetRoomAtScreenPoint(click, out Vector2Int room), Is.True);
        Assert.That(room, Is.EqualTo(Vector2Int.right));
    }

    [Test]
    public void BasicWeaponIsNotGeneratedWhenEveryoneCarriesIt()
    {
        var catalog = ScriptableObject.CreateInstance<ExpeditionCatalog>();
        var alternative = ScriptableObject.CreateInstance<WeaponDefinition>();
        FieldInfo catalogField = typeof(ExpeditionSession).GetField("catalog", BindingFlags.Static | BindingFlags.NonPublic);
        object previous = catalogField.GetValue(null);
        try
        {
            catalog.basicWeapon = weapon;
            catalog.weapons = new[] { weapon, alternative };
            catalogField.SetValue(null, catalog);
            run.weapons = catalog.weapons;
            for (int i = 0; i < 3; i++)
                CreateAgent("Agent " + i, Vector3.right * i, new AgentProfile());
            var randomWeapon = typeof(GameRun).GetMethod("RandomWeapon", PrivateInstance);
            for (int i = 0; i < 30; i++)
                Assert.That(randomWeapon.Invoke(run, null), Is.SameAs(alternative));
        }
        finally
        {
            catalogField.SetValue(null, previous);
            Object.DestroyImmediate(alternative);
            Object.DestroyImmediate(catalog);
        }
    }

    [Test]
    public void InjuredCowardStillFiresAtAnEnemyInRange()
    {
        object room = CreateRoom();
        PlayerController agent = CreateAgent("Coward", Vector3.zero,
            new AgentProfile { bravery = 1, accuracy = 10 });
        CreateAgent("Survivor", Vector3.right * 4f, new AgentProfile());
        var mouseObject = new GameObject("Test mouse");
        objects.Add(mouseObject);
        mouseObject.transform.SetParent(((GameObject)room.GetType().GetField("root").GetValue(room)).transform);
        mouseObject.transform.position = Vector3.forward * 3f;
        var mouse = mouseObject.AddComponent<MouseEnemy>();
        CharacterController controller = mouse.GetComponent<CharacterController>();
        controller.center = Vector3.up * 0.5f;
        controller.height = 1f;
        mouse.Initialize(run, agent, Vector3.forward * 4f, 1);
        var targetCollider = mouseObject.AddComponent<BoxCollider>();
        targetCollider.center = Vector3.up * 0.5f;
        agent.TakeDamage(80f);
        Physics.SyncTransforms();

        typeof(PlayerController).GetMethod("Update", PrivateInstance).Invoke(agent, null);

        float remainingHealth = (float)typeof(MouseEnemy).GetField("health", PrivateInstance).GetValue(mouse);
        Assert.That(remainingHealth, Is.EqualTo(66f));
        Assert.That(agent.Weapons[0].Energy, Is.EqualTo(11f));
        Assert.That(agent.Dead, Is.False);
    }

    [Test]
    public void DefeatedSquadEndsRunWithoutCountingFurtherRooms()
    {
        PlayerController first = CreateAgent("First", Vector3.zero, new AgentProfile());
        PlayerController second = CreateAgent("Second", Vector3.right * 2f, new AgentProfile());
        PlayerController third = CreateAgent("Third", Vector3.left * 2f, new AgentProfile());
        first.TakeDamage(1000f);
        second.TakeDamage(1000f);
        third.TakeDamage(1000f);
        Assert.That(run.Finished, Is.True);

        ClearRoom(CreateRoom());
        ClearRoom(CreateRoom());
        Assert.That(first.Dead && second.Dead && third.Dead, Is.True);
    }

    [TestCase(3.5f, -1)]
    [TestCase(1f, 1)]
    [TestCase(0f, 1)]
    [TestCase(2.1f, 0)]
    public void MeleeAgentMaintainsDistanceWithinAttackRange(float distance, int direction)
    {
        CreateRoom();
        PlayerController agent = CreateAgent("Melee", Vector3.back * distance, new AgentProfile { bravery = 10 });
        typeof(PlayerController).GetMethod("MaintainMeleeDistance", PrivateInstance)
            .Invoke(agent, new object[] { Vector3.zero, 2.5f, 0.1f });
        float newDistance = new Vector2(agent.transform.position.x, agent.transform.position.z).magnitude;
        if (direction < 0) Assert.That(newDistance, Is.LessThan(distance));
        else if (direction > 0) Assert.That(newDistance, Is.GreaterThan(distance));
        else Assert.That(newDistance, Is.EqualTo(distance).Within(0.001f));
        if (distance < 2.5f) Assert.That(newDistance, Is.LessThanOrEqualTo(2.5f));
    }

    [Test]
    public void MeleeAgentAttacksAndKitesWhenMouseIsClose()
    {
        object room = CreateRoom();
        PlayerController agent = CreateAgent("Melee", Vector3.back, new AgentProfile { bravery = 10, accuracy = 10 });
        var melee = ScriptableObject.CreateInstance<MeleeWeaponDefinition>();
        try
        {
            melee.pushDistance = 0f;
            agent.Weapons[0] = new WeaponInstance(melee);
            var mouseObject = new GameObject("Test mouse");
            objects.Add(mouseObject);
            mouseObject.transform.SetParent(((GameObject)room.GetType().GetField("root").GetValue(room)).transform);
            var mouse = mouseObject.AddComponent<MouseEnemy>();
            var mouseCollider = mouseObject.AddComponent<BoxCollider>();
            mouseCollider.center = Vector3.up * 0.5f;
            mouse.Initialize(run, agent, Vector3.forward, 1);
            Physics.SyncTransforms();

            typeof(PlayerController).GetMethod("Update", PrivateInstance).Invoke(agent, null);

            Assert.That((float)typeof(MouseEnemy).GetField("health", PrivateInstance).GetValue(mouse), Is.EqualTo(66f));
            Assert.That(agent.Weapons[0].Energy, Is.EqualTo(11f));
            if (Time.deltaTime > 0f)
                Assert.That(agent.transform.position.z, Is.LessThan(-1f));
        }
        finally
        {
            Object.DestroyImmediate(melee);
        }
    }

    [Test]
    public void MeleeAgentFollowsPlayerOrderInsteadOfKiting()
    {
        object room = CreateRoom();
        PlayerController agent = CreateAgent("Melee", Vector3.back, new AgentProfile { bravery = 10 });
        var melee = ScriptableObject.CreateInstance<MeleeWeaponDefinition>();
        try
        {
            agent.Weapons[0] = new WeaponInstance(melee);
            var mouseObject = new GameObject("Test mouse");
            objects.Add(mouseObject);
            mouseObject.transform.SetParent(((GameObject)room.GetType().GetField("root").GetValue(room)).transform);
            var mouse = mouseObject.AddComponent<MouseEnemy>();
            mouse.Initialize(run, agent, Vector3.forward, 1);
            agent.SetDestination(Vector3.left * 5f + Vector3.back);
            Physics.SyncTransforms();

            typeof(PlayerController).GetMethod("Update", PrivateInstance).Invoke(agent, null);

            if (Time.deltaTime > 0f)
            {
                Assert.That(agent.transform.position.x, Is.LessThan(0f));
                Assert.That(agent.transform.position.z, Is.EqualTo(-1f).Within(0.001f));
            }
        }
        finally
        {
            Object.DestroyImmediate(melee);
        }
    }

    [Test]
    public void AgentIndicatorTracksHealthDeathAndReturn()
    {
        PlayerController agent = CreateAgent("Luna", Vector3.forward * 10f, new AgentProfile { name = "Luna" });
        CreateAgent("Survivor", Vector3.right * 2f, new AgentProfile());
        squadIndicators.Register(agent);
        var canvas = (Canvas)typeof(SquadIndicators).GetField("canvas", PrivateInstance).GetValue(squadIndicators);
        var ring = agent.GetComponentInChildren<AgentHealthRing>();
        var label = canvas.GetComponentInChildren<Text>();
        var update = typeof(SquadIndicators).GetMethod("LateUpdate", PrivateInstance);
        update.Invoke(squadIndicators, null);
        Assert.That(label.text, Is.EqualTo("Luna"));
        Assert.That(ring.FillAmount, Is.EqualTo(1f));
        Assert.That(ring.transform.parent, Is.SameAs(agent.transform));
        Assert.That(ring.HealthColor, Is.EqualTo(new Color32(70, 220, 100, 255)));
        foreach (Vector3 vertex in ring.GetComponent<MeshFilter>().sharedMesh.vertices)
            Assert.That(vertex.y, Is.EqualTo(0f));

        agent.TakeDamage(40f);
        update.Invoke(squadIndicators, null);
        Assert.That(ring.FillAmount, Is.EqualTo(0.6f).Within(0.001f));
        Assert.That(ring.HealthColor, Is.EqualTo(new Color32(255, 210, 45, 255)));
        ring.FillAmount = 0.61f;
        Assert.That(ring.HealthColor, Is.EqualTo(new Color32(70, 220, 100, 255)));
        ring.FillAmount = 0.31f;
        Assert.That(ring.HealthColor, Is.EqualTo(new Color32(255, 210, 45, 255)));
        ring.FillAmount = 0.3f;
        Assert.That(ring.HealthColor, Is.EqualTo(new Color32(240, 65, 55, 255)));
        agent.TakeDamage(1000f);
        update.Invoke(squadIndicators, null);
        Assert.That(ring.gameObject.activeInHierarchy, Is.False);
        agent.ReturnToSquad(Vector3.forward * 10f);
        update.Invoke(squadIndicators, null);
        Assert.That(ring.gameObject.activeInHierarchy, Is.True);
        Assert.That(ring.FillAmount, Is.EqualTo(0.5f).Within(0.001f));
        Mesh mesh = ring.GetComponent<MeshFilter>().sharedMesh;
        Assert.That(mesh.triangles.Length, Is.EqualTo((64 + 32) * 6));
        ring.FillAmount = 0f;
        Assert.That(mesh.triangles.Length, Is.EqualTo(64 * 6));
    }

    [Test]
    public void AgentOcclusionCopiesModelAndFollowsItsVisibility()
    {
        var root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        objects.Add(root);
        Renderer source = root.GetComponent<Renderer>();
        var indicator = root.AddComponent<AgentOcclusionIndicator>();
        indicator.Initialize();
        var overlay = root.transform.Find("Occluded Agent Silhouette").GetComponent<Renderer>();
        Assert.That(overlay.sharedMaterial.shader.name, Is.EqualTo("MouseKeeper/AgentOccluded"));
        Assert.That(overlay.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(root.GetComponent<MeshFilter>().sharedMesh));
        source.enabled = false;
        typeof(AgentOcclusionIndicator).GetMethod("LateUpdate", PrivateInstance).Invoke(indicator, null);
        Assert.That(overlay.enabled, Is.False);
    }

    [TestCase(1, 10f)]
    [TestCase(5, 3f)]
    [TestCase(10, 0f)]
    public void FearDurationMatchesBravery(int bravery, float expected)
    {
        Assert.That(PlayerController.FearDuration(bravery), Is.EqualTo(expected));
    }

    [Test]
    public void TacticalRetreatKeepsAccurateAndInaccurateAgentsWithinWeaponRange()
    {
        const float range = 15f;
        float low = PlayerController.RetreatDistance(1, range);
        float high = PlayerController.RetreatDistance(10, range);
        Assert.That(low, Is.GreaterThan(0f));
        Assert.That(high, Is.GreaterThan(low).And.LessThan(range));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void FrightenedAgentsMayObeyCommandsButCriticalAgentsFleeDuringPanic(bool succeeds)
    {
        object room = CreateRoom();
        PlayerController agent = CreateAgent("Timid", Vector3.zero, new AgentProfile { bravery = 1, accuracy = 10 });
        CreateAgent("Ally", Vector3.right * 5f, new AgentProfile());
        var mouseObject = new GameObject("Test mouse");
        objects.Add(mouseObject);
        mouseObject.transform.SetParent(((GameObject)room.GetType().GetField("root").GetValue(room)).transform);
        mouseObject.transform.position = Vector3.forward * 3f;
        var mouse = mouseObject.AddComponent<MouseEnemy>();
        mouse.Initialize(run, agent, Vector3.forward * 4f, 1);
        agent.TakeDamage(60f);

        UnityEngine.Random.State previous = UnityEngine.Random.state;
        try
        {
            int seed = 0;
            float roll;
            do
            {
                UnityEngine.Random.InitState(seed++);
                roll = UnityEngine.Random.value;
            } while ((roll < 0.1f) != succeeds);
            UnityEngine.Random.InitState(seed - 1);
            agent.IssueMoveOrder(Vector3.right * 4f);
            Assert.That(agent.ReachedDestination, Is.EqualTo(!succeeds));
            Assert.That((bool)typeof(PlayerController).GetField("overcomingFear", PrivateInstance).GetValue(agent),
                Is.EqualTo(succeeds));
            Physics.SyncTransforms();
            typeof(PlayerController).GetMethod("Update", PrivateInstance).Invoke(agent, null);
            if (Time.deltaTime > 0f)
            {
                if (succeeds) Assert.That(agent.transform.position.x, Is.GreaterThan(0f));
                else Assert.That(agent.transform.position.z, Is.LessThan(0f));
            }
            if (succeeds)
            {
                agent.TakeDamage(20f);
                typeof(PlayerController).GetMethod("Update", PrivateInstance).Invoke(agent, null);
                Assert.That(agent.ReachedDestination, Is.True);
                Assert.That((bool)typeof(PlayerController).GetField("overcomingFear", PrivateInstance).GetValue(agent), Is.False);
                agent.IssueMoveOrder(Vector3.right * 4f);
                Assert.That(agent.ReachedDestination, Is.True);
                Assert.That((bool)typeof(PlayerController).GetField("overcomingFear", PrivateInstance).GetValue(agent), Is.False);
            }
        }
        finally
        {
            UnityEngine.Random.state = previous;
        }
    }

    [Test]
    public void LastSurvivorObeysEvenWhenCriticallyInjured()
    {
        object room = CreateRoom();
        PlayerController agent = CreateAgent("Timid", Vector3.zero, new AgentProfile { bravery = 1 });
        PlayerController ally = CreateAgent("Ally", Vector3.right * 5f, new AgentProfile());
        var mouseObject = new GameObject("Test mouse");
        objects.Add(mouseObject);
        mouseObject.transform.SetParent(((GameObject)room.GetType().GetField("root").GetValue(room)).transform);
        mouseObject.transform.position = Vector3.forward * 3f;
        var mouse = mouseObject.AddComponent<MouseEnemy>();
        mouse.Initialize(run, agent, Vector3.forward * 4f, 1);
        ally.TakeDamage(1000f);
        agent.TakeDamage(90f);
        agent.IssueMoveOrder(Vector3.right * 4f);
        Assert.That(agent.ReachedDestination, Is.False);
        Physics.SyncTransforms();
        typeof(PlayerController).GetMethod("Update", PrivateInstance).Invoke(agent, null);
        Assert.That(agent.ReachedDestination, Is.False);
    }

    [Test]
    public void FearlessCriticallyInjuredAgentStillObeysOrders()
    {
        object room = CreateRoom();
        PlayerController agent = CreateAgent("Fearless", Vector3.zero, new AgentProfile { bravery = 10 });
        var mouseObject = new GameObject("Test mouse");
        objects.Add(mouseObject);
        mouseObject.transform.SetParent(((GameObject)room.GetType().GetField("root").GetValue(room)).transform);
        var mouse = mouseObject.AddComponent<MouseEnemy>();
        mouse.Initialize(run, agent, Vector3.forward, 1);
        agent.TakeDamage(90f);
        agent.IssueMoveOrder(Vector3.right);
        Assert.That(agent.ReachedDestination, Is.False);
    }

    [Test]
    public void BraveryIncreasesChanceToObeyTheSameOrder()
    {
        object room = CreateRoom();
        PlayerController timid = CreateAgent("Timid", Vector3.zero, new AgentProfile { bravery = 1 });
        PlayerController brave = CreateAgent("Brave", Vector3.right, new AgentProfile { bravery = 5 });
        var mouseObject = new GameObject("Test mouse");
        objects.Add(mouseObject);
        mouseObject.transform.SetParent(((GameObject)room.GetType().GetField("root").GetValue(room)).transform);
        var mouse = mouseObject.AddComponent<MouseEnemy>();
        mouse.Initialize(run, timid, Vector3.forward, 1);
        timid.TakeDamage(70f);
        brave.TakeDamage(70f);
        UnityEngine.Random.State previous = UnityEngine.Random.state;
        try
        {
            int seed = 0;
            float roll;
            do
            {
                UnityEngine.Random.InitState(seed++);
                roll = UnityEngine.Random.value;
            } while (roll < 0.1f || roll >= 0.5f);
            UnityEngine.Random.InitState(seed - 1);
            timid.IssueMoveOrder(Vector3.right * 4f);
            UnityEngine.Random.InitState(seed - 1);
            brave.IssueMoveOrder(Vector3.right * 4f);
            Assert.That(timid.ReachedDestination, Is.True);
            Assert.That(brave.ReachedDestination, Is.False);
        }
        finally
        {
            UnityEngine.Random.state = previous;
        }
    }

    [TestCase("MouseBlaster", typeof(RaytraceWeaponDefinition))]
    [TestCase("MouseSweeper", typeof(MeleeWeaponDefinition))]
    [TestCase("MouseFlamer", typeof(ParticleWeaponDefinition))]
    [TestCase("PulseBlaster", typeof(PulseWeaponDefinition))]
    public void DefaultWeaponAssetsHaveUsableIcons(string file, Type expectedType)
    {
        var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Data/Weapons/" + file + ".asset");
        Assert.That(definition, Is.Not.Null);
        Assert.That(definition, Is.InstanceOf(expectedType));
        Assert.That(definition.displayName, Is.Not.Empty);
        Assert.That(definition.icon, Is.Not.Null);
        Assert.That(definition.damage, Is.GreaterThan(0f));
        Assert.That(new WeaponInstance(definition).Definition, Is.SameAs(definition));
    }

    [TestCase("GasGrenade")]
    [TestCase("Blower")]
    [TestCase("Cheese")]
    [TestCase("Shotgun")]
    [TestCase("Whip")]
    public void SpecialWeaponIconsAreAssignedAndShownOnPickups(string file)
    {
        var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Data/Weapons/" + file + ".asset");
        Assert.That(definition, Is.Not.Null);
        Assert.That(definition.icon, Is.SameAs(Resources.Load<Sprite>("ItemIcons/" + file)));
        Assert.That(definition.icon, Is.Not.Null);
        var root = new GameObject("Pickup");
        try
        {
            ItemPickup pickup = root.AddComponent<ItemPickup>();
            pickup.weapon = new WeaponInstance(definition);
            Assert.That(pickup.DisplayIcon, Is.SameAs(definition.icon));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [TestCase(typeof(InhalerPassiveItemDefinition), "Inhaler")]
    [TestCase(typeof(SpikedCoverPassiveItemDefinition), "SpikedCover")]
    [TestCase(typeof(KarmaBalancePassiveItemDefinition), "KarmaBalance")]
    [TestCase(typeof(HealthBalancePassiveItemDefinition), "HealthBalance")]
    public void NewPassiveIconsHaveDefaultsAndAllowOverrides(Type type, string file)
    {
        var passive = (PassiveItemDefinition)ScriptableObject.CreateInstance(type);
        var root = new GameObject("Pickup");
        try
        {
            Sprite expected = Resources.Load<Sprite>("ItemIcons/" + file);
            Assert.That(expected, Is.Not.Null);
            Assert.That(passive.DisplayIcon, Is.SameAs(expected));
            ItemPickup pickup = root.AddComponent<ItemPickup>();
            pickup.passive = passive;
            Assert.That(pickup.DisplayIcon, Is.SameAs(expected));
            passive.icon = Resources.Load<Sprite>("ItemIcons/Cheese");
            Assert.That(passive.DisplayIcon, Is.SameAs(passive.icon));
            Assert.That(pickup.DisplayIcon, Is.SameAs(passive.icon));
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(passive); }
    }

    [TestCase("MouseBlaster", "Assets/Models/Weapons/poissondart.FBX")]
    [TestCase("MouseSweeper", "Assets/Models/Weapons/sword_mp_1.fbx")]
    [TestCase("MouseFlamer", "Assets/Models/Weapons/flamethrower.FBX")]
    [TestCase("PulseBlaster", "Assets/Models/Weapons/atomblaster.FBX")]
    public void WeaponModelIsShownOnAgentAndPickup(string file, string modelPath)
    {
        CreateRoom();
        var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Data/Weapons/" + file + ".asset");
        Assert.That(definition.modelPrefab, Is.SameAs(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath)));
        var instance = new WeaponInstance(definition);
        PlayerController agent = CreateAgent("Agent", Vector3.zero, new AgentProfile());
        agent.Equip(instance);
        Transform mount = agent.transform.Find("Weapon Mount");
        Assert.That(mount.Find("Weapon Model").GetComponentInChildren<Renderer>(), Is.Not.Null);
        ItemPickup pickup = run.SpawnWeaponPickup(instance, Vector3.forward);
        Assert.That(pickup.transform.Find("Weapon Model").GetComponentInChildren<Renderer>(), Is.Not.Null);
        Assert.That(pickup.weapon, Is.SameAs(instance));
        foreach (Collider collider in mount.GetComponentsInChildren<Collider>())
            Assert.That(collider.enabled, Is.False);
        foreach (Collider collider in pickup.transform.Find("Weapon Model").GetComponentsInChildren<Collider>())
            Assert.That(collider.enabled, Is.False);
        Assert.That(pickup.GetComponent<Collider>().enabled, Is.True);
        Assert.That(pickup.GetComponent<Collider>().isTrigger, Is.True);
    }

    [Test]
    public void MeleeSwingOccursOnlyWhenAnAttackIsSpent()
    {
        CreateRoom();
        var definition = AssetDatabase.LoadAssetAtPath<MeleeWeaponDefinition>("Assets/Data/Weapons/MouseSweeper.asset");
        PlayerController agent = CreateAgent("Agent", Vector3.zero, new AgentProfile());
        var instance = new WeaponInstance(definition);
        agent.Equip(instance);
        Transform mount = agent.transform.Find("Weapon Mount");
        definition.PrimaryAttack(agent, instance);
        Assert.That(Quaternion.Angle(mount.localRotation, Quaternion.identity), Is.GreaterThan(1f));
        mount.localRotation = Quaternion.identity;
        definition.PrimaryAttack(agent, instance);
        Assert.That(Quaternion.Angle(mount.localRotation, Quaternion.identity), Is.LessThan(0.01f));

        agent.Equip(new WeaponInstance(weapon));
        Assert.That(mount.localRotation, Is.EqualTo(Quaternion.identity));
        Assert.That(mount.Find("Weapon Model"), Is.Not.Null);
    }

    [Test]
    public void WeaponMeshOverrideUsesAssignedMaterialForAgentAndPickup()
    {
        CreateRoom();
        GameObject source = GameObject.CreatePrimitive(PrimitiveType.Cube);
        objects.Add(source);
        weapon.modelMesh = source.GetComponent<MeshFilter>().sharedMesh;
        weapon.modelMaterial = source.GetComponent<MeshRenderer>().sharedMaterial;
        PlayerController agent = CreateAgent("Agent", Vector3.zero, new AgentProfile());
        var pickup = run.SpawnWeaponPickup(agent.Weapons[0], Vector3.forward);
        foreach (Transform parent in new[] { agent.transform.Find("Weapon Mount"), pickup.transform })
        {
            Transform model = parent.Find("Weapon Model");
            Assert.That(model.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(weapon.modelMesh));
            Assert.That(model.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(weapon.modelMaterial));
        }
    }

    [Test]
    public void WeaponPrefabDoesNotAddPhysicsToAgentOrPickup()
    {
        CreateRoom();
        GameObject source = GameObject.CreatePrimitive(PrimitiveType.Cube);
        objects.Add(source);
        source.AddComponent<Rigidbody>();
        weapon.modelPrefab = source;
        PlayerController agent = CreateAgent("Agent", Vector3.zero, new AgentProfile());
        var pickup = run.SpawnWeaponPickup(agent.Weapons[0], Vector3.forward);
        foreach (Transform parent in new[] { agent.transform.Find("Weapon Mount"), pickup.transform })
        {
            Transform model = parent.Find("Weapon Model");
            Assert.That(model.GetComponentInChildren<Collider>().enabled, Is.False);
            Assert.That(model.GetComponentInChildren<Rigidbody>().isKinematic, Is.True);
            Assert.That(model.GetComponentInChildren<Rigidbody>().detectCollisions, Is.False);
        }
    }

    [Test]
    public void SampleSceneUsesTheDefaultWeaponAssets()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/SampleScene.unity");
        try
        {
            GameRun sceneRun = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                sceneRun = root.GetComponentInChildren<GameRun>(true);
                if (sceneRun != null) break;
            }
            Assert.That(sceneRun, Is.Not.Null);
            Assert.That(sceneRun.weapons.Length, Is.EqualTo(4));
            foreach (WeaponDefinition definition in sceneRun.weapons)
            {
                Assert.That(definition, Is.Not.Null);
                Assert.That(definition.icon, Is.Not.Null);
            }
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    [Test]
    public void AgentAssetsPersistAndProvideIndependentRuntimeProfiles()
    {
        string weaponPath = AssetDatabase.GenerateUniqueAssetPath("Assets/Tests/Editor/TestAgentWeapon.asset");
        string agentPath = AssetDatabase.GenerateUniqueAssetPath("Assets/Tests/Editor/TestAgent.asset");
        string modelPath = AssetDatabase.GenerateUniqueAssetPath("Assets/Tests/Editor/TestAgentModel.prefab");
        var assetWeapon = ScriptableObject.CreateInstance<WeaponDefinition>();
        var asset = ScriptableObject.CreateInstance<AgentDefinition>();
        var modelObject = new GameObject("Test agent model");
        try
        {
            AssetDatabase.CreateAsset(assetWeapon, weaponPath);
            asset.profile.name = "Persistente";
            asset.profile.health = 160f;
            asset.profile.accuracy = 9;
            asset.modelPrefab = PrefabUtility.SaveAsPrefabAsset(modelObject, modelPath);
            asset.startingWeapon = assetWeapon;
            AssetDatabase.CreateAsset(asset, agentPath);
            AssetDatabase.SaveAssetIfDirty(asset);
            AssetDatabase.ImportAsset(agentPath, ImportAssetOptions.ForceUpdate);
            AgentDefinition loaded = AssetDatabase.LoadAssetAtPath<AgentDefinition>(agentPath);
            Assert.That(loaded.profile.name, Is.EqualTo("Persistente"));
            Assert.That(loaded.profile.health, Is.EqualTo(160f));
            Assert.That(loaded.profile.accuracy, Is.EqualTo(9));
            Assert.That(loaded.startingWeapon, Is.SameAs(assetWeapon));
            Assert.That(loaded.modelPrefab, Is.Not.Null);

            run.weapons = new[] { weapon };
            run.squad = new[] { loaded, loaded, loaded };
            Assert.That((bool)typeof(GameRun).GetMethod("LoadAgentProfiles", PrivateInstance).Invoke(run, null), Is.True);
            var profiles = (AgentProfile[])typeof(GameRun).GetField("profiles", PrivateInstance).GetValue(run);
            Assert.That(profiles[0].name, Is.EqualTo("Persistente"));
            profiles[0].name = "Modificado";
            Assert.That(loaded.profile.name, Is.EqualTo("Persistente"));
            Assert.That(profiles[1].name, Is.EqualTo("Persistente"));

            typeof(GameRun).GetMethod("CreateSquad", PrivateInstance).Invoke(run, null);
            var agents = (List<PlayerController>)typeof(GameRun).GetField("agents", PrivateInstance).GetValue(run);
            foreach (PlayerController agent in agents) objects.Add(agent.gameObject);
            Assert.That(agents.Count, Is.EqualTo(3));
            Assert.That(agents[0].Health, Is.EqualTo(160f));
            Assert.That(agents[0].Weapons[0].Definition, Is.SameAs(assetWeapon));
            Assert.That(agents[0].transform.Find(loaded.modelPrefab.name + "(Clone)"), Is.Not.Null);
        }
        finally
        {
            Object.DestroyImmediate(modelObject);
            AssetDatabase.DeleteAsset(agentPath);
            AssetDatabase.DeleteAsset(weaponPath);
            AssetDatabase.DeleteAsset(modelPath);
        }
    }
}
