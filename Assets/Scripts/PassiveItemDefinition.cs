using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Passive Item")]
    public class PassiveItemDefinition : ScriptableObject
    {
        public string displayName = "Running Shoes";
        public float movementSpeedBonus = 1f;
        public float maxHealthBonus;
        public float energyRechargeBonus;

        public virtual void OnPickUp(PlayerController player) { }
        public virtual void OnRoomEnter(PlayerController player) { }
        public virtual void OnRoomExit(PlayerController player) { }
        public virtual void OnDeath(PlayerController player) { }
        public virtual void OnMouseKill(PlayerController player, MouseEnemy mouse) { }
        public virtual void OnTakeDamage(PlayerController player, float amount) { }
    }
}
