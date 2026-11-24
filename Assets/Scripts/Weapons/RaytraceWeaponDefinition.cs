using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Weapons/Raytrace")]
    public class RaytraceWeaponDefinition : WeaponDefinition
    {
        [Min(1)] public int bulletAmount = 1;
        public bool automatic = true;
        [Min(0f)] public float spreadAngle = 12f;
        [Tooltip("Optional particle effect emitted for each shot.")]
        public GameObject projectileEffect;

        public override bool HoldPrimaryAttack => automatic;

        public override void PrimaryAttack(PlayerController player, WeaponInstance weapon)
        {
            if (!weapon.TrySpend(primaryCost, attackInterval)) return;
            int count = Mathf.Max(1, bulletAmount);
            for (int i = 0; i < count; i++)
            {
                float offset = count == 1 ? 0f : Mathf.Lerp(-spreadAngle * 0.5f, spreadAngle * 0.5f, (float)i / (count - 1));
                player.Fire(damage, range, Quaternion.Euler(0f, offset, 0f) * player.AimDirection, weapon);
            }
        }
    }
}
