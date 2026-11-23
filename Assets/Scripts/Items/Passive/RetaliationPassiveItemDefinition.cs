using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Passive Items/Retaliation")]
    public class RetaliationPassiveItemDefinition : PassiveItemDefinition
    {
        [Min(0f)] public float damageFraction = 0.25f;

        public RetaliationPassiveItemDefinition()
        {
            movementSpeedBonus = 0f;
        }

        public override void OnTakeDamage(PlayerController player, float amount)
        {
            player.DamageMiceInRoom(amount * Mathf.Max(0f, damageFraction));
        }
    }
}
