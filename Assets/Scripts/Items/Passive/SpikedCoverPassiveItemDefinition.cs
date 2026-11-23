using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Passive Items/Spiked Cover")]
    public class SpikedCoverPassiveItemDefinition : PassiveItemDefinition
    {
        protected override string DefaultIconResource => "ItemIcons/SpikedCover";
        public SpikedCoverPassiveItemDefinition() { movementSpeedBonus = 0f; }
        public override void OnAttacked(PlayerController player, MouseEnemy attacker, float amount)
        {
            if (attacker != null) attacker.TakeDamage(amount * 0.25f, player);
        }
    }
}
