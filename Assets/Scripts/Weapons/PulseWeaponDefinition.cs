using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Weapons/Pulse")]
    public class PulseWeaponDefinition : RaytraceWeaponDefinition
    {
        [Min(0f)] public float pulseDamage = 10f;

        public override void SecondaryAttack(PlayerController player, WeaponInstance weapon)
        {
            if (weapon.TrySpend(secondaryCost, attackInterval * 2f))
                player.DamageMiceInRoom(pulseDamage, weapon);
        }
    }
}
