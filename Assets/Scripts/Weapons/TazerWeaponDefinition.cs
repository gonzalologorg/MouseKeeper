using System.Collections.Generic;
using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Weapons/Tazer")]
    public sealed class TazerWeaponDefinition : WeaponDefinition
    {
        public override bool ApplyDamageScaling => false;

        public override void PrimaryAttack(PlayerController player, WeaponInstance weapon)
        {
            if (player.CurrentRoom == null) return;
            Vector3 origin = player.transform.position + Vector3.up * 0.65f;
            var targets = new List<MouseEnemy>();
            foreach (MouseEnemy mouse in player.CurrentRoom.GetComponentsInChildren<MouseEnemy>())
            {
                Vector3 offset = mouse.transform.position - player.transform.position;
                offset.y = 0f;
                if (mouse.IsTargetable && offset.sqrMagnitude <= range * range)
                    targets.Add(mouse);
            }
            if (targets.Count == 0) return;
            if (!weapon.TrySpend(primaryCost, attackInterval)) return;
            float dividedDamage = DamagePerTarget(damage, targets.Count);
            foreach (MouseEnemy mouse in targets)
            {
                DrawCable(player.CurrentRoom, origin, mouse.transform.position + Vector3.up * 0.6f);
                mouse.TakeDamage(dividedDamage, player, weapon, roundDamage: false);
            }
        }

        public static float DamagePerTarget(float totalDamage, int count) => count > 0 ? Mathf.Max(0f, totalDamage) / count : 0f;

        static void DrawCable(Transform parent, Vector3 from, Vector3 to)
        {
            var cable = new GameObject("Electric cable");
            cable.transform.SetParent(parent, true);
            LineRenderer line = cable.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.startWidth = line.endWidth = 0.08f;
            line.sharedMaterial = PrimitiveMaterial.Shared;
            line.startColor = line.endColor = new Color(0.4f, 0.85f, 1f);
            Object.Destroy(cable, 0.2f);
        }
        public override void SecondaryAttack(PlayerController player, WeaponInstance weapon) { }
    }
}
