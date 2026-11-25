using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Weapons/Rusty Blade")]
    public sealed class RustyBladeWeaponDefinition : MeleeWeaponDefinition
    {
        public override bool ApplyDamageScaling => false;
        public override void OnDamage(PlayerController player, WeaponInstance weapon, MouseEnemy mouse, float amount)
        {
            if (!mouse.IsDefeated) mouse.ApplyBleed(player, weapon);
        }
    }
}
