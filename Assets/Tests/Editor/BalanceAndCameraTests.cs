using System;
using System.Collections.Generic;
using System.Reflection;
using MouseKeeper;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class BalanceAndCameraTests
{
    const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [TestCase(14f, 10f, 0, false, 3)]
    [TestCase(18f, 14f, 0, false, 4)]
    [TestCase(22f, 18f, 0, false, 5)]
    [TestCase(14f, 14f, 1, false, 3)]
    [TestCase(22f, 10f, 1, false, 4)]
    [TestCase(22f, 14f, 1, false, 5)]
    [TestCase(14f, 10f, 0, true, 1)]
    [TestCase(22f, 18f, 2, true, 2)]
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

    [TestCase(40f, 1, 60f)]
    [TestCase(200f, 1, 300f)]
    [TestCase(40f, 3, 69.6f)]
    [TestCase(40f, 10, 79.2f)]
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

    [TestCase(MouseEnemy.MouseVariant.Normal, true)]
    [TestCase(MouseEnemy.MouseVariant.Jumping, false)]
    [TestCase(MouseEnemy.MouseVariant.Tanky, false)]
    [TestCase(MouseEnemy.MouseVariant.Speedy, false)]
    public void OnlyNormalRatsFleeWhenHit(MouseEnemy.MouseVariant variant, bool expected)
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
            mouse.ConfigureVariant(variant);
            mouse.Initialize(run, agent, Vector3.zero, 1);
            mouse.TakeDamage(1f, agent);
            Assert.That((bool)typeof(MouseEnemy).GetField("fleeing", PrivateInstance).GetValue(mouse), Is.EqualTo(expected));
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

    [TestCase(0, 0f, MouseEnemy.MouseVariant.Normal)]
    [TestCase(1, 0.199f, MouseEnemy.MouseVariant.Jumping)]
    [TestCase(1, 0.2f, MouseEnemy.MouseVariant.Tanky)]
    [TestCase(1, 0.4f, MouseEnemy.MouseVariant.Speedy)]
    [TestCase(1, 0.601f, MouseEnemy.MouseVariant.Normal)]
    [TestCase(2, 0.333f, MouseEnemy.MouseVariant.Jumping)]
    [TestCase(2, 0.334f, MouseEnemy.MouseVariant.Tanky)]
    [TestCase(2, 0.667f, MouseEnemy.MouseVariant.Speedy)]
    [TestCase(2, 1f, MouseEnemy.MouseVariant.Speedy)]
    public void RareRatDistributionMatchesFloor(int floor, float roll, MouseEnemy.MouseVariant expected)
    {
        var method = typeof(GameRun).GetMethod("ChooseMouseVariant", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method.Invoke(null, new object[] { floor, roll }), Is.EqualTo(expected));
    }

    [TestCase(MouseEnemy.MouseVariant.Jumping, 120f, 12f, 2.4f)]
    [TestCase(MouseEnemy.MouseVariant.Tanky, 160f, 6.4f, 1.2f)]
    [TestCase(MouseEnemy.MouseVariant.Speedy, 40f, 8f, 4.8f)]
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
            mouse.ConfigureVariant(variant);
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
            mouse.ConfigureVariant(MouseEnemy.MouseVariant.Jumping);
            mouse.Initialize(run, first, Vector3.zero, 1);
            Physics.SyncTransforms();
            MethodInfo update = typeof(MouseEnemy).GetMethod("UpdateJump", PrivateInstance);
            FieldInfo target = typeof(MouseEnemy).GetField("jumpTarget", PrivateInstance);
            update.Invoke(mouse, new object[] { 0.9f });
            Assert.That(target.GetValue(mouse), Is.Null);
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
            mouse.ConfigureVariant(MouseEnemy.MouseVariant.Jumping);
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
            Assert.That(agent.Health, Is.EqualTo(initialHealth - 12f),
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
            typeof(MouseEnemy).GetField("hidden", PrivateInstance).SetValue(focus, true);
            Assert.That(run.SelectTarget(first), Is.SameAs(close));
            typeof(MouseEnemy).GetField("hidden", PrivateInstance).SetValue(focus, false);
            Assert.That(run.SelectTarget(first), Is.SameAs(focus));
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
            mouse.Initialize(run, agent, Vector3.zero, 1);
            var weapon = new WeaponInstance(definition);
            FieldInfo health = typeof(MouseEnemy).GetField("health", PrivateInstance);

            mouse.TakeDamage(20f, agent, weapon);
            Assert.That((float)health.GetValue(mouse), Is.EqualTo(66f));
            mouse.TakeDamage(15f, agent, weapon);
            Assert.That((float)health.GetValue(mouse), Is.EqualTo(55f));
            mouse.TakeDamage(20f, agent);
            Assert.That((float)health.GetValue(mouse), Is.EqualTo(35f));
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
