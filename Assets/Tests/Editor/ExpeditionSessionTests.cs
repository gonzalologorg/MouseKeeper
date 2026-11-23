using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MouseKeeper;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public sealed class ExpeditionSessionTests
{
    const string Key = "MouseKeeper.Campaign.v1";
    const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
    ExpeditionCatalog catalog;
    CampaignData data;
    GameObject[] roots;
    GameRun run;
    string previousSave;
    bool hadSave;

    [SetUp]
    public void SetUp()
    {
        hadSave = PlayerPrefs.HasKey(Key);
        previousSave = PlayerPrefs.GetString(Key);
        catalog = ScriptableObject.CreateInstance<ExpeditionCatalog>();
        catalog.agents = new AgentDefinition[6];
        for (int i = 0; i < catalog.agents.Length; i++)
        {
            catalog.agents[i] = ScriptableObject.CreateInstance<AgentDefinition>();
            catalog.agents[i].name = "Agent" + i;
            catalog.agents[i].missionCost = 10;
            catalog.agents[i].freeRecruit = i < 4;
            catalog.agents[i].profile.greed = 1;
        }
        catalog.weapons = new WeaponDefinition[2];
        for (int i = 0; i < catalog.weapons.Length; i++)
        {
            catalog.weapons[i] = ScriptableObject.CreateInstance<WeaponDefinition>();
            catalog.weapons[i].name = "Weapon" + i;
        }
        catalog.basicWeapon = catalog.weapons[0];
        data = ExpeditionSession.NewCampaign(catalog);
        typeof(ExpeditionSession).GetField("catalog", StaticPrivate).SetValue(null, catalog);
        typeof(ExpeditionSession).GetField("data", StaticPrivate).SetValue(null, data);
        roots = new GameObject[4];
        roots[0] = new GameObject("Run");
        run = roots[0].AddComponent<GameRun>();
    }

    [TearDown]
    public void TearDown()
    {
        typeof(ExpeditionSession).GetField("catalog", StaticPrivate).SetValue(null, null);
        typeof(ExpeditionSession).GetField("data", StaticPrivate).SetValue(null, null);
        if (hadSave) PlayerPrefs.SetString(Key, previousSave); else PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
        foreach (GameObject root in roots) if (root != null) Object.DestroyImmediate(root);
        foreach (AgentDefinition agent in catalog.agents) Object.DestroyImmediate(agent);
        foreach (WeaponDefinition weapon in catalog.weapons) Object.DestroyImmediate(weapon);
        Object.DestroyImmediate(catalog);
    }

    PlayerController[] Squad(bool firstDead = false)
    {
        var result = new PlayerController[3];
        for (int i = 0; i < result.Length; i++)
        {
            if (roots[i + 1] != null) Object.DestroyImmediate(roots[i + 1]);
            roots[i + 1] = new GameObject("Agent");
            result[i] = roots[i + 1].AddComponent<PlayerController>();
            result[i].Initialize(run, catalog.agents[data.selectedAgents[i]].profile);
            result[i].Weapons[0] = new WeaponInstance(catalog.weapons[data.selectedWeapons[i]]);
        }
        if (firstDead)
            typeof(PlayerController).GetField("<Dead>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(result[0], true);
        return result;
    }

    [TestCase(ExpeditionDifficulty.Easy, 25, 0)]
    [TestCase(ExpeditionDifficulty.Normal, 50, 0)]
    [TestCase(ExpeditionDifficulty.Hard, 75, 1)]
    [TestCase(ExpeditionDifficulty.Swarm, 125, 2)]
    public void DifficultyPersistsAndMultipliesOnlyTheContract(ExpeditionDifficulty difficulty, int reward, int extraRooms)
    {
        Assert.That(ExpeditionSession.Launch(difficulty, out _), Is.True);
        Assert.That(JsonUtility.FromJson<CampaignData>(PlayerPrefs.GetString(Key)).difficulty, Is.EqualTo(difficulty));
        Assert.That(ExpeditionSession.ExtraRooms(difficulty), Is.EqualTo(extraRooms));

        PlayerController[] squad = Squad();
        squad[0].AddMoney(7);
        ExpeditionSession.Settle(ExpeditionSession.FloorCount, squad, true);
        Assert.That(data.lastResultValues[1], Is.EqualTo(reward));
        Assert.That(data.lastResultValues[2], Is.EqualTo(7));
    }

    [Test]
    public void SettlementPaysHiredAgentsAndRecoversOneMissingAgentPerExpedition()
    {
        data.money = 30;
        data.selectedAgents = new[] { 0, 4, 5 };
        Assert.That(ExpeditionSession.Launch(out _), Is.True);
        Assert.That(data.inventory[0], Is.Zero);
        PlayerController[] squad = Squad(true);
        squad[1].AddMoney(10);
        ExpeditionSession.Settle(1, squad, false);
        Assert.That(data.money, Is.EqualTo(35)); // 17 contract + 10 loot - two payments of 11.
        Assert.That(data.inventory[0], Is.Zero);
        Assert.That(data.unavailable[0], Is.GreaterThan(0));
        Assert.That(ExpeditionSession.CanSelect(data, catalog, 0), Is.True);
        data.selectedAgents = new[] { 3, 4, 5 };
        Assert.That(ExpeditionSession.Launch(out _), Is.True);
        ExpeditionSession.Settle(0, Squad(), false);
        Assert.That(data.unavailable[0], Is.Zero);
    }

    [Test]
    public void SelectionRejectsDuplicateAgentsAndInsufficientWeapons()
    {
        data.selectedAgents[2] = 1;
        Assert.That(ExpeditionSession.CanLaunch(data, catalog, out _), Is.False);
        data.selectedAgents[2] = 2;
        data.selectedWeapons[1] = 1;
        Assert.That(ExpeditionSession.CanLaunch(data, catalog, out _), Is.False);
    }

    [Test]
    public void HiringUsesRemainingFundsAndRefreshFiltersTheRoster()
    {
        data.money = 20;
        catalog.agents[3].freeRecruit = false;
        catalog.agents[3].missionCost = 15;
        catalog.agents[4].missionCost = 16;
        catalog.agents[5].missionCost = 5;
        Assert.That(ExpeditionSession.CanHireForSlot(data, catalog, 3, 0), Is.True);
        Assert.That(ExpeditionSession.CanHireForSlot(data, catalog, 4, 0), Is.True);
        data.selectedAgents[0] = 3;
        Assert.That(ExpeditionSession.CanHireForSlot(data, catalog, 4, 1), Is.False);
        Assert.That(ExpeditionSession.CanHireForSlot(data, catalog, 5, 1), Is.True);

        var host = new GameObject("Planning test");
        try
        {
            var controller = host.AddComponent<PlanningController>();
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(PlanningController).GetField("selectedSlot", fields).SetValue(controller, 1);
            typeof(PlanningController).GetMethod("Start", fields).Invoke(controller, null);
            var roster = host.GetComponent<UIDocument>().rootVisualElement.Q<ScrollView>("roster");
            Assert.That(roster.Query<Label>("agent-cost").ToList().Exists(label => label.text == "$16"), Is.False);
            Assert.That(roster.Query<Label>("agent-cost").ToList().Exists(label => label.text == "$5"), Is.True);
            data.selectedMoney[2] = 1;
            Assert.That(ExpeditionSession.CanHireForSlot(data, catalog, 5, 1), Is.False);
        }
        finally { Object.DestroyImmediate(host); }
    }

    [Test]
    public void CareerTotalsSurviveLevelResetAndLossAndClearOnCampaignRestart()
    {
        Assert.That(ExpeditionSession.Launch(out _), Is.True);
        var record = typeof(ExpeditionSession).GetMethod("RecordAgentTotals", StaticPrivate);
        record.Invoke(null, new object[] { 0, 3, 12 });
        AgentProgress saved = JsonUtility.FromJson<CampaignData>(PlayerPrefs.GetString(Key)).progress[0];
        Assert.That(saved.ratsKilled, Is.EqualTo(3));
        Assert.That(saved.moneyCollected, Is.EqualTo(12));

        data.progress[0].accuracy = 2;
        Assert.That(ExpeditionSession.ResetProgress(0).Level, Is.EqualTo(1));
        ExpeditionSession.Settle(1f, Squad(true), false);
        Assert.That(data.progress[0].ratsKilled, Is.EqualTo(3));
        Assert.That(data.progress[0].moneyCollected, Is.EqualTo(12));

        ExpeditionSession.Restart();
        Assert.That(ExpeditionSession.Data.progress[0].ratsKilled, Is.Zero);
        Assert.That(ExpeditionSession.Data.progress[0].moneyCollected, Is.Zero);
    }

    [Test]
    public void CompletedExpeditionsDetermineLevelAndUpgradesHaveTheirOwnLimit()
    {
        data.progress[0].expeditionsCompleted = 9;
        data.progress[0].bravery = 4;
        data.progress[0].agility = 4;
        data.progress[0].pending = 2;
        Assert.That(ExpeditionSession.Upgrade(data, catalog, 0, 1), Is.True);
        Assert.That(ExpeditionSession.Profile(data, catalog, 0).Level, Is.EqualTo(9));
        Assert.That(ExpeditionSession.Upgrade(data, catalog, 0, 1), Is.False);

        Assert.That(ExpeditionSession.Launch(out _), Is.True);
        ExpeditionSession.Settle(ExpeditionSession.FloorCount, Squad(), true);
        Assert.That(ExpeditionSession.Profile(data, catalog, 0).Level, Is.EqualTo(10));

        Assert.That(ExpeditionSession.Launch(out _), Is.True);
        ExpeditionSession.Settle(1f, Squad(), false);
        Assert.That(data.progress[0].expeditionsCompleted, Is.EqualTo(10));

        Assert.That(ExpeditionSession.Launch(out _), Is.True);
        ExpeditionSession.Settle(ExpeditionSession.FloorCount, Squad(), true);
        Assert.That(data.progress[0].expeditionsCompleted, Is.EqualTo(11));
        Assert.That(ExpeditionSession.ResetProgress(0).Level, Is.EqualTo(10));
    }

    [Test]
    public void PlanningUiReflectsRosterInventoryAndMoneyChanges()
    {
        var rootObject = new GameObject("Planning test");
        try
        {
            data.money = 10;
            data.unavailable[4] = 2;
            var controller = rootObject.AddComponent<PlanningController>();
            typeof(PlanningController).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, null);
            Assert.That(controller.enabled, Is.True);
            UIDocument document = rootObject.GetComponent<UIDocument>();
            Assert.That(document, Is.Not.Null);
            VisualElement root = document.rootVisualElement;
            ScrollView roster = root.Q<ScrollView>("roster");
            VisualElement weapons = root.Q<VisualElement>("weapons");
            Assert.That(roster.childCount, Is.EqualTo(catalog.agents.Length - ExpeditionSession.SquadSize));
            Assert.That(weapons.childCount, Is.EqualTo(1));
            Assert.That(roster.contentContainer[1].Q<Button>("agent-button").enabledSelf, Is.False);
            typeof(PlanningController).GetMethod("ChangeMoney", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, new object[] { 1, 1 });
            Assert.That(data.selectedMoney[1], Is.EqualTo(1));
            Assert.That(root.Q<Label>("slot-money-1").text, Does.Contain("$1"));
            Assert.That(root.Q<Label>("funds").text, Does.Contain("Asignados: $1"));
        }
        finally { Object.DestroyImmediate(rootObject); }
    }

    [Test]
    public void DefeatSettlesWithoutLeavingBeforeConfirmation()
    {
        Assert.That(ExpeditionSession.Launch(out _), Is.True);
        PlayerController[] squad = Squad();
        ((List<PlayerController>)typeof(GameRun).GetField("agents", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(run)).AddRange(squad);
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        foreach (PlayerController agent in squad) agent.DebugKill();
        Assert.That(run.Finished, Is.True);
        Assert.That(ExpeditionSession.Active, Is.False);
        Assert.That(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, Is.EqualTo(scene));
        Assert.That(typeof(GameRun).GetField("returnToPlanningAfterDefeat", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(run), Is.EqualTo(true));
        run.EndRun();
        Assert.That(typeof(GameRun).GetField("returnToPlanningAfterDefeat", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(run), Is.EqualTo(true));
    }

    [Test]
    public void EnglishAndSpanishTextsAndAgentDialogueAreLocalized()
    {
        const string languageKey = "MouseKeeper.Language";
        bool hadLanguage = PlayerPrefs.HasKey(languageKey);
        string language = PlayerPrefs.GetString(languageKey);
        var rootObject = new GameObject("Localized planning");
        try
        {
            GameText.SetEnglish(false);
            Assert.That(GameText.Get("Queso"), Is.EqualTo("Queso"));
            Assert.That(GameText.Get("Running Shoes"), Is.EqualTo("Zapatillas"));
            GameText.SetEnglish(true);
            Assert.That(GameText.Get("Queso"), Is.EqualTo("Cheese"));
            Assert.That(GameText.Get("Todos los agentes han padecido,\npresiona Enter para continuar"),
                Is.EqualTo("All agents have perished,\npress Enter to continue"));
            Assert.That(GameText.Format("Dinero asignado: ${0}", 5), Is.EqualTo("Assigned cash: $5"));
            data.lastResult = "legacy";
            data.lastResultKey = "Contrato: {0}% (${1}) | Efectivo recuperado: ${2} | Pagos: ${3} | Para ti: ${4}";
            data.lastResultValues = new[] { 50, 25, 4, 3, 26 };
            var controller = rootObject.AddComponent<PlanningController>();
            typeof(PlanningController).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, null);
            Label result = rootObject.GetComponent<UIDocument>().rootVisualElement.Q<Label>("last-result");
            Assert.That(result.text, Does.Contain("Contract: 50%"));
            GameText.SetEnglish(false);
            typeof(PlanningController).GetMethod("Refresh", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, null);
            Assert.That(result.text, Does.Contain("Contrato: 50%"));
            GameText.SetEnglish(true);
            foreach (string file in System.IO.Directory.GetFiles(
                System.IO.Path.Combine(Application.dataPath, "Data", "Dialogues", "Spanish"), "*.json"))
            {
                AgentDialogueLines lines = JsonUtility.FromJson<AgentDialogueLines>(System.IO.File.ReadAllText(file));
                foreach (DialogueEvent kind in Enum.GetValues(typeof(DialogueEvent)))
                    foreach (string line in lines.For(kind))
                    {
                        string translated = GameText.Get(line);
                        Assert.That(translated, Is.Not.EqualTo(line), file + ": " + line);
                        Assert.That(translated.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length,
                            Is.LessThanOrEqualTo(10), file + ": " + translated);
                    }
            }
        }
        finally
        {
            Object.DestroyImmediate(rootObject);
            if (hadLanguage) PlayerPrefs.SetString(languageKey, language);
            else PlayerPrefs.DeleteKey(languageKey);
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void LaunchClearsPreviousLocalizedResult()
    {
        data.lastResult = "Old result";
        data.lastResultKey = "Old key";
        data.lastResultValues = new[] { 1 };
        Assert.That(ExpeditionSession.Launch(out _), Is.True);
        Assert.That(data.lastResult, Is.Null);
        Assert.That(data.lastResultKey, Is.Null);
        Assert.That(data.lastResultValues, Is.Null);
    }

    [Test]
    public void PlanningEditorDebugCommandsUpdateAndPersistCampaign()
    {
        var rootObject = new GameObject("Planning test");
        try
        {
            var controller = rootObject.AddComponent<PlanningController>();
            typeof(PlanningController).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, null);
            MethodInfo debug = typeof(PlanningController).GetMethod("ApplyEditorDebug", BindingFlags.Instance | BindingFlags.NonPublic);
            debug.Invoke(controller, new object[] { 2 });
            Assert.That(data.money, Is.EqualTo(20));
            debug.Invoke(controller, new object[] { 3 });
            Assert.That(data.inventory[1], Is.EqualTo(1));
            Assert.That(data.inventory[0], Is.Zero);
            debug.Invoke(controller, new object[] { 4 });
            foreach (AgentProgress progress in data.progress) Assert.That(progress.pending, Is.EqualTo(5));
            CampaignData saved = JsonUtility.FromJson<CampaignData>(PlayerPrefs.GetString(Key));
            Assert.That(saved.money, Is.EqualTo(20));
            Assert.That(saved.inventory[1], Is.EqualTo(1));
            Assert.That(saved.progress[5].pending, Is.EqualTo(5));
            debug.Invoke(controller, new object[] { 1 });
            CampaignData reset = ExpeditionSession.Data;
            Assert.That(reset.money, Is.Zero);
            Assert.That(reset.inventory[1], Is.Zero);
            Assert.That(reset.progress[0].pending, Is.Zero);
            Assert.That(reset.selectedWeapons, Is.All.EqualTo(0));
            Assert.That(JsonUtility.FromJson<CampaignData>(PlayerPrefs.GetString(Key)).money, Is.Zero);
        }
        finally { Object.DestroyImmediate(rootObject); }
    }

    [Test]
    public void PlanningHidesUnaffordableAgentsAndUnavailableWeapons()
    {
        var rootObject = new GameObject("Planning test");
        try
        {
            data.unavailable[0] = 1;
            data.upgradePoints = 1;
            var controller = rootObject.AddComponent<PlanningController>();
            typeof(PlanningController).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, null);
            VisualElement root = rootObject.GetComponent<UIDocument>().rootVisualElement;
            ScrollView roster = root.Q<ScrollView>("roster");
            Assert.That(roster.childCount, Is.EqualTo(1));
            Assert.That(roster.contentContainer[0].Q<Button>("agent-button").enabledSelf, Is.True);
            Assert.That(root.Q<VisualElement>("weapons").childCount, Is.EqualTo(1));
            Assert.That(root.Q<Label>("upgrade-heading").text, Does.Contain("1"));
            Assert.That(root.Q<Button>("upgrade-skill-1").enabledSelf, Is.True);
            Assert.That(root.Q<Label>("lives").text, Does.Contain("5/6"));
            data.money = 10;
            data.inventory[1] = 1;
            data.unavailable[4] = 1;
            typeof(PlanningController).GetMethod("Refresh", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, null);
            Assert.That(roster.childCount, Is.EqualTo(3));
            Assert.That(roster.contentContainer[1].Q<Button>("agent-button").enabledSelf, Is.False);
            Assert.That(root.Q<VisualElement>("weapons").childCount, Is.EqualTo(2));
        }
        finally { Object.DestroyImmediate(rootObject); }
    }

    [Test]
    public void PlanningShowsCompactWeaponTilesAndNoStatTooltips()
    {
        var rootObject = new GameObject("Planning test");
        try
        {
            catalog.weapons[0].displayName = "Test blaster";
            catalog.weapons[0].damage = 12;
            catalog.weapons[0].range = 8;
            var controller = rootObject.AddComponent<PlanningController>();
            typeof(PlanningController).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, null);
            VisualElement root = rootObject.GetComponent<UIDocument>().rootVisualElement;
            VisualElement weapons = root.Q<VisualElement>("weapons");
            Assert.That(weapons, Is.Not.InstanceOf<ScrollView>());
            Button weapon = weapons.Q<Button>("weapon-button");
            Assert.That(weapon.Q<Label>("weapon-details").text, Is.EqualTo("12 | 8"));
            Assert.That(weapon.Q<Label>("weapon-label"), Is.Null);
            typeof(PlanningController).GetMethod("ShowWeaponTooltip", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, new object[] { weapon, catalog.weapons[0].displayName });
            Assert.That(root.Q<Label>("weapon-tooltip").text, Is.EqualTo("Test blaster"));
            Assert.That(root.Q<Label>("weapon-tooltip").style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(root.Q<VisualElement>("slot-stats-0").ElementAt(0).tooltip, Is.Null.Or.Empty);
            Assert.That(root.Q<VisualElement>("skill-upgrades").ElementAt(0).tooltip, Is.Null.Or.Empty);
            Assert.That(root.Q<Label>("stat-tooltip"), Is.Null);
        }
        finally { Object.DestroyImmediate(rootObject); }
    }

    [Test]
    public void PaidAgentsRequireEnoughUnreservedFundsAndBasicAgentsAreFree()
    {
        data.money = 9;
        data.selectedAgents[2] = 4;
        Assert.That(ExpeditionSession.CanLaunch(data, catalog, out _), Is.False);
        data.money = 10;
        Assert.That(ExpeditionSession.CanLaunch(data, catalog, out _), Is.True);
        data.selectedMoney[0] = 1;
        Assert.That(ExpeditionSession.CanLaunch(data, catalog, out _), Is.False);
        data.selectedAgents[2] = 2;
        data.money = 1;
        Assert.That(ExpeditionSession.CanLaunch(data, catalog, out _), Is.True);
        Assert.That(ExpeditionSession.HireCost(catalog.agents[2]), Is.Zero);
    }

    [Test]
    public void EachFloorGeneratesFiveToEightConnectedRoomsWithAnExteriorFinalExit()
    {
        var rooms = (IList)typeof(GameRun).GetField("rooms", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(run);
        Type roomType = typeof(GameRun).GetNestedType("Room", BindingFlags.NonPublic);
        MethodInfo generate = typeof(GameRun).GetMethod("GenerateMaze", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo levelIndex = typeof(GameRun).GetField("levelIndex", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo bossRoom = typeof(GameRun).GetField("bossRoom", BindingFlags.Instance | BindingFlags.NonPublic);
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        UnityEngine.Random.State previous = UnityEngine.Random.state;
        try
        {
            for (int floor = 0; floor < 3; floor++)
            {
                var counts = new HashSet<int>();
                levelIndex.SetValue(run, floor);
                for (int seed = 0; seed < 100; seed++)
                {
                    rooms.Clear();
                    UnityEngine.Random.InitState(seed);
                    generate.Invoke(run, null);
                    Assert.That(rooms.Count, Is.InRange(5, 8));
                    counts.Add(rooms.Count);
                    object boss = bossRoom.GetValue(run);
                    Assert.That(boss, Is.Not.SameAs(rooms[0]));
                    var positions = new HashSet<Vector2Int>();
                    int shops = 0, treasures = 0;
                    foreach (object room in rooms)
                    {
                        Assert.That(positions.Add((Vector2Int)roomType.GetField("position").GetValue(room)), Is.True);
                        string kind = roomType.GetField("kind").GetValue(room).ToString();
                        if (kind == "Shop") shops++;
                        if (kind == "Treasure") treasures++;
                    }
                    Assert.That(shops, Is.EqualTo(floor == 1 ? 1 : 0));
                    Assert.That(treasures, Is.EqualTo(floor == 1 ? 0 : 1));
                    var visited = new HashSet<object> { rooms[0] };
                    var pending = new Queue<object>();
                    pending.Enqueue(rooms[0]);
                    while (pending.Count > 0)
                    {
                        object room = pending.Dequeue();
                        foreach (object neighbour in (Array)roomType.GetField("neighbours").GetValue(room))
                            if (neighbour != null && visited.Add(neighbour)) pending.Enqueue(neighbour);
                    }
                    Assert.That(visited.Count, Is.EqualTo(rooms.Count));
                    if (floor == 2)
                    {
                        int exit = (int)roomType.GetField("exitDirection").GetValue(boss);
                        Assert.That(exit, Is.InRange(0, 3));
                        Vector2Int position = (Vector2Int)roomType.GetField("position").GetValue(boss);
                        Assert.That(positions.Contains(position + directions[exit]), Is.False);
                    }
                }
                Assert.That(counts, Is.EquivalentTo(new[] { 5, 6, 7, 8 }));
            }
        }
        finally
        {
            UnityEngine.Random.state = previous;
        }
    }

    [Test]
    public void BasicAgentsRemainAvailableWithoutErasingMissingStatus()
    {
        data.unavailable = new[] { 2, 1, 2, 2, 2, 0 };
        Assert.That(ExpeditionSession.CanSelect(data, catalog, 0), Is.True);
        Assert.That(ExpeditionSession.CanSelect(data, catalog, 4), Is.False);
        Assert.That(ExpeditionSession.EnsureAvailableAgents(data), Is.False);
        Assert.That(data.unavailable, Is.EqualTo(new[] { 2, 1, 2, 2, 2, 0 }));
    }

    [Test]
    public void OneMissingAgentReturnsAndEachSurvivorGetsOnePoint()
    {
        data.unavailable[4] = 1;
        data.unavailable[5] = 2;
        Assert.That(ExpeditionSession.Launch(out _), Is.True);
        ExpeditionSession.Settle(0, Squad(), true);
        Assert.That(data.unavailable[4], Is.Zero);
        Assert.That(data.unavailable[5], Is.EqualTo(2));
        Assert.That(ExpeditionSession.Losses(data), Is.EqualTo(1));
        Assert.That(data.upgradePoints, Is.Zero);
        for (int i = 0; i < 3; i++) Assert.That(data.progress[i].pending, Is.EqualTo(1));
    }

    [Test]
    public void DefeatAndAbandonmentDoNotGrantPoints()
    {
        Assert.That(ExpeditionSession.Launch(out _), Is.True);
        ExpeditionSession.Settle(0, Squad(), false);
        Assert.That(data.upgradePoints, Is.Zero);
        for (int i = 0; i < 3; i++) Assert.That(data.progress[i].pending, Is.Zero);
        Assert.That(ExpeditionSession.Launch(out _), Is.True);
        ExpeditionSession.Abandon();
        Assert.That(data.upgradePoints, Is.Zero);
    }

    [Test]
    public void FallenAgentDoesNotReceiveCompletionPoint()
    {
        Assert.That(ExpeditionSession.Launch(out _), Is.True);
        ExpeditionSession.Settle(3f, Squad(true), true);
        Assert.That(data.progress[0].pending, Is.Zero);
        Assert.That(data.progress[1].pending, Is.EqualTo(1));
        Assert.That(data.progress[2].pending, Is.EqualTo(1));
    }

    [Test]
    public void LegacyPendingPointsAreMigratedOnceWithoutLosingUpgrades()
    {
        data.progressionVersion = 0;
        data.progress[0].pending = 2;
        data.progress[1].pending = 1;
        data.progress[1].accuracy = 2;
        Assert.That(ExpeditionSession.ReconcileCatalog(data, catalog), Is.True);
        Assert.That(data.upgradePoints, Is.Zero);
        Assert.That(data.progress[0].pending, Is.EqualTo(2));
        Assert.That(data.progress[1].pending, Is.EqualTo(1));
        Assert.That(data.progress[1].accuracy, Is.EqualTo(2));
        Assert.That(ExpeditionSession.ReconcileCatalog(data, catalog), Is.True);
        Assert.That(data.progress[0].pending, Is.EqualTo(2));
    }

    [Test]
    public void LoadingLegacyCampaignPersistsMigrationWithoutDuplicatingPoints()
    {
        ExpeditionCatalog savedCatalog = Resources.Load<ExpeditionCatalog>("ExpeditionCatalog");
        CampaignData legacy = ExpeditionSession.NewCampaign(savedCatalog);
        legacy.progressionVersion = 0;
        legacy.progress[0].pending = 2;
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(legacy));
        typeof(ExpeditionSession).GetField("catalog", StaticPrivate).SetValue(null, null);
        typeof(ExpeditionSession).GetField("data", StaticPrivate).SetValue(null, null);
        Assert.That(ExpeditionSession.Initialize(), Is.True);
        Assert.That(ExpeditionSession.Data.progress[0].pending, Is.EqualTo(2));
        CampaignData persisted = JsonUtility.FromJson<CampaignData>(PlayerPrefs.GetString(Key));
        Assert.That(persisted.progressionVersion, Is.EqualTo(2));
        Assert.That(persisted.progress[0].pending, Is.EqualTo(2));
        typeof(ExpeditionSession).GetField("catalog", StaticPrivate).SetValue(null, null);
        typeof(ExpeditionSession).GetField("data", StaticPrivate).SetValue(null, null);
        Assert.That(ExpeditionSession.Initialize(), Is.True);
        Assert.That(ExpeditionSession.Data.progress[0].pending, Is.EqualTo(2));
    }

    [Test]
    public void IndividualPointsCannotBeSpentOnAnotherAgent()
    {
        data.progress[0].pending = 1;
        Assert.That(ExpeditionSession.Upgrade(data, catalog, 1, 1), Is.False);
        Assert.That(ExpeditionSession.Upgrade(data, catalog, 0, 1), Is.True);
        Assert.That(data.progress[0].pending, Is.Zero);
        Assert.That(data.progress[1].Total, Is.Zero);
    }

    [Test]
    public void CappedSkillDoesNotConsumeSharedPoint()
    {
        data.upgradePoints = 1;
        catalog.agents[0].profile.accuracy = 10;
        Assert.That(ExpeditionSession.Upgrade(data, catalog, 0, 1), Is.False);
        Assert.That(data.upgradePoints, Is.EqualTo(1));
        Assert.That(data.progress[0].Total, Is.Zero);
    }

    [Test]
    public void UpgradesIncreaseChosenSkillHealthAndSpeedAndResetOnDeath()
    {
        data.upgradePoints = 1;
        AgentProfile baseline = ExpeditionSession.Profile(data, catalog, 0);
        Assert.That(ExpeditionSession.Upgrade(data, catalog, 0, 1), Is.True);
        AgentProfile improved = ExpeditionSession.Profile(data, catalog, 0);
        Assert.That(improved.accuracy, Is.EqualTo(baseline.accuracy + 1));
        Assert.That(improved.EffectiveHealth, Is.EqualTo(Mathf.Ceil(baseline.EffectiveHealth * 1.1f)));
        Assert.That(improved.EffectiveSpeed, Is.EqualTo(baseline.EffectiveSpeed * 1.05f).Within(0.001f));
        Assert.That(ExpeditionSession.Upgrade(data, catalog, 0, 1), Is.False);
        AgentProfile restored = ExpeditionSession.ResetProgress(0);
        Assert.That(restored.accuracy, Is.EqualTo(baseline.accuracy));
        Assert.That(restored.EffectiveHealth, Is.EqualTo(baseline.EffectiveHealth));
        Assert.That(restored.EffectiveSpeed, Is.EqualTo(baseline.EffectiveSpeed));
    }

    [Test]
    public void RevivedAgentUsesOriginalStatsAfterDying()
    {
        data.upgradePoints = 1;
        Assert.That(ExpeditionSession.Upgrade(data, catalog, 0, 1), Is.True);
        Assert.That(ExpeditionSession.Launch(out _), Is.True);
        PlayerController[] squad = Squad();
        squad[0].ResetProgression(ExpeditionSession.Profile(data, catalog, 0));
        var living = (List<PlayerController>)typeof(GameRun)
            .GetField("agents", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(run);
        living.AddRange(squad);
        typeof(PlayerController).GetField("<Dead>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(squad[0], true);
        run.AgentDied(squad[0]);
        squad[0].ReturnToSquad(Vector3.zero);
        Assert.That(data.progress[0].Total, Is.Zero);
        Assert.That(squad[0].Profile.accuracy, Is.EqualTo(catalog.agents[0].profile.accuracy));
        Assert.That(squad[0].MaxHealth, Is.EqualTo(catalog.agents[0].profile.EffectiveHealth));
        Assert.That(squad[0].MovementSpeed, Is.EqualTo(catalog.agents[0].profile.speed));
    }

    [Test]
    public void RevivalPassiveAlsoResetsUpgrades()
    {
        var cameraObject = new GameObject("Test camera");
        var passive = ScriptableObject.CreateInstance<RevivePassiveItemDefinition>();
        DamageIndicators indicators = null;
        try
        {
            data.upgradePoints = 1;
            Assert.That(ExpeditionSession.Upgrade(data, catalog, 0, 1), Is.True);
            Assert.That(ExpeditionSession.Launch(out _), Is.True);
            PlayerController[] squad = Squad();
            squad[0].ResetProgression(ExpeditionSession.Profile(data, catalog, 0));
            ((List<PlayerController>)typeof(GameRun).GetField("agents", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(run)).AddRange(squad);
            indicators = roots[0].AddComponent<DamageIndicators>();
            indicators.Initialize(cameraObject.AddComponent<Camera>());
            typeof(GameRun).GetField("damageIndicators", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(run, indicators);
            squad[0].AddPassive(passive);
            squad[0].TakeDamage(10000f);
            Assert.That(squad[0].Dead, Is.False);
            Assert.That(squad[0].Health, Is.GreaterThan(0));
            Assert.That(data.progress[0].Total, Is.Zero);
            Assert.That(squad[0].Profile.accuracy, Is.EqualTo(catalog.agents[0].profile.accuracy));
        }
        finally
        {
            if (indicators != null)
            {
                var canvas = (Canvas)typeof(DamageIndicators).GetField("canvas", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(indicators);
                if (canvas != null) Object.DestroyImmediate(canvas.gameObject);
            }
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(passive);
        }
    }

    [Test]
    public void SixMissingAgentsEndTheCampaignAndRestartClearsIt()
    {
        data.money = 20;
        data.unavailable = new[] { 1, 2, 3, 4, 0, 0 };
        data.selectedAgents = new[] { 0, 4, 5 };
        Assert.That(ExpeditionSession.Launch(out _), Is.True);
        PlayerController[] squad = Squad();
        foreach (PlayerController agent in squad)
            typeof(PlayerController).GetField("<Dead>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(agent, true);
        ExpeditionSession.Settle(0, squad, false);
        Assert.That(ExpeditionSession.GameOver, Is.True);
        Assert.That(ExpeditionSession.Losses(data), Is.EqualTo(6));
        Assert.That(ExpeditionSession.CanLaunch(data, catalog, out _), Is.False);
        ExpeditionSession.Restart();
        Assert.That(ExpeditionSession.GameOver, Is.False);
        Assert.That(ExpeditionSession.Losses(ExpeditionSession.Data), Is.Zero);
    }

    [Test]
    public void WeaponChoicesExcludeCopiesAssignedToOtherAgents()
    {
        Assert.That(ExpeditionSession.AvailableForSlot(data, catalog, 0, 0), Is.EqualTo(int.MaxValue));
        data.inventory[1] = 1;
        data.selectedWeapons[1] = 1;
        Assert.That(ExpeditionSession.AvailableForSlot(data, catalog, 1, 0), Is.Zero);
        Assert.That(ExpeditionSession.AvailableForSlot(data, catalog, 1, 1), Is.EqualTo(1));
    }

    [Test]
    public void MissingWeaponsFallBackToUnlimitedBasicWithoutDuplicatingInventory()
    {
        data.inventory[1] = 1;
        data.selectedWeapons = new[] { 1, 1, 0 };
        Assert.That(ExpeditionSession.Launch(out _), Is.True);
        Assert.That(data.selectedWeapons, Is.EqualTo(new[] { 1, 0, 0 }));
        Assert.That(data.inventory, Is.EqualTo(new[] { 0, 0 }));
        ExpeditionSession.Settle(0, Squad(), false);
        Assert.That(data.inventory, Is.EqualTo(new[] { 0, 1 }));
        data.selectedWeapons = new[] { 1, 0, 0 };
        Assert.That(ExpeditionSession.Launch(out _), Is.True);
        ExpeditionSession.Abandon();
        Assert.That(data.inventory, Is.EqualTo(new[] { 0, 1 }));
    }

    [Test]
    public void LostSelectedWeaponIsReplacedWhenReturningToPlanning()
    {
        data.inventory[1] = 1;
        data.selectedWeapons[0] = 1;
        Assert.That(ExpeditionSession.Launch(out _), Is.True);
        PlayerController[] squad = Squad();
        squad[0].Weapons[0] = new WeaponInstance(catalog.basicWeapon);
        ExpeditionSession.Settle(0, squad, false);
        Assert.That(data.inventory[1], Is.Zero);
        Assert.That(data.selectedWeapons, Is.EqualTo(new[] { 0, 0, 0 }));
        Assert.That(ExpeditionSession.CanLaunch(data, catalog, out _), Is.True);
    }

    [Test]
    public void RandomPlanningUsesAvailableAgentsAndWeapons()
    {
        data.unavailable[4] = 2;
        data.inventory[1] = 1;
        data.money = 10;
        var squads = new HashSet<string>();
        var weapons = new HashSet<int>();
        UnityEngine.Random.State previous = UnityEngine.Random.state;
        try
        {
            UnityEngine.Random.InitState(41);
            for (int attempt = 0; attempt < 100; attempt++)
            {
                data.selectedMoney = new[] { 1, 2, 3 };
                Assert.That(ExpeditionSession.RandomizePlan(data, catalog), Is.True);
                Assert.That(new HashSet<int>(data.selectedAgents).Count, Is.EqualTo(3));
                Assert.That(Array.IndexOf(data.selectedAgents, 4), Is.EqualTo(-1));
                Assert.That(Array.FindAll(data.selectedWeapons, index => index == 1).Length, Is.LessThanOrEqualTo(1));
                Assert.That(data.selectedMoney, Is.EqualTo(new[] { 0, 0, 0 }));
                Assert.That(ExpeditionSession.CanLaunch(data, catalog, out _), Is.True);
                squads.Add(string.Join(",", data.selectedAgents));
                foreach (int weapon in data.selectedWeapons) weapons.Add(weapon);
            }
            Assert.That(squads.Count, Is.GreaterThan(1));
            Assert.That(weapons, Is.EquivalentTo(new[] { 0, 1 }));
        }
        finally
        {
            UnityEngine.Random.state = previous;
        }
    }

    [Test]
    public void PartialFloorDiscountMatchesTheRequestedExample()
    {
        var rooms = (IList)typeof(GameRun).GetField("rooms", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(run);
        Type type = typeof(GameRun).GetNestedType("Room", BindingFlags.NonPublic);
        MethodInfo record = typeof(GameRun).GetMethod("RecordCompletedFloor", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo bossField = typeof(GameRun).GetField("bossRoom", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo creditField = typeof(GameRun).GetField("completedFloorCredit", BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (int cleared in new[] { 3, 4, 5 })
        {
            rooms.Clear();
            for (int i = 0; i < 5; i++)
            {
                object room = Activator.CreateInstance(type, true);
                type.GetField("visited").SetValue(room, i < cleared);
                type.GetField("cleared").SetValue(room, i < cleared);
                rooms.Add(room);
            }
            type.GetField("bossDefeated").SetValue(rooms[0], true);
            bossField.SetValue(run, rooms[0]);
            record.Invoke(run, null);
            float expected = cleared == 3 ? 0.4f : cleared == 4 ? 0.6f : 1.6f;
            Assert.That((float)creditField.GetValue(run), Is.EqualTo(expected).Within(0.0001f));
        }
        foreach (AgentDefinition agent in catalog.agents) agent.missionCost = 0;
        Assert.That(ExpeditionSession.Launch(out _), Is.True);
        ExpeditionSession.Settle((float)creditField.GetValue(run), Squad(), true);
        Assert.That(data.money, Is.EqualTo(27));
        Assert.That(data.lastResult, Does.Contain("Contrato: 53%"));
    }

    [Test]
    public void PlanningSceneContainsItsController()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Planning.unity");
        try
        {
            int count = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
                count += root.GetComponentsInChildren<PlanningController>(true).Length;
            Assert.That(count, Is.EqualTo(1));
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    [Test]
    public void DefaultCatalogHasFifteenDistinctAgentsAndThreeModestStarters()
    {
        ExpeditionCatalog defaults = AssetDatabase.LoadAssetAtPath<ExpeditionCatalog>("Assets/Resources/ExpeditionCatalog.asset");
        Assert.That(defaults, Is.Not.Null);
        Assert.That(defaults.agents.Length, Is.GreaterThanOrEqualTo(15));
        var names = new HashSet<string>();
        var stats = new HashSet<(float, float, float)>();
        foreach (AgentDefinition agent in defaults.agents)
        {
            Assert.That(agent, Is.Not.Null);
            Assert.That(names.Add(agent.profile.name), Is.True);
            Assert.That(stats.Add((agent.profile.health, agent.profile.speed, agent.profile.attackSpeed)), Is.True);
        }
        for (int i = 0; i < 3; i++)
        {
            Assert.That(defaults.agents[i].profile.health, Is.LessThanOrEqualTo(60));
            Assert.That(defaults.agents[i].profile.speed, Is.LessThanOrEqualTo(3.5f));
            Assert.That(defaults.agents[i].profile.attackSpeed, Is.LessThanOrEqualTo(0.86f));
        }
    }

    [Test]
    public void DefaultCatalogLoadsEveryWeaponAndItsSpecializedDefinition()
    {
        var defaults = Resources.Load<ExpeditionCatalog>("ExpeditionCatalog");
        Assert.That(defaults == null, Is.False);
        for (int i = 0; i < defaults.weapons.Length; i++)
            Assert.That(defaults.weapons[i] == null, Is.False, $"weapons[{i}]");

        Assert.That(defaults.weapons, Has.Some.TypeOf<GasGrenadeWeaponDefinition>());
        Assert.That(defaults.weapons, Has.Some.TypeOf<CheeseDecoyWeaponDefinition>());
        Assert.That(defaults.weapons, Has.Some.TypeOf<BlowerWeaponDefinition>());
        Assert.That(defaults.weapons, Has.Some.TypeOf<ShotgunWeaponDefinition>());
        Assert.That(defaults.weapons, Has.Some.TypeOf<WhipWeaponDefinition>());
    }

    [Test]
    public void AssignedCashIsReservedAndOnlySurvivorsReturnIt()
    {
        data.money = 20;
        data.selectedMoney = new[] { 4, 5, 6 };
        foreach (AgentDefinition agent in catalog.agents) agent.missionCost = 0;
        Assert.That(ExpeditionSession.Launch(out _), Is.True);
        Assert.That(data.money, Is.EqualTo(5));
        PlayerController[] squad = Squad(true);
        for (int i = 0; i < squad.Length; i++) squad[i].AddMoney(data.selectedMoney[i]);
        ExpeditionSession.Settle(0, squad, false);
        Assert.That(data.money, Is.EqualTo(16));
        data.selectedMoney[2] = 100;
        Assert.That(ExpeditionSession.CanLaunch(data, catalog, out _), Is.False);
    }

    [Test]
    public void CatalogReorderingPreservesIdentitiesAndInventory()
    {
        data.unavailable[0] = 2;
        data.progress[0].accuracy = 2;
        data.inventory[1] = 4;
        Array.Reverse(catalog.agents);
        Array.Reverse(catalog.weapons);
        Assert.That(ExpeditionSession.ReconcileCatalog(data, catalog), Is.True);
        Assert.That(data.unavailable[5], Is.EqualTo(2));
        Assert.That(data.progress[5].accuracy, Is.EqualTo(2));
        Assert.That(data.inventory, Is.EqualTo(new[] { 4, 0 }));
        Assert.That(data.selectedAgents, Is.EqualTo(new[] { 5, 4, 3 }));
        Assert.That(data.selectedWeapons, Is.EqualTo(new[] { 1, 1, 1 }));
    }
}
