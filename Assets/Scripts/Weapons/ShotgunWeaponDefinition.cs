using System.Collections.Generic;
using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Weapons/Special/Shotgun")]
    public sealed class ShotgunWeaponDefinition : SpecialWeaponDefinition
    {
        public override bool ApplyDamageScaling => false;
        public override float EstimatedDps => Mathf.Max(0f, damage) / Mathf.Max(0.05f, attackInterval);
        protected override void PerformPrimaryAttack(PlayerController player, WeaponInstance weapon)
        {
            var hits = new HashSet<MouseEnemy>();
            Vector3 origin = player.transform.position + Vector3.up * 0.65f + player.AimDirection * 0.6f;
            for (int i = -1; i <= 1; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, i * 12f, 0f) * player.AimDirection;
                if (!Physics.Raycast(origin, direction, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Ignore)) continue;
                MouseEnemy mouse = hit.collider.GetComponentInParent<MouseEnemy>();
                if (mouse == null || !hits.Add(mouse)) continue;
                float scaledDamage = damage * Mathf.Lerp(1f, 0.4f, Mathf.InverseLerp(range * 0.2f, range, hit.distance));
                mouse.TakeDamage(scaledDamage, player, weapon, false, hit.point, origin);
            }
        }
    }
}
