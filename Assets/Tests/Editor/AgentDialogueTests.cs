using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MouseKeeper;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;
using Object = UnityEngine.Object;

public sealed class DialogueTestHealingPassive : PassiveItemDefinition
{
    public override void OnPickUp(PlayerController player) => player.Heal(5f);
}

public sealed class AgentDialogueTests
{
    const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    GameObject runObject;
    GameObject cameraObject;
    GameRun run;
    SquadIndicators indicators;
    DamageIndicators damage;
    AgentDefinition definition;
    TextAsset text;
    readonly List<GameObject> objects = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        runObject = new GameObject("Test run");
        cameraObject = new GameObject("Test camera");
        run = runObject.AddComponent<GameRun>();
        Camera camera = cameraObject.AddComponent<Camera>();
        indicators = runObject.AddComponent<SquadIndicators>();
        indicators.Initialize(camera);
        damage = runObject.AddComponent<DamageIndicators>();
        damage.Initialize(camera);
        typeof(GameRun).GetField("squadIndicators", PrivateInstance).SetValue(run, indicators);
        typeof(GameRun).GetField("damageIndicators", PrivateInstance).SetValue(run, damage);
        text = new TextAsset("{\"roomCleared\":[\"Sala uno\",\"Sala dos\",\"Sala tres\",\"Sala cuatro\",\"Sala cinco\"]," +
            "\"injured\":[\"Herido uno\",\"Herido dos\",\"Herido tres\",\"Herido cuatro\",\"Herido cinco\"]," +
            "\"purchase\":[\"Compra uno\",\"Compra dos\",\"Compra tres\",\"Compra cuatro\",\"Compra cinco\"]," +
            "\"healed\":[\"Cura uno\",\"Cura dos\",\"Cura tres\",\"Cura cuatro\",\"Cura cinco\"]," +
            "\"bossDefeated\":[\"Jefe uno\",\"Jefe dos\",\"Jefe tres\",\"Jefe cuatro\",\"Jefe cinco\"]," +
            "\"allyDied\":[\"Amigo uno\",\"Amigo dos\",\"Amigo tres\",\"Amigo cuatro\",\"Amigo cinco\"]}");
        definition = ScriptableObject.CreateInstance<AgentDefinition>();
        definition.dialogues = new[] { new LocalizedAgentDialogue { language = "Spanish", file = text } };
    }

    [TearDown]
    public void TearDown()
    {
        Canvas speechCanvas = (Canvas)typeof(SquadIndicators).GetField("canvas", PrivateInstance).GetValue(indicators);
        Canvas damageCanvas = (Canvas)typeof(DamageIndicators).GetField("canvas", PrivateInstance).GetValue(damage);
        if (speechCanvas != null) Object.DestroyImmediate(speechCanvas.gameObject);
        if (damageCanvas != null) Object.DestroyImmediate(damageCanvas.gameObject);
        foreach (GameObject obj in objects)
            if (obj != null) Object.DestroyImmediate(obj);
        objects.Clear();
        Object.DestroyImmediate(definition);
        Object.DestroyImmediate(text);
        Object.DestroyImmediate(runObject);
        Object.DestroyImmediate(cameraObject);
    }

    PlayerController CreateAgent(string name, Vector3 position)
    {
        var root = new GameObject(name);
        objects.Add(root);
        root.transform.position = position;
        PlayerController agent = root.AddComponent<PlayerController>();
        agent.Initialize(run, new AgentProfile { name = name });
        ((List<PlayerController>)typeof(GameRun).GetField("agents", PrivateInstance).GetValue(run)).Add(agent);
        indicators.Register(agent);
        AgentDialogue dialogue = root.AddComponent<AgentDialogue>();
        dialogue.Initialize(agent, definition, indicators);
        agent.SetDialogue(dialogue);
        return agent;
    }

    object Indicator(PlayerController agent)
    {
        IList list = (IList)typeof(SquadIndicators).GetField("indicators", PrivateInstance).GetValue(indicators);
        foreach (object item in list)
            if ((PlayerController)item.GetType().GetField("agent").GetValue(item) == agent) return item;
        throw new InvalidOperationException("Agent indicator not registered.");
    }

    IList Speeches(PlayerController agent) => (IList)Indicator(agent).GetType().GetField("speeches").GetValue(Indicator(agent));
    Queue<AgentEmotion> Emotions(PlayerController agent) =>
        (Queue<AgentEmotion>)Indicator(agent).GetType().GetField("emotions").GetValue(Indicator(agent));
    static string Content(object speech) => (string)speech.GetType().GetField("content").GetValue(speech);

    [Test]
    public void DefaultAgentsHaveFiveShortDistinctLinesForEveryEvent()
    {
        ExpeditionCatalog catalog = Resources.Load<ExpeditionCatalog>("ExpeditionCatalog");
        Assert.That(catalog, Is.Not.Null);
        Assert.That(catalog.agents.Length, Is.EqualTo(15));
        foreach (AgentDefinition agent in catalog.agents)
        {
            TextAsset file = AgentDialogue.SelectFile(agent, "Spanish");
            Assert.That(file, Is.Not.Null, agent.name);
            AgentDialogueLines lines = JsonUtility.FromJson<AgentDialogueLines>(file.text);
            foreach (DialogueEvent kind in Enum.GetValues(typeof(DialogueEvent)))
            {
                string[] entries = lines.For(kind);
                Assert.That(entries.Length, Is.EqualTo(5), agent.name + " " + kind);
                Assert.That(entries.Distinct().Count(), Is.EqualTo(5));
                foreach (string entry in entries)
                    Assert.That(entry.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length, Is.InRange(1, 10));
            }
        }
    }

    [Test]
    public void LocalizedFileUsesRequestedLanguageThenSpanishFallback()
    {
        var english = new TextAsset("{}");
        try
        {
            definition.dialogues = new[] {
                new LocalizedAgentDialogue { language = "Spanish", file = text },
                new LocalizedAgentDialogue { language = "English", file = english }
            };
            Assert.That(AgentDialogue.SelectFile(definition, "english"), Is.SameAs(english));
            Assert.That(AgentDialogue.SelectFile(definition, "French"), Is.SameAs(text));
        }
        finally { Object.DestroyImmediate(english); }
    }

    [Test]
    public void LinesDoNotRepeatWithinFloorAndMandatorySpeechContinuesAfterExhaustion()
    {
        PlayerController agent = CreateAgent("Speaker", Vector3.zero);
        Assert.That(agent.Dialogue.Speak(DialogueEvent.Healed, 0f), Is.False);
        for (int i = 0; i < 6; i++)
            Assert.That(agent.Dialogue.Speak(DialogueEvent.Healed, 1f), Is.True);
        IList speeches = Speeches(agent);
        var firstFive = new HashSet<string>();
        for (int i = 0; i < 5; i++) firstFive.Add(Content(speeches[i]));
        Assert.That(firstFive.Count, Is.EqualTo(5));
        Assert.That(Content(speeches[5]), Is.Not.EqualTo(Content(speeches[4])));
        agent.Dialogue.ResetFloor();
        for (int i = 0; i < 5; i++) Assert.That(agent.Dialogue.Speak(DialogueEvent.Healed, 1f), Is.True);
        var nextFloor = new HashSet<string>();
        for (int i = 6; i < 11; i++) nextFloor.Add(Content(speeches[i]));
        Assert.That(nextFloor.Count, Is.EqualTo(5));
    }

    [TestCase(0.2f)]
    [TestCase(0.5f)]
    public void OptionalSpeechUsesTheConfiguredProbability(float chance)
    {
        PlayerController agent = CreateAgent("Speaker", Vector3.zero);
        for (int seed = 1; seed <= 30; seed++)
        {
            UnityEngine.Random.InitState(seed);
            float roll = UnityEngine.Random.value;
            UnityEngine.Random.InitState(seed);
            Assert.That(agent.Dialogue.Speak(DialogueEvent.Purchase, chance), Is.EqualTo(roll < chance));
        }
    }

    [Test]
    public void SeriousInjuryIsOncePerFloorAndActualHealingAlwaysSpeaks()
    {
        PlayerController agent = CreateAgent("Injured", Vector3.zero);
        CreateAgent("Survivor", Vector3.right * 4f);
        agent.Heal(1f);
        Assert.That(Speeches(agent).Count, Is.Zero);
        agent.TakeDamage(75f);
        Assert.That(Speeches(agent).Count, Is.EqualTo(1));
        agent.Dialogue.CheckInjury();
        Assert.That(Speeches(agent).Count, Is.EqualTo(1));
        agent.Heal(5f);
        agent.Heal(5f);
        Assert.That(Speeches(agent).Count, Is.EqualTo(3));
        agent.Dialogue.ResetFloor();
        agent.TakeDamage(10f);
        Assert.That(Speeches(agent).Count, Is.EqualTo(4));
    }

    [Test]
    public void PassiveHealingSpeaksOncePerActualHealing()
    {
        PlayerController agent = CreateAgent("Injured", Vector3.zero);
        agent.TakeDamage(25f);
        var passive = ScriptableObject.CreateInstance<DialogueTestHealingPassive>();
        try
        {
            agent.AddPassive(passive);
            Assert.That(Speeches(agent).Count, Is.EqualTo(1));
            passive.maxHealthBonus = 10f;
            agent.AddPassive(passive);
            Assert.That(Speeches(agent).Count, Is.EqualTo(3));
        }
        finally { Object.DestroyImmediate(passive); }
    }

    [Test]
    public void BossHasExactlyOneSpeakerAndHappinessRequiresNoEarlierDeaths()
    {
        PlayerController first = CreateAgent("First", Vector3.zero);
        PlayerController second = CreateAgent("Second", Vector3.right * 3f);
        PlayerController third = CreateAgent("Third", Vector3.left * 3f);
        typeof(GameRun).GetMethod("BossDialogue", PrivateInstance).Invoke(run, null);
        Assert.That(Speeches(first).Count + Speeches(second).Count + Speeches(third).Count, Is.EqualTo(1));
        foreach (PlayerController agent in new[] { first, second, third })
        {
            Assert.That(Emotions(agent).Dequeue(), Is.EqualTo(AgentEmotion.Happiness));
            Assert.That(Emotions(agent).Count, Is.Zero);
        }
        typeof(GameRun).GetField("anyAgentDied", PrivateInstance).SetValue(run, true);
        typeof(GameRun).GetMethod("BossDialogue", PrivateInstance).Invoke(run, null);
        Assert.That(Speeches(first).Count + Speeches(second).Count + Speeches(third).Count, Is.EqualTo(2));
        Assert.That(Emotions(first).Count + Emotions(second).Count + Emotions(third).Count, Is.Zero);
    }

    [Test]
    public void DeathReactionsWaitOneSecondPerLivingTeammate()
    {
        PlayerController first = CreateAgent("First", Vector3.zero);
        PlayerController second = CreateAgent("Second", Vector3.right * 3f);
        IEnumerator reactions = (IEnumerator)typeof(GameRun).GetMethod("DeathReactions", PrivateInstance)
            .Invoke(run, new object[] { new List<PlayerController> { first, second } });
        FieldInfo seconds = typeof(WaitForSeconds).GetField("m_Seconds", PrivateInstance);
        Assert.That(reactions.MoveNext(), Is.True);
        Assert.That((float)seconds.GetValue(reactions.Current), Is.EqualTo(1f));
        Assert.That(Speeches(first).Count + Speeches(second).Count, Is.Zero);
        Assert.That(reactions.MoveNext(), Is.True);
        Assert.That((float)seconds.GetValue(reactions.Current), Is.EqualTo(1f));
        Assert.That(Speeches(first).Count, Is.EqualTo(1));
        Assert.That(Speeches(second).Count, Is.Zero);
        Assert.That(reactions.MoveNext(), Is.False);
        Assert.That(Speeches(second).Count, Is.EqualTo(1));
    }

    [Test]
    public void SuccessfulPassiveAndWeaponPickupsAnnounceTheirNames()
    {
        PlayerController agent = CreateAgent("Speaker", Vector3.zero);
        var passive = ScriptableObject.CreateInstance<PassiveItemDefinition>();
        var weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
        passive.displayName = "Amuleto de prueba";
        weapon.displayName = "Arma de prueba";
        var passiveObject = new GameObject("Passive");
        var weaponObject = new GameObject("Weapon");
        objects.Add(passiveObject);
        objects.Add(weaponObject);
        try
        {
            ItemPickup passivePickup = passiveObject.AddComponent<ItemPickup>();
            passivePickup.passive = passive;
            LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
            passivePickup.PickUp(agent);
            Assert.That(Speeches(agent).Count, Is.EqualTo(1));
            Assert.That(Content(Speeches(agent)[0]), Is.EqualTo(passive.displayName));
            ItemPickup weaponPickup = weaponObject.AddComponent<ItemPickup>();
            weaponPickup.weapon = new WeaponInstance(weapon);
            LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
            weaponPickup.PickUp(agent);
            Assert.That(Speeches(agent).Count, Is.EqualTo(2));
            Assert.That(Content(Speeches(agent)[1]), Is.EqualTo(weapon.displayName));
        }
        finally { Object.DestroyImmediate(passive); Object.DestroyImmediate(weapon); }
    }

    [Test]
    public void UnaffordablePickupDoesNotAnnounceItsName()
    {
        PlayerController agent = CreateAgent("Speaker", Vector3.zero);
        var passive = ScriptableObject.CreateInstance<PassiveItemDefinition>();
        var item = new GameObject("Passive");
        objects.Add(item);
        try
        {
            ItemPickup pickup = item.AddComponent<ItemPickup>();
            pickup.passive = passive;
            pickup.SetPrice(5, null);
            pickup.PickUp(agent);
            Assert.That(Speeches(agent).Count, Is.Zero);
            Assert.That(pickup.Available, Is.True);
        }
        finally { Object.DestroyImmediate(passive); }
    }

    [Test]
    public void ContestedLootAndPurchasesTriggerTheCorrespondingEmotions()
    {
        PlayerController collector = CreateAgent("Collector", Vector3.zero);
        PlayerController observer = CreateAgent("Observer", Vector3.right * 3f);
        var item = new GameObject("Money");
        objects.Add(item);
        item.AddComponent<MoneyVial>();
        ItemPickup pickup = item.AddComponent<ItemPickup>();
        typeof(ItemPickup).GetMethod("Awake", PrivateInstance).Invoke(pickup, null);
        typeof(PlayerController).GetProperty("Karma").SetValue(collector, -1);
        typeof(PlayerController).GetProperty("Karma").SetValue(observer, 7);
        run.ReactToContestedPickup(collector, pickup);
        Assert.That(Emotions(observer).Count, Is.Zero);
        typeof(PlayerController).GetProperty("Karma").SetValue(observer, 8);
        run.ReactToContestedPickup(collector, pickup);
        Assert.That(Emotions(observer).Dequeue(), Is.EqualTo(AgentEmotion.Anger));
        collector.Dialogue.Purchase();
        Assert.That(Emotions(collector).Count, Is.Zero);
        collector.Dialogue.Purchase();
        Assert.That(Emotions(collector).Dequeue(), Is.EqualTo(AgentEmotion.Desire));
    }

    [Test]
    public void FuryNeedsTwoNearbyMiceAndDoesNotRepeatWhileSurrounded()
    {
        PlayerController agent = CreateAgent("Speaker", Vector3.zero);
        Type type = typeof(GameRun).GetNestedType("Room", BindingFlags.NonPublic);
        object room = Activator.CreateInstance(type, true);
        var root = new GameObject("Room");
        objects.Add(root);
        type.GetField("root").SetValue(room, root);
        typeof(GameRun).GetField("currentRoom", PrivateInstance).SetValue(run, room);
        var first = new GameObject("First mouse");
        var second = new GameObject("Second mouse");
        first.transform.SetParent(root.transform);
        second.transform.SetParent(root.transform);
        first.transform.position = Vector3.left * 2f;
        second.transform.position = Vector3.right * 4f;
        first.AddComponent<MouseEnemy>();
        second.AddComponent<MouseEnemy>();
        Assert.That(run.IsSurrounded(agent), Is.False);
        second.transform.position = Vector3.right * 3f;
        Assert.That(run.IsSurrounded(agent), Is.True);
        agent.Dialogue.SetFury(true);
        agent.Dialogue.SetFury(true);
        Assert.That(Emotions(agent).Count, Is.EqualTo(1));
        Assert.That(Emotions(agent).Dequeue(), Is.EqualTo(AgentEmotion.Fury));
        agent.Dialogue.SetFury(false);
        agent.Dialogue.SetFury(true);
        Assert.That(Emotions(agent).Dequeue(), Is.EqualTo(AgentEmotion.Fury));
    }

    [Test]
    public void TypewriterAndEmojiBubbleFollowTheirDisplayTiming()
    {
        PlayerController agent = CreateAgent("Speaker", Vector3.forward * 10f);
        Assert.That(agent.Dialogue.Speak(DialogueEvent.Healed, 1f), Is.True);
        object speech = Speeches(agent)[0];
        FieldInfo started = speech.GetType().GetField("started");
        FieldInfo typing = speech.GetType().GetField("typingDuration");
        Text label = (Text)speech.GetType().GetField("text").GetValue(speech);
        float duration = (float)typing.GetValue(speech);
        started.SetValue(speech, Time.time - duration * 0.5f);
        agent.Dialogue.Emotion(AgentEmotion.Fear);
        typeof(SquadIndicators).GetMethod("LateUpdate", PrivateInstance).Invoke(indicators, null);
        Assert.That(label.text.Length, Is.GreaterThan(0).And.LessThan(Content(speech).Length));
        object indicator = Indicator(agent);
        Image emoji = (Image)indicator.GetType().GetField("emotion").GetValue(indicator);
        Assert.That(emoji.gameObject.activeSelf, Is.True);
        Assert.That(emoji.sprite, Is.Not.Null);
        started.SetValue(speech, Time.time - duration - 2.5f);
        indicator.GetType().GetField("emotionStarted").SetValue(indicator, Time.time - 2.1f);
        typeof(SquadIndicators).GetMethod("LateUpdate", PrivateInstance).Invoke(indicators, null);
        Assert.That(label.text, Is.EqualTo(Content(speech)));
        Assert.That(emoji.gameObject.activeSelf, Is.False);
    }
}
