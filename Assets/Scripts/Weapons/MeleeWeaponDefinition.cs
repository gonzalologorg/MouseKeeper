using System.Collections.Generic;
using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Weapons/Melee")]
    public class MeleeWeaponDefinition : WeaponDefinition
    {
        [Range(0f, 360f)] public float angle = 90f;
        [Min(0f)] public float pushDistance = 1.5f;

        public override bool UsesMeleeDistance => true;

        public MeleeWeaponDefinition()
        {
            range = 2.5f;
        }

        public override void PrimaryAttack(PlayerController player, WeaponInstance weapon)
        {
            if (!weapon.TrySpend(primaryCost, attackInterval)) return;
            player.SwingWeapon(weapon);
            var hitMice = new HashSet<MouseEnemy>();
            Vector3 center = player.transform.position + Vector3.up * 0.5f;
            foreach (Collider hit in Physics.OverlapSphere(center, Mathf.Max(0f, range), ~0, QueryTriggerInteraction.Ignore))
            {
                MouseEnemy mouse = hit.GetComponentInParent<MouseEnemy>();
                if (mouse == null || !hitMice.Add(mouse)) continue;
                Vector3 direction = mouse.transform.position - player.transform.position;
                direction.y = 0f;
                Vector3 horizontalAim = Vector3.ProjectOnPlane(player.AimDirection, Vector3.up);
                if (direction.sqrMagnitude > range * range ||
                    Vector3.Angle(horizontalAim, direction) > Mathf.Clamp(angle, 0f, 360f) * 0.5f) continue;
                mouse.TakeDamage(damage, player, weapon);
                mouse.Push(direction.sqrMagnitude > 0f ? direction : player.AimDirection, pushDistance);
            }
        }

        public override void SecondaryAttack(PlayerController player, WeaponInstance weapon) { }
    }
}
