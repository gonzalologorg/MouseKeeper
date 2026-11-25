using System.Collections.Generic;
using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Passive Items/Reactive")]
    public sealed class ReactivePassiveItemDefinition : PassiveItemDefinition
    {
        public enum Effect { OverchargedBattery, SonicRepellent, SpontaneousIgnition, SpontaneousFreeze }
        public Effect effect;
        [Min(0f)] public float radius = 4f;
        readonly Dictionary<PlayerController, int> kills = new Dictionary<PlayerController, int>();
        readonly Dictionary<PlayerController, float> elapsed = new Dictionary<PlayerController, float>();
        public ReactivePassiveItemDefinition() { movementSpeedBonus = 0f; }
        protected override string DefaultIconResource => "ItemIcons/" + effect;

        public override void OnMouseKill(PlayerController player, MouseEnemy mouse)
        {
            if (effect != Effect.OverchargedBattery || player.CurrentRoom == null) return;
            kills.TryGetValue(player, out int count);
            count++;
            kills[player] = count % 5;
            if (count < 5) return;
            SpawnEffect(player.CurrentRoom, mouse.transform.position, new Color(0.4f, 0.85f, 1f), 1f);
            var targets = new List<MouseEnemy>();
            foreach (MouseEnemy other in player.CurrentRoom.GetComponentsInChildren<MouseEnemy>())
                if (other.IsTargetable && (other.transform.position - mouse.transform.position).sqrMagnitude <= radius * radius)
                    targets.Add(other);
            foreach (MouseEnemy other in targets) other.TakeDamage(10f / targets.Count, player, roundDamage: false, damageSource: "overcharged_battery");
        }

        public override void OnCombatTick(PlayerController player, float deltaTime)
        {
            if (effect != Effect.SonicRepellent || player.CurrentRoom == null) return;
            elapsed.TryGetValue(player, out float time);
            time += deltaTime;
            if (time < 5f) { elapsed[player] = time; return; }
            elapsed[player] = time - 5f;
            SpawnEffect(player.CurrentRoom, player.transform.position, new Color(0.55f, 0.85f, 1f), radius);
            foreach (MouseEnemy mouse in player.CurrentRoom.GetComponentsInChildren<MouseEnemy>())
            {
                Vector3 away = mouse.transform.position - player.transform.position;
                away.y = 0f;
                if (mouse.IsTargetable && away.sqrMagnitude < radius * radius && away.sqrMagnitude > 0.001f)
                    mouse.Push(away, Mathf.Max(0f, radius - away.magnitude));
            }
        }

        public override void OnRoomExit(PlayerController player) => elapsed.Remove(player);
        public override void OnCriticalHit(PlayerController player, MouseEnemy mouse)
        {
            if (effect == Effect.SpontaneousIgnition)
            {
                SpawnEffect(mouse.transform, mouse.transform.position, new Color(1f, 0.35f, 0.05f), 0.7f, 5f);
                mouse.ApplyDamageOverTime(player, 30f, 5, "spontaneous_ignition");
            }
            else if (effect == Effect.SpontaneousFreeze) mouse.ApplyFreeze(3f);
        }

        static void SpawnEffect(Transform parent, Vector3 position, Color color, float size, float duration = 0.5f)
        {
            var visual = new GameObject("Passive effect");
            visual.transform.SetParent(parent, true);
            visual.transform.position = position + Vector3.up * 0.5f;
            ParticleSystem particles = visual.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = true;
            main.startLifetime = 0.3f;
            main.startSpeed = size * 1.5f;
            main.startSize = 0.15f;
            main.startColor = color;
            var emission = particles.emission;
            emission.rateOverTime = 25f;
            Object.Destroy(visual, duration);
        }
    }
}
