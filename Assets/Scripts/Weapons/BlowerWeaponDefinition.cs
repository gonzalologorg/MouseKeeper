using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Weapons/Special/Blower")]
    public sealed class BlowerWeaponDefinition : SpecialWeaponDefinition
    {
        protected override void PerformPrimaryAttack(PlayerController player, WeaponInstance weapon)
        {
            player.SwingWeapon(weapon);
            foreach (MouseEnemy mouse in player.CurrentRoom.GetComponentsInChildren<MouseEnemy>())
            {
                if (TryGetTargetDirection(player, mouse, out Vector3 direction))
                    mouse.Push(direction, 2.5f);
            }
        }
    }
}
