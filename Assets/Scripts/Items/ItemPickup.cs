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
        bool lowestHealthOnly;
        PlayerController fallenOwner;
        public bool IsFallenWeapon { get; private set; }
        public bool IsEmergencyWeapon { get; private set; }
        internal void MarkEmergencyWeapon() { IsEmergencyWeapon = true; }
        internal bool IsFromFallenAgent(PlayerController agent) => fallenOwner == agent;
        internal void MarkFallenWeapon(PlayerController owner) { IsFallenWeapon = true; fallenOwner = owner; }
        int price;
        GameObject priceLabel;
        MoneyVial money;
        HealthVial health;

        public PickupKind Kind => weapon != null ? PickupKind.Weapon : passive != null ? PickupKind.Passive :
            health != null ? PickupKind.Health : money != null ? PickupKind.Money : PickupKind.None;
        public bool Available => !consumed && Kind != PickupKind.None;
        public int Price => price;
        public string DisplayName => GameText.Get(weapon != null ? weapon.Definition.displayName :
            passive != null ? passive.displayName : null);
        public Sprite DisplayIcon => weapon != null ? weapon.Definition.icon :
            passive != null ? passive.DisplayIcon : null;

        void Awake()
        {
            money = GetComponent<MoneyVial>();
            health = GetComponent<HealthVial>();
        }

        void Start()
        {
            if (Available) gameObject.AddComponent<PickupGlow>().Initialize(true);
        }

        public bool CanPickUp(PlayerController player)
        {
            return Available && !player.Dead &&
                (Kind != PickupKind.Health || player.Health < player.MaxHealth) &&
                (Kind != PickupKind.Health || player.CanTakeHealth()) &&
                (!lowestHealthOnly || player.IsLowestHealthAgent()) &&
                (Kind != PickupKind.Money || player.Karma >= -7 || player.IsLastSurvivor);
        }

        public void SetAutomaticCollection()
        {
            collectOnContact = true;
        }

        public void SetLowestHealthPriority()
        {
            lowestHealthOnly = true;
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
            if (player != null && player.CanCollect(this)) PickUp(player);
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
            PickupKind kind = Kind;
            bool purchased = price > 0;
            bool usedGreed = !purchased && player.IsGreedClaim(this);
            if (purchased && !player.TryPayForPickup(this))
            {
                player.ShowPurchaseFailure(price);
                return;
            }
            if (!purchased && kind != PickupKind.Weapon) player.NotifyContestedPickup(this);
            if (weapon != null) player.Equip(weapon);
            else if (passive != null && !passive.ConsumeOnPickup) player.AddPassive(passive);
            else if (health != null) player.Heal(health.amount, source: "health_pickup");
            else if (money != null) player.CollectMoney(money.amount);
            consumed = true;
            player.RecordBalancePickup(kind, DisplayName, price);
            if (!IsEmergencyWeapon) player.RecordPickupScore(kind, price);
            player.NotifyLootCollected(kind, usedGreed, purchased);
            if (passive != null && passive.ConsumeOnPickup) passive.OnPickUp(player);
            if ((kind == PickupKind.Weapon || kind == PickupKind.Passive) && player.Dialogue != null)
                player.Dialogue.AnnouncePickup(DisplayName);
            if (price > 0 && player.Dialogue != null) player.Dialogue.Purchase();
            player.NotifyWeaponPickupCompleted(this);
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (priceLabel != null) Destroy(priceLabel);
        }
    }
}
