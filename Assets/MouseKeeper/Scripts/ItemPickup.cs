using UnityEngine;

namespace MouseKeeper
{
    public sealed class ItemPickup : MonoBehaviour
    {
        public WeaponInstance weapon;
        public PassiveItemDefinition passive;

        public void PickUp(PlayerController player)
        {
            if (weapon != null) player.Equip(weapon);
            else if (passive != null) player.AddPassive(passive);
            else return;
            Destroy(gameObject);
        }
    }
}
