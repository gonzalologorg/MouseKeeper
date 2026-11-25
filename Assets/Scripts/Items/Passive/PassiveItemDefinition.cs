using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Passive Item")]
    public class PassiveItemDefinition : ScriptableObject
    {
        public string displayName = "Running Shoes";
        public Sprite icon;
        public float movementSpeedBonus = 1f;
        public float maxHealthBonus;
        public float energyRechargeBonus;
        public virtual bool ConsumeOnPickup => false;
        protected virtual string DefaultIconResource => null;
        Sprite defaultIcon;
        bool defaultIconLoaded;
        public Sprite DisplayIcon
        {
            get
            {
                if (icon != null) return icon;
                if (!defaultIconLoaded && !string.IsNullOrEmpty(DefaultIconResource))
                {
                    defaultIconLoaded = true;
                    defaultIcon = Resources.Load<Sprite>(DefaultIconResource);
                    if (defaultIcon == null) Debug.LogError("No se encontró el icono de pasivo " + DefaultIconResource, this);
                }
                return defaultIcon;
            }
        }

        public virtual void OnPickUp(PlayerController player) { }
        public virtual void OnRoomEnter(PlayerController player) { }
        public virtual void OnRoomExit(PlayerController player) { }
        public virtual void OnDeath(PlayerController player) { }
        public virtual void OnMouseKill(PlayerController player, MouseEnemy mouse) { }
        public virtual void OnCriticalHit(PlayerController player, MouseEnemy mouse) { }
        public virtual void OnCombatTick(PlayerController player, float deltaTime) { }
        public virtual void OnTakeDamage(PlayerController player, float amount) { }
        public virtual void OnAttacked(PlayerController player, MouseEnemy attacker, float amount) { }
    }
}
