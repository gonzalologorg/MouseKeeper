using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Weapons/Special/Blower")]
    public sealed class BlowerWeaponDefinition : SpecialWeaponDefinition
    {
        public BlowerWeaponDefinition() { tags = WeaponTags.Utility | WeaponTags.Support; }
        public override bool ApplyDamageScaling => false;
        public override float EstimatedDps => Mathf.Max(0f, damage);
        protected override void PerformPrimaryAttack(PlayerController player, WeaponInstance weapon)
        {
            player.SwingWeapon(weapon);
            foreach (MouseEnemy mouse in player.CurrentRoom.GetComponentsInChildren<MouseEnemy>())
            {
                if (TryGetTargetDirection(player, mouse, out Vector3 direction))
                {
                    mouse.TakeDamage(damage * weapon.PrimaryInterval, player, weapon, roundDamage: false);
                    mouse.Push(direction, 2.5f);
                }
            }
        }
    }
}
