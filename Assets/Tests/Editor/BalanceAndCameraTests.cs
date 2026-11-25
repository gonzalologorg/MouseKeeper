using System;
using System.Collections.Generic;
using System.Reflection;
using MouseKeeper;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

public sealed class BalanceAndCameraTests
{
    const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [Test]
    public void RatIsDetectedOnAgentButNotBesideOrPassingAbove()
    {
        var agent = new Bounds(new Vector3(0f, 0.9f, 0f), new Vector3(0.8f, 1.8f, 0.8f));
        var rat = new Bounds(new Vector3(0f, 2.3f, 0f), new Vector3(0.7f, 1f, 0.7f));
        Assert.That(MouseEnemy.IsStandingOnAgent(rat, agent), Is.True);
        rat.center = new Vector3(1.2f, 2.3f, 0f);
        Assert.That(MouseEnemy.IsStandingOnAgent(rat, agent), Is.False);
        rat.center = new Vector3(0f, 3f, 0f);
        Assert.That(MouseEnemy.IsStandingOnAgent(rat, agent), Is.False);
        rat.center = new Vector3(0.6f, 0.5f, 0f);
        Assert.That(MouseEnemy.IsStandingOnAgent(rat, agent), Is.False);
    }

    [Test]
    public void BlasterUsesDartPrefabAndRoomGeometryUsesWorldLayer()
    {
        var weapon = AssetDatabase.LoadAssetAtPath<RaytraceWeaponDefinition>("Assets/Data/Weapons/MouseBlaster.asset");
        var dart = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/FX/Dart.prefab");
        Assert.That(weapon, Is.Not.Null);
        Assert.That(dart, Is.Not.Null);
        Assert.That(weapon.projectileEffect, Is.SameAs(dart));
        Assert.That(dart.GetComponentInChildren<ParticleSystem>(), Is.Not.Null);
        int world = LayerMask.NameToLayer("World");
        Assert.That(world, Is.GreaterThanOrEqualTo(0));
        var room = new GameObject("Room");
        try
        {
            var wall = new GameObject("Wall");
            wall.transform.SetParent(room.transform);
            var collider = wall.AddComponent<BoxCollider>();
            typeof(GameRun).GetMethod("SetWorldLayer", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { room });
            Assert.That(room.layer, Is.EqualTo(world));
            Assert.That(collider.gameObject.layer, Is.EqualTo(world));
        }
        finally { Object.DestroyImmediate(room); }
    }

    [TestCase(14f, 10f, 0, false, 11)]
    [TestCase(18f, 14f, 0, false, 15)]
    [TestCase(22f, 18f, 0, false, 19)]
    [TestCase(14f, 14f, 1, false, 11)]
    [TestCase(22f, 10f, 1, false, 15)]
    [TestCase(22f, 14f, 1, false, 19)]
    [TestCase(14f, 10f, 0, true, 1)]
    [TestCase(22f, 18f, 2, true, 1)]
    public void MouseCountScalesWithRoomAreaAndFloor(float width, float depth, int floor, bool boss, int expected)
    {
        var runObject = new GameObject("Test run");
        try
        {
            var run = runObject.AddComponent<GameRun>();
            Type roomType = typeof(GameRun).GetNestedType("Room", BindingFlags.NonPublic);
            object room = Activator.CreateInstance(roomType, true);
            roomType.GetField("width").SetValue(room, width);
            roomType.GetField("depthSize").SetValue(room, depth);
            typeof(GameRun).GetField("levelIndex", PrivateInstance).SetValue(run, floor);
            if (boss) typeof(GameRun).GetField("bossRoom", PrivateInstance).SetValue(run, room);
            int count = (int)typeof(GameRun).GetMethod("MouseCountForRoom", PrivateInstance).Invoke(run, new[] { room });
            Assert.That(count, Is.EqualTo(expected));
        }
        finally { Object.DestroyImmediate(runObject); }
    }

    [Test]
    public void BossIsExcludedFromRatLimitAndMushroomSplitsRespectIt()
    {
        Type roomType = typeof(GameRun).GetNestedType("Room", BindingFlags.NonPublic);
        object room = Activator.CreateInstance(roomType, true);
        var bossObject = new GameObject("Boss");
        try
        {
            var boss = bossObject.AddComponent<MouseEnemy>();
            roomType.GetField("boss").SetValue(room, boss);
            FieldInfo enemies = roomType.GetField("enemies");
            MethodInfo ratCount = typeof(GameRun).GetMethod("BossRatCount", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo offspringCount = typeof(GameRun).GetMethod("BossOffspringCount", BindingFlags.Static | BindingFlags.NonPublic);
            enemies.SetValue(room, 6);
            Assert.That(ratCount.Invoke(null, new[] { room }), Is.EqualTo(5));
            Assert.That(offspringCount.Invoke(null, new[] { room }), Is.EqualTo(1));
            enemies.SetValue(room, 5);
            Assert.That(ratCount.Invoke(null, new[] { room }), Is.EqualTo(4));
            Assert.That(offspringCount.Invoke(null, new[] { room }), Is.EqualTo(2));
            typeof(MouseEnemy).GetField("defeated", PrivateInstance).SetValue(boss, true);
            Assert.That(ratCount.Invoke(null, new[] { room }), Is.EqualTo(5));
            typeof(MouseEnemy).GetField("defeated", PrivateInstance).SetValue(boss, false);
        }
        finally { Object.DestroyImmediate(bossObject); }
    }

    [Test]
    public void BossHealthAndDamageThresholdsScaleWithoutLosingCrossedSteps()
    {
        var root = new GameObject("Run");
        var bossObject = new GameObject("Boss");
        try
        {
            var run = root.AddComponent<GameRun>();
            var boss = bossObject.AddComponent<MouseEnemy>();
            boss.maxHealth = 100f;
            Type roomType = typeof(GameRun).GetNestedType("Room", BindingFlags.NonPublic);
            object room = Activator.CreateInstance(roomType, true);
            roomType.GetField("boss").SetValue(room, boss);
            typeof(GameRun).GetField("currentRoom", PrivateInstance).SetValue(run, room);
            MethodInfo multiplier = typeof(GameRun).GetMethod("BossHealthMultiplier", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(multiplier.Invoke(null, new object[] { 1 }), Is.EqualTo(3f));
            Assert.That(multiplier.Invoke(null, new object[] { 3 }), Is.EqualTo(9f));
            MethodInfo queue = typeof(GameRun).GetMethod("QueueBossDamageSpawns", PrivateInstance);
            queue.Invoke(run, new object[] { boss, 90f });
            Assert.That(roomType.GetField("pendingBossRats").GetValue(room), Is.EqualTo(1));
            queue.Invoke(run, new object[] { boss, 55f });
            Assert.That(roomType.GetField("pendingBossRats").GetValue(room), Is.EqualTo(4));
            queue.Invoke(run, new object[] { boss, 55f });
            queue.Invoke(run, new object[] { boss, 0f });
            Assert.That(roomType.GetField("pendingBossRats").GetValue(room), Is.EqualTo(10));
            Assert.That(roomType.GetField("spawning").GetValue(room), Is.True);
            MethodInfo points = typeof(GameRun).GetMethod("MouseScore", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(points.Invoke(null, new object[] { false, MouseEnemy.MouseVariant.Mushroom, 0 }), Is.EqualTo(25d));
            Assert.That(points.Invoke(null, new object[] { false, MouseEnemy.MouseVariant.Mushroom, 1 }), Is.EqualTo(12.5d));
            Assert.That(points.Invoke(null, new object[] { true, MouseEnemy.MouseVariant.Mushroom, 1 }), Is.EqualTo(100d));
        }
        finally { Object.DestroyImmediate(bossObject); Object.DestroyImmediate(root); }
    }

    [Test]
    public void ExplosivePresetHasExtraHealthAndDoubleRadius()
    {
        EnemyPreset preset = AssetDatabase.LoadAssetAtPath<EnemyPreset>("Assets/Data/Enemies/Explosive.asset");
        Assert.That(preset.maxHealth, Is.EqualTo(60f));
        Assert.That(preset.explosionRadius, Is.EqualTo(6f));
    }

    [TestCase(40f, 1, 24f)]
    [TestCase(200f, 1, 120f)]
    [TestCase(40f, 3, 27.84f)]
    [TestCase(40f, 10, 31.68f)]
    public void MouseHealthUsesGentleCappedDepthScaling(float initialHealth, int depth, float expectedHealth)
    {
        var runObject = new GameObject("Test run");
        var agentObject = new GameObject("Test agent");
        var mouseObject = new GameObject("Test mouse");
        try
        {
            var run = runObject.AddComponent<GameRun>();
            var agent = agentObject.AddComponent<PlayerController>();
            var mouse = mouseObject.AddComponent<MouseEnemy>();
            mouse.maxHealth = initialHealth;
            mouse.Initialize(run, agent, Vector3.zero, depth);
            Assert.That(mouse.maxHealth, Is.EqualTo(expectedHealth).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(mouseObject);
            Object.DestroyImmediate(agentObject);
            Object.DestroyImmediate(runObject);
        }
    }

    [Test]
    public void AccuracyMattersMoreAtLongRange()
    {
        Assert.That(PlayerController.AimErrorDegrees(1, 2f), Is.EqualTo(3f).Within(0.001f));
        Assert.That(PlayerController.AimErrorDegrees(1, 12f), Is.EqualTo(30f).Within(0.001f));
        Assert.That(PlayerController.AimErrorDegrees(10, 12f), Is.Zero);
        Assert.That(PlayerController.AimErrorDegrees(7, 12f), Is.LessThan(PlayerController.AimErrorDegrees(3, 12f)));
    }

    [TestCase(MouseEnemy.MouseVariant.Normal)]
    [TestCase(MouseEnemy.MouseVariant.Jumping)]
    [TestCase(MouseEnemy.MouseVariant.Tanky)]
    [TestCase(MouseEnemy.MouseVariant.Speedy)]
    [TestCase(MouseEnemy.MouseVariant.Explosive)]
    [TestCase(MouseEnemy.MouseVariant.Poison)]
    [TestCase(MouseEnemy.MouseVariant.Mushroom)]
    public void RatsRemainTargetableAfterBeingHit(MouseEnemy.MouseVariant variant)
    {
        var runObject = new GameObject("Test run");
        var agentObject = new GameObject("Test agent");
        var mouseObject = new GameObject("Test mouse");
        var cameraObject = new GameObject("Test camera");
        DamageIndicators indicators = null;
        try
        {
            var run = runObject.AddComponent<GameRun>();
            indicators = runObject.AddComponent<DamageIndicators>();
            indicators.Initialize(cameraObject.AddComponent<Camera>());
            typeof(GameRun).GetField("damageIndicators", PrivateInstance).SetValue(run, indicators);
            var agent = agentObject.AddComponent<PlayerController>();
            agent.Initialize(run, new AgentProfile());
            var mouse = mouseObject.AddComponent<MouseEnemy>();
            mouse.SetBehavior(variant);
            mouse.Initialize(run, agent, Vector3.zero, 1);
            mouse.TakeDamage(1f, agent);
            Assert.That(mouse.IsTargetable, Is.True);
        }
        finally
        {
            if (indicators != null)
            {
                var canvas = (Canvas)typeof(DamageIndicators).GetField("canvas", PrivateInstance).GetValue(indicators);
                if (canvas != null) Object.DestroyImmediate(canvas.gameObject);
            }
            Object.DestroyImmediate(mouseObject);
            Object.DestroyImmediate(agentObject);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(runObject);
        }
    }

    [TestCase(0, 0.01f, MouseEnemy.MouseVariant.Jumping)]
    [TestCase(0, 0.05f, MouseEnemy.MouseVariant.Tanky)]
    [TestCase(0, 0.1f, MouseEnemy.MouseVariant.Speedy)]
    [TestCase(0, 0.2f, MouseEnemy.MouseVariant.Normal)]
    [TestCase(1, 0.1f, MouseEnemy.MouseVariant.Jumping)]
    [TestCase(1, 0.2f, MouseEnemy.MouseVariant.Tanky)]
    [TestCase(1, 0.3f, MouseEnemy.MouseVariant.Speedy)]
    [TestCase(1, 0.5f, MouseEnemy.MouseVariant.Normal)]
    [TestCase(1, 0.85f, MouseEnemy.MouseVariant.Explosive)]
    [TestCase(1, 0.9f, MouseEnemy.MouseVariant.Poison)]
    [TestCase(2, 1f, MouseEnemy.MouseVariant.Mushroom)]
    public void RareRatDistributionMatchesFloor(int floor, float roll, MouseEnemy.MouseVariant expected)
    {
        var pool = AssetDatabase.LoadAssetAtPath<EnemySpawnPool>("Assets/Data/Enemies/DefaultSpawnPool.asset");
        Assert.That(pool, Is.Not.Null);
        Assert.That(pool.Choose(floor, roll).behavior, Is.EqualTo(expected));
    }

    [Test]
    public void SpawnPoolUsesRelativeWeightsAndSkipsDisabledEntries()
    {
        var pool = ScriptableObject.CreateInstance<EnemySpawnPool>();
        var first = ScriptableObject.CreateInstance<EnemyPreset>();
        var second = ScriptableObject.CreateInstance<EnemyPreset>();
        try
        {
            pool.enemies = new[] {
                new EnemySpawnPool.Entry { enemy = first, floorWeights = new[] { 1f, 0f } },
                new EnemySpawnPool.Entry { enemy = second, floorWeights = new[] { 3f, 2f } }
            };
            Assert.That(pool.Choose(0, 0.249f), Is.SameAs(first));
            Assert.That(pool.Choose(0, 0.25f), Is.SameAs(second));
            Assert.That(pool.Choose(3, 0f), Is.SameAs(second));
            pool.enemies[1].floorWeights[1] = 0f;
            Assert.That(pool.Choose(1, 0f), Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
            Object.DestroyImmediate(pool);
        }
    }

    [Test]
    public void FoamSlowBuildsAndCapsAtEightyPercent()
    {
        var obj = new GameObject("Foam target");
        try
        {
            MouseEnemy rat = obj.AddComponent<MouseEnemy>();
            rat.ApplySlow(2f);
            Assert.That(rat.SlowFraction, Is.EqualTo(0.2f).Within(0.001f));
            for (int i = 0; i < 8; i++) rat.ApplySlow(2f);
            Assert.That(rat.SlowFraction, Is.EqualTo(0.8f).Within(0.001f));
        }
        finally { Object.DestroyImmediate(obj); }
    }

    [Test]
    public void MushroomOffspringSplitOnlyOnSuccessfulRoll()
    {
        MethodInfo canSplit = typeof(GameRun).GetMethod("MushroomCanSplit", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(canSplit.Invoke(null, new object[] { 0, 0.99f }), Is.True);
        Assert.That(canSplit.Invoke(null, new object[] { 1, 0.49f }), Is.True);
        Assert.That(canSplit.Invoke(null, new object[] { 2, 0.5f }), Is.False);
        Assert.That(canSplit.Invoke(null, new object[] { 3, 0f }), Is.False);
    }

    [Test]
    public void MushroomOffspringKeepSixtyFivePercentHealthEachGeneration()
    {
        var parentObject = new GameObject("Parent");
        var childObject = new GameObject("Child");
        var grandchildObject = new GameObject("Grandchild");
        var finalObject = new GameObject("Final generation");
        try
        {
            var parent = parentObject.AddComponent<MouseEnemy>();
            var child = childObject.AddComponent<MouseEnemy>();
            var grandchild = grandchildObject.AddComponent<MouseEnemy>();
            var final = finalObject.AddComponent<MouseEnemy>();
            parent.maxHealth = 80f;
            parent.movementSpeed = 5f;
            child.InitializeOffspring(parent);
            grandchild.InitializeOffspring(child);
            final.InitializeOffspring(grandchild);
            Assert.That(child.SplitGeneration, Is.EqualTo(1));
            Assert.That(grandchild.SplitGeneration, Is.EqualTo(2));
            Assert.That(child.maxHealth, Is.EqualTo(80f * 0.65f).Within(0.001f));
            Assert.That(grandchild.maxHealth, Is.EqualTo(80f * 0.65f * 0.65f).Within(0.001f));
            Assert.That(child.movementSpeed, Is.EqualTo(6f).Within(0.001f));
            Assert.That(grandchild.movementSpeed, Is.EqualTo(7f).Within(0.001f));
            Assert.That(final.movementSpeed, Is.EqualTo(8f).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(parentObject);
            Object.DestroyImmediate(childObject);
            Object.DestroyImmediate(grandchildObject);
            Object.DestroyImmediate(finalObject);
        }
    }

    [TestCase(MouseEnemy.MouseVariant.Jumping, 36f, 4.8f, 2.4f)]
    [TestCase(MouseEnemy.MouseVariant.Tanky, 48f, 2.56f, 1.2f)]
    [TestCase(MouseEnemy.MouseVariant.Speedy, 12f, 3.2f, 4.8f)]
    [TestCase(MouseEnemy.MouseVariant.Explosive, 36f, 2.56f, 1.2f)]
    public void RareRatStatsScaleFromTheNormalRat(
        MouseEnemy.MouseVariant variant, float health, float damage, float speed)
    {
        var runObject = new GameObject("Test run");
        var agentObject = new GameObject("Test agent");
        var mouseObject = new GameObject("Test mouse");
        try
        {
            var run = runObject.AddComponent<GameRun>();
            var agent = agentObject.AddComponent<PlayerController>();
            var mouse = mouseObject.AddComponent<MouseEnemy>();
            var preset = AssetDatabase.LoadAssetAtPath<EnemyPreset>("Assets/Data/Enemies/" + variant + ".asset");
            Assert.That(preset, Is.Not.Null);
            preset.ApplyTo(mouse);
            mouse.Initialize(run, agent, Vector3.zero, 1);
            Assert.That(mouse.maxHealth, Is.EqualTo(health).Within(0.001f));
            Assert.That(mouse.attackDamage, Is.EqualTo(damage).Within(0.001f));
            Assert.That(mouse.movementSpeed, Is.EqualTo(speed).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(mouseObject);
            Object.DestroyImmediate(agentObject);
            Object.DestroyImmediate(runObject);
        }
    }

    [Test]
    public void JumpingRatLeapsEverySecondAndAlternatesTargets()
    {
        var runObject = new GameObject("Test run");
        var firstObject = new GameObject("First agent");
        var secondObject = new GameObject("Second agent");
        var mouseObject = new GameObject("Jumping rat");
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(40f, 1f, 40f);
            firstObject.transform.position = new Vector3(8f, 1f, 0f);
            secondObject.transform.position = new Vector3(12f, 1f, 0f);
            mouseObject.transform.position = new Vector3(0f, 1f, 0f);
            var run = runObject.AddComponent<GameRun>();
            var first = firstObject.AddComponent<PlayerController>();
            var second = secondObject.AddComponent<PlayerController>();
            var agents = (List<PlayerController>)typeof(GameRun).GetField("agents", PrivateInstance).GetValue(run);
            agents.Add(first);
            agents.Add(second);
            var mouse = mouseObject.AddComponent<MouseEnemy>();
            AssetDatabase.LoadAssetAtPath<EnemyPreset>("Assets/Data/Enemies/Jumping.asset").ApplyTo(mouse);
            mouse.Initialize(run, first, Vector3.zero, 1);
            Physics.SyncTransforms();
            MethodInfo update = typeof(MouseEnemy).GetMethod("UpdateJump", PrivateInstance);
            FieldInfo target = typeof(MouseEnemy).GetField("jumpTarget", PrivateInstance);
            update.Invoke(mouse, new object[] { 0.11f });
            Assert.That(target.GetValue(mouse), Is.SameAs(first));
            Assert.That(mouse.transform.position.y, Is.GreaterThan(1f));
            for (int i = 0; i < 6; i++) update.Invoke(mouse, new object[] { 0.1f });
            Assert.That(mouse.transform.position.x, Is.GreaterThan(5f));
            update.Invoke(mouse, new object[] { 0.2f });
            Assert.That(target.GetValue(mouse), Is.SameAs(first));
            update.Invoke(mouse, new object[] { 0.21f });
            Assert.That(target.GetValue(mouse), Is.SameAs(second));
        }
        finally
        {
            Object.DestroyImmediate(floor);
            Object.DestroyImmediate(mouseObject);
            Object.DestroyImmediate(firstObject);
            Object.DestroyImmediate(secondObject);
            Object.DestroyImmediate(runObject);
        }
    }

    [Test]
    public void JumpingRatCanBeDodgedAndAttacksTheLastSurvivor()
    {
        var runObject = new GameObject("Test run");
        var agentObject = new GameObject("Only agent");
        var mouseObject = new GameObject("Jumping rat");
        var cameraObject = new GameObject("Test camera");
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        DamageIndicators indicators = null;
        try
        {
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(40f, 1f, 40f);
            agentObject.transform.position = new Vector3(4f, 1f, 0f);
            mouseObject.transform.position = new Vector3(0f, 1f, 0f);
            var run = runObject.AddComponent<GameRun>();
            indicators = runObject.AddComponent<DamageIndicators>();
            indicators.Initialize(cameraObject.AddComponent<Camera>());
            typeof(GameRun).GetField("damageIndicators", PrivateInstance).SetValue(run, indicators);
            var agent = agentObject.AddComponent<PlayerController>();
            agent.Initialize(run, new AgentProfile());
            ((List<PlayerController>)typeof(GameRun).GetField("agents", PrivateInstance).GetValue(run)).Add(agent);
            var mouse = mouseObject.AddComponent<MouseEnemy>();
            AssetDatabase.LoadAssetAtPath<EnemyPreset>("Assets/Data/Enemies/Jumping.asset").ApplyTo(mouse);
            mouse.Initialize(run, agent, Vector3.zero, 1);
            Physics.SyncTransforms();
            MethodInfo update = typeof(MouseEnemy).GetMethod("UpdateJump", PrivateInstance);
            FieldInfo cooldown = typeof(MouseEnemy).GetField("jumpCooldown", PrivateInstance);
            float initialHealth = agent.Health;

            cooldown.SetValue(mouse, 0f);
            update.Invoke(mouse, new object[] { 0.1f });
            agentObject.transform.position = new Vector3(8f, 1f, 0f);
            Physics.SyncTransforms();
            for (int i = 0; i < 6; i++) update.Invoke(mouse, new object[] { 0.1f });
            Assert.That(agent.Health, Is.EqualTo(initialHealth));

            agentObject.transform.position = mouseObject.transform.position + Vector3.right * 2f;
            Physics.SyncTransforms();
            cooldown.SetValue(mouse, 0f);
            for (int i = 0; i < 7; i++) update.Invoke(mouse, new object[] { 0.1f });
            Assert.That(agent.Health, Is.EqualTo(initialHealth - 5f),
                "Rat position: " + mouse.transform.position + "; agent position: " + agent.transform.position +
                "; rat bounds: " + mouse.GetComponent<CharacterController>().bounds +
                "; agent bounds: " + agent.GetComponent<CharacterController>().bounds);
            Assert.That(mouse.RecentlyAttacked(agent), Is.True);
        }
        finally
        {
            if (indicators != null)
            {
                var canvas = (Canvas)typeof(DamageIndicators).GetField("canvas", PrivateInstance).GetValue(indicators);
                if (canvas != null) Object.DestroyImmediate(canvas.gameObject);
            }
            Object.DestroyImmediate(floor);
            Object.DestroyImmediate(mouseObject);
            Object.DestroyImmediate(agentObject);
            Object.DestroyImmediate(runObject);
            Object.DestroyImmediate(cameraObject);
        }
    }

    [Test]
    public void FocusOrderOverridesTargetChoiceUntilDefeatAndSurvivesHiding()
    {
        var runObject = new GameObject("Test run");
        var roomRoot = new GameObject("Room");
        var outsiderRoot = new GameObject("Other room");
        var firstObject = new GameObject("First agent");
        var secondObject = new GameObject("Second agent");
        try
        {
            var run = runObject.AddComponent<GameRun>();
            Type roomType = typeof(GameRun).GetNestedType("Room", BindingFlags.NonPublic);
            object room = Activator.CreateInstance(roomType, true);
            roomType.GetField("root").SetValue(room, roomRoot);
            roomType.GetField("enemies").SetValue(room, 2);
            typeof(GameRun).GetField("currentRoom", PrivateInstance).SetValue(run, room);
            var first = firstObject.AddComponent<PlayerController>();
            var second = secondObject.AddComponent<PlayerController>();
            first.Initialize(run, new AgentProfile());
            second.Initialize(run, new AgentProfile());
            secondObject.transform.position = Vector3.left;
            var agents = (List<PlayerController>)typeof(GameRun).GetField("agents", PrivateInstance).GetValue(run);
            agents.Add(first);
            agents.Add(second);
            var close = new GameObject("Close rat").AddComponent<MouseEnemy>();
            close.transform.SetParent(roomRoot.transform);
            close.transform.position = Vector3.right;
            var focus = new GameObject("Focused rat").AddComponent<MouseEnemy>();
            focus.transform.SetParent(roomRoot.transform);
            focus.transform.position = Vector3.right * 5f;
            var outsider = new GameObject("Outside rat").AddComponent<MouseEnemy>();
            outsider.transform.SetParent(outsiderRoot.transform);
            first.SetDestination(Vector3.forward);
            second.SetDestination(Vector3.back);

            Assert.That(run.FocusMouse(outsider), Is.False);
            Assert.That(run.FocusMouse(focus), Is.True);
            Assert.That(first.ReachedDestination && second.ReachedDestination, Is.True);
            Assert.That(run.SelectTarget(first), Is.SameAs(focus));
            Assert.That(run.SelectTarget(second), Is.SameAs(focus));
            run.MouseDefeated(focus);
            Assert.That(run.SelectTarget(first), Is.SameAs(close));
        }
        finally
        {
            Object.DestroyImmediate(roomRoot);
            Object.DestroyImmediate(outsiderRoot);
            Object.DestroyImmediate(firstObject);
            Object.DestroyImmediate(secondObject);
            Object.DestroyImmediate(runObject);
        }
    }

    [Test]
    public void FormationsShareOneTargetOrChooseEachAgentsNearestEnemy()
    {
        var runObject = new GameObject("Test run");
        var roomRoot = new GameObject("Room");
        var firstObject = new GameObject("First agent");
        var secondObject = new GameObject("Second agent");
        try
        {
            var run = runObject.AddComponent<GameRun>();
            Type roomType = typeof(GameRun).GetNestedType("Room", BindingFlags.NonPublic);
            object room = Activator.CreateInstance(roomType, true);
            roomType.GetField("root").SetValue(room, roomRoot);
            typeof(GameRun).GetField("currentRoom", PrivateInstance).SetValue(run, room);
            var first = firstObject.AddComponent<PlayerController>();
            var second = secondObject.AddComponent<PlayerController>();
            first.Initialize(run, new AgentProfile());
            second.Initialize(run, new AgentProfile());
            firstObject.transform.position = Vector3.left * 6f;
            secondObject.transform.position = Vector3.right * 6f;
            var agents = (List<PlayerController>)typeof(GameRun).GetField("agents", PrivateInstance).GetValue(run);
            agents.Add(first);
            agents.Add(second);
            var left = new GameObject("Left rat").AddComponent<MouseEnemy>();
            left.transform.SetParent(roomRoot.transform);
            left.transform.position = Vector3.left * 5f;
            var right = new GameObject("Right rat").AddComponent<MouseEnemy>();
            right.transform.SetParent(roomRoot.transform);
            right.transform.position = Vector3.right * 5f;

            run.SetFormation(GameRun.SquadFormation.Focus);
            Assert.That(run.SelectTarget(first), Is.SameAs(run.SelectTarget(second)));
            run.SetFormation(GameRun.SquadFormation.Nearest);
            Assert.That(run.SelectTarget(first), Is.SameAs(left));
            Assert.That(run.SelectTarget(second), Is.SameAs(right));
            run.SetFormation(GameRun.SquadFormation.KeepDistance);
            Assert.That(run.SelectTarget(first), Is.SameAs(left));
            Assert.That(run.SelectTarget(second), Is.SameAs(right));
            Assert.That(run.FocusMouse(right), Is.True);
            Assert.That(run.Formation, Is.EqualTo(GameRun.SquadFormation.Focus));
            Assert.That(run.SelectTarget(first), Is.SameAs(right));
        }
        finally
        {
            Object.DestroyImmediate(roomRoot);
            Object.DestroyImmediate(firstObject);
            Object.DestroyImmediate(secondObject);
            Object.DestroyImmediate(runObject);
        }
    }

    [TestCase(15f, 2f, true)]
    [TestCase(15f, 8f, false)]
    [TestCase(2f, 0.8f, true)]
    public void DistanceFormationRetreatsWhenEnemyIsTooClose(float range, float distance, bool retreats)
    {
        var runObject = new GameObject("Test run");
        var roomRoot = new GameObject("Room");
        var agentObject = new GameObject("Agent");
        try
        {
            var run = runObject.AddComponent<GameRun>();
            Type roomType = typeof(GameRun).GetNestedType("Room", BindingFlags.NonPublic);
            object room = Activator.CreateInstance(roomType, true);
            roomType.GetField("root").SetValue(room, roomRoot);
            roomType.GetField("width").SetValue(room, 40f);
            roomType.GetField("depthSize").SetValue(room, 40f);
            typeof(GameRun).GetField("currentRoom", PrivateInstance).SetValue(run, room);
            var agent = agentObject.AddComponent<PlayerController>();
            agent.Initialize(run, new AgentProfile());
            Physics.SyncTransforms();
            typeof(PlayerController).GetMethod("MaintainFormationDistance", PrivateInstance)
                .Invoke(agent, new object[] { Vector3.right * distance, range, 0.2f });
            if (retreats) Assert.That(agent.transform.position.x, Is.LessThan(-0.1f));
            else Assert.That(agent.transform.position.x, Is.EqualTo(0f).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(roomRoot);
            Object.DestroyImmediate(agentObject);
            Object.DestroyImmediate(runObject);
        }
    }

    [Test]
    public void SelectionTriggerCoversRatModelWithoutBlockingWeaponRaycasts()
    {
        var runObject = new GameObject("Test run");
        var agentObject = new GameObject("Test agent");
        var mouseObject = new GameObject("Test rat");
        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            visual.transform.SetParent(mouseObject.transform);
            visual.transform.localPosition = Vector3.up * 2f;
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            var run = runObject.AddComponent<GameRun>();
            var mouse = mouseObject.AddComponent<MouseEnemy>();
            mouse.Initialize(run, agentObject.AddComponent<PlayerController>(), Vector3.zero, 1);
            Physics.SyncTransforms();
            Ray ray = new Ray(new Vector3(-3f, 2f, 0f), Vector3.right);
            Assert.That(Physics.Raycast(ray, out RaycastHit hit, 6f, ~0, QueryTriggerInteraction.Collide), Is.True);
            Assert.That(hit.collider.GetComponentInParent<MouseEnemy>(), Is.SameAs(mouse));
            Assert.That(hit.collider.isTrigger, Is.True);
            Assert.That(Physics.Raycast(ray, 6f, ~0, QueryTriggerInteraction.Ignore), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(mouseObject);
            Object.DestroyImmediate(agentObject);
            Object.DestroyImmediate(runObject);
        }
    }

    [Test]
    public void WeaponDamageIsReducedBeforeRoundingButPassiveDamageIsNot()
    {
        var runObject = new GameObject("Test run");
        var agentObject = new GameObject("Test agent");
        var mouseObject = new GameObject("Test mouse");
        var cameraObject = new GameObject("Test camera");
        var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
        DamageIndicators indicators = null;
        try
        {
            var run = runObject.AddComponent<GameRun>();
            indicators = runObject.AddComponent<DamageIndicators>();
            indicators.Initialize(cameraObject.AddComponent<Camera>());
            typeof(GameRun).GetField("damageIndicators", PrivateInstance).SetValue(run, indicators);
            var agent = agentObject.AddComponent<PlayerController>();
            agent.Initialize(run, new AgentProfile());
            var mouse = mouseObject.AddComponent<MouseEnemy>();
            mouse.maxHealth = 200f;
            mouse.Initialize(run, agent, Vector3.zero, 1);
            var weapon = new WeaponInstance(definition);
            FieldInfo health = typeof(MouseEnemy).GetField("health", PrivateInstance);

            mouse.TakeDamage(20f, agent, weapon);
            Assert.That((float)health.GetValue(mouse), Is.EqualTo(106f).Within(0.001f));
            mouse.TakeDamage(15f, agent, weapon);
            Assert.That((float)health.GetValue(mouse), Is.EqualTo(95f).Within(0.001f));
            mouse.TakeDamage(20f, agent);
            Assert.That((float)health.GetValue(mouse), Is.EqualTo(75f).Within(0.001f));
        }
        finally
        {
            if (indicators != null)
            {
                var canvas = (Canvas)typeof(DamageIndicators).GetField("canvas", PrivateInstance).GetValue(indicators);
                if (canvas != null) Object.DestroyImmediate(canvas.gameObject);
            }
            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(mouseObject);
            Object.DestroyImmediate(agentObject);
            Object.DestroyImmediate(runObject);
            Object.DestroyImmediate(cameraObject);
        }
    }

    [TestCase(8f, 8f, 1.777778f)]
    [TestCase(18f, 14f, 1.777778f)]
    [TestCase(30f, 10f, 0.75f)]
    public void CameraFramesCurrentRoom(float width, float depth, float aspect)
    {
        var runObject = new GameObject("Test run");
        var cameraObject = new GameObject("Test camera");
        try
        {
            var run = runObject.AddComponent<GameRun>();
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.aspect = aspect;
            camera.transform.rotation = Quaternion.Euler(50f, 45f, 0f);
            typeof(GameRun).GetField("viewCamera", PrivateInstance).SetValue(run, camera);
            Type roomType = typeof(GameRun).GetNestedType("Room", BindingFlags.NonPublic);
            object room = Activator.CreateInstance(roomType, true);
            Vector3 center = new Vector3(-35f, 0f, 50f);
            roomType.GetField("center").SetValue(room, center);
            roomType.GetField("width").SetValue(room, width);
            roomType.GetField("depthSize").SetValue(room, depth);
            typeof(GameRun).GetMethod("FitCameraToRoom", PrivateInstance).Invoke(run, new[] { room });
            Vector3 focus = (Vector3)typeof(GameRun).GetField("cameraFocus", PrivateInstance).GetValue(run);
            float distance = (float)typeof(GameRun).GetField("cameraDistance", PrivateInstance).GetValue(run);
            camera.transform.position = focus - camera.transform.forward * distance;

            float minX = 1f, maxX = 0f, minY = 1f, maxY = 0f;
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    for (int y = 0; y <= 1; y++)
                    {
                        Vector3 point = center + new Vector3(x * width * 0.5f, y * 3.9f, z * depth * 0.5f);
                        Vector3 viewport = camera.WorldToViewportPoint(point);
                        Assert.That(viewport.x, Is.InRange(0f, 1f));
                        Assert.That(viewport.y, Is.InRange(0f, 1f));
                        Assert.That(viewport.z, Is.GreaterThan(0f));
                        minX = Mathf.Min(minX, viewport.x); maxX = Mathf.Max(maxX, viewport.x);
                        minY = Mathf.Min(minY, viewport.y); maxY = Mathf.Max(maxY, viewport.y);
                    }
            Assert.That(Mathf.Max(maxX - minX, maxY - minY), Is.GreaterThan(0.7f));
        }
        finally
        {
            Object.DestroyImmediate(runObject);
            Object.DestroyImmediate(cameraObject);
        }
    }
}
