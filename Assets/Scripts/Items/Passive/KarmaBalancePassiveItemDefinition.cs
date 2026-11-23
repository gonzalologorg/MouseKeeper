using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Passive Items/Karma Balance")]
    public class KarmaBalancePassiveItemDefinition : PassiveItemDefinition
    {
        protected override string DefaultIconResource => "ItemIcons/KarmaBalance";
        public override bool ConsumeOnPickup => true;
        public KarmaBalancePassiveItemDefinition() { movementSpeedBonus = 0f; }
        public override void OnPickUp(PlayerController player) => player.BalanceSquadKarma();
    }
}
