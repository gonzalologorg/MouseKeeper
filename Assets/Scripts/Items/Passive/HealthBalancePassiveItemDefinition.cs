using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Passive Items/Health Balance")]
    public class HealthBalancePassiveItemDefinition : PassiveItemDefinition
    {
        protected override string DefaultIconResource => "ItemIcons/HealthBalance";
        public override bool ConsumeOnPickup => true;
        public HealthBalancePassiveItemDefinition() { movementSpeedBonus = 0f; }
        public override void OnPickUp(PlayerController player) => player.BalanceSquadHealth();
    }
}
