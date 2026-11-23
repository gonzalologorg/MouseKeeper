using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Passive Items/Revive")]
    public class RevivePassiveItemDefinition : PassiveItemDefinition
    {
        [Min(0f)] public float restoredHealth = 50f;

        public RevivePassiveItemDefinition()
        {
            movementSpeedBonus = 0f;
        }

        public override void OnDeath(PlayerController player)
        {
            player.ConsumePassiveAndHeal(this, restoredHealth);
        }
    }
}
