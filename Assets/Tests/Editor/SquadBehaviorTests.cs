using MouseKeeper;
using NUnit.Framework;
using UnityEngine;

public sealed class SquadBehaviorTests
{
    WeaponDefinition definition;

    [SetUp]
    public void SetUp()
    {
        definition = ScriptableObject.CreateInstance<WeaponDefinition>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(definition);
    }

    [Test]
    public void AttackSpeedChangesWeaponCooldown()
    {
        var normal = new WeaponInstance(definition, 1f, 5);
        var fast = new WeaponInstance(definition, 2f, 5);
        Assert.That(normal.TrySpend(1f, 1f), Is.True);
        Assert.That(fast.TrySpend(1f, 1f), Is.True);
        normal.Tick(0.6f, 0f);
        fast.Tick(0.6f, 0f);
        Assert.That(normal.TrySpend(1f, 1f), Is.False);
        Assert.That(fast.TrySpend(1f, 1f), Is.True);
    }

    [Test]
    public void AgilityShortensReload()
    {
        var slow = new WeaponInstance(definition, 1f, 1);
        var agile = new WeaponInstance(definition, 1f, 10);
        Assert.That(slow.TrySpend(1f, 0f), Is.True);
        Assert.That(agile.TrySpend(1f, 0f), Is.True);
        slow.StartReload(1f);
        agile.StartReload(1f);
        slow.Tick(0.75f, 0f);
        agile.Tick(0.75f, 0f);
        Assert.That(slow.IsReloading, Is.True);
        Assert.That(agile.IsReloading, Is.False);
    }

    [Test]
    public void SlowAgentsGainHealthWithoutChangingFasterAgents()
    {
        var root = new GameObject("Test agent");
        var runObject = new GameObject("Test run");
        try
        {
            var agent = root.AddComponent<PlayerController>();
            var run = runObject.AddComponent<GameRun>();
            var profile = new AgentProfile { health = 100f, speed = 3f };
            agent.Initialize(run, profile);
            Assert.That(agent.MaxHealth, Is.EqualTo(175f));
            Assert.That(agent.Health, Is.EqualTo(175f));
            Assert.That(profile.health, Is.EqualTo(100f));
            Assert.That(new AgentProfile { health = 100f, speed = 2f }.EffectiveHealth, Is.EqualTo(250f));
            Assert.That(new AgentProfile { health = 100f, speed = 4f }.EffectiveHealth, Is.EqualTo(100f));
            Assert.That(new AgentProfile { health = 100f, speed = 8f }.EffectiveHealth, Is.EqualTo(100f));
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(runObject);
        }
    }

    [Test]
    public void AgentOnlyReplacesFullInventoryForAnUpgrade()
    {
        var root = new GameObject("Test agent");
        var upgrade = ScriptableObject.CreateInstance<WeaponDefinition>();
        try
        {
            var agent = root.AddComponent<PlayerController>();
            agent.Weapons[0] = new WeaponInstance(definition);
            Assert.That(agent.WantsWeapon(new WeaponInstance(definition)), Is.False);
            upgrade.damage = definition.damage * 1.5f;
            Assert.That(agent.WantsWeapon(new WeaponInstance(upgrade)), Is.True);
            agent.Weapons[0] = null;
            Assert.That(agent.WantsWeapon(new WeaponInstance(definition)), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(upgrade);
        }
    }
}
