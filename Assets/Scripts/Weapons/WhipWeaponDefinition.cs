using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Weapons/Special/Whip")]
    public sealed class WhipWeaponDefinition : SpecialWeaponDefinition
    {
        public WhipWeaponDefinition() { tags = WeaponTags.Utility | WeaponTags.Support | WeaponTags.Melee; }
        [Min(0f)] public float pushDistance = 4f;
        public override bool UsesMeleeDistance => true;

        protected override float GetPrimaryInterval(PlayerController player)
        {
            return attackInterval * Mathf.Max(0.1f, player.Profile.attackSpeed);
        }

        protected override void PerformPrimaryAttack(PlayerController player, WeaponInstance weapon)
        {
            player.SwingWeapon(weapon);
            MouseEnemy closest = null;
            float nearest = float.PositiveInfinity;
            foreach (MouseEnemy mouse in player.CurrentRoom.GetComponentsInChildren<MouseEnemy>())
            {
                if (!TryGetTargetDirection(player, mouse, out Vector3 direction)) continue;
                if (direction.sqrMagnitude >= nearest) continue;
                nearest = direction.sqrMagnitude;
                closest = mouse;
            }

            if (closest == null) return;
            closest.TakeDamage(damage, player, weapon);
            closest.Push(closest.transform.position - player.transform.position, pushDistance);
            closest.RetreatFrom(player.transform.position, 2f);
        }
    }
}
