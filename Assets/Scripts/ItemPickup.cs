using UnityEngine;

namespace MouseKeeper
{
    public enum PickupKind { None, Money, Health, Passive, Weapon }

    public sealed class ItemPickup : MonoBehaviour
    {
        public WeaponInstance weapon;
        public PassiveItemDefinition passive;
        bool consumed;
        bool collectOnContact;
        int price;
        GameObject priceLabel;
        MoneyVial money;
        HealthVial health;

        public PickupKind Kind => weapon != null ? PickupKind.Weapon : passive != null ? PickupKind.Passive :
            health != null ? PickupKind.Health : money != null ? PickupKind.Money : PickupKind.None;
        public bool Available => !consumed && Kind != PickupKind.None;

        void Awake()
        {
            money = GetComponent<MoneyVial>();
            health = GetComponent<HealthVial>();
        }

        public bool CanPickUp(PlayerController player)
        {
            return Available && !player.Dead && (Kind != PickupKind.Health || player.Health < player.MaxHealth);
        }

        public void SetAutomaticCollection()
        {
            collectOnContact = true;
        }

        public void SetPrice(int amount, GameObject label)
        {
            if (amount <= 0)
            {
                Debug.LogError("Pickup price must be positive.", this);
                return;
            }
            price = amount;
            priceLabel = label;
        }

        void OnTriggerStay(Collider other)
        {
            if (!collectOnContact || !Available) return;
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null) PickUp(player);
        }

        public void PickUp(PlayerController player)
        {
            if (!CanPickUp(player)) return;
            if (health != null && health.amount <= 0f)
            {
                Debug.LogError("Health vial amount must be positive.", this);
                return;
            }
            if (money != null && money.amount <= 0)
            {
                Debug.LogError("Money vial amount must be positive.", this);
                return;
            }
            if (price > 0 && !player.TrySpendMoney(price))
            {
                player.ShowPurchaseFailure(price);
                return;
            }
            if (weapon != null) player.Equip(weapon);
            else if (passive != null) player.AddPassive(passive);
            else if (health != null) player.Heal(health.amount);
            else if (money != null) player.AddMoney(money.amount);
            consumed = true;
            player.NotifyItemPickupChanged();
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (priceLabel != null) Destroy(priceLabel);
        }
    }
}
