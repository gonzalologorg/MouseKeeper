using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Weapons/Foam Launcher")]
    public sealed class FoamWeaponDefinition : ParticleWeaponDefinition
    {
        [Min(0f)] public float slowDuration = 2f;
        public override bool ApplyDamageScaling => false;
        public override void OnDamage(PlayerController player, WeaponInstance weapon, MouseEnemy mouse, float amount)
        {
            if (!mouse.IsDefeated) mouse.ApplySlow(slowDuration);
        }
    }
}
