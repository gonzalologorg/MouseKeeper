using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Weapon")]
    public class WeaponDefinition : ScriptableObject
    {
        public string displayName = "Mouse Blaster";
        public float maxEnergy = 12f;
        public float damage = 20f;
        public float attackInterval = 0.25f;
        public float range = 15f;
        public float primaryCost = 1f;
        public float secondaryCost = 4f;
        public float reloadDuration = 1f;

        public virtual bool HoldPrimaryAttack => true;

        public virtual void PrimaryAttack(PlayerController player, WeaponInstance weapon)
        {
            if (weapon.TrySpend(primaryCost, attackInterval))
                player.Fire(damage, range, weapon);
        }

        public virtual void SecondaryAttack(PlayerController player, WeaponInstance weapon)
        {
            if (weapon.TrySpend(secondaryCost, attackInterval * 2f))
                player.Fire(damage * 3f, range, weapon);
        }

        public virtual void Reload(PlayerController player, WeaponInstance weapon)
        {
            weapon.StartReload(reloadDuration);
        }

        public virtual void Think(PlayerController player, WeaponInstance weapon, float deltaTime) { }
        public virtual void OnDamage(PlayerController player, WeaponInstance weapon, MouseEnemy mouse, float amount) { }
        public virtual void OnDrop(PlayerController player, WeaponInstance weapon) { }
        public virtual void OnPickup(PlayerController player, WeaponInstance weapon) { }
    }
}
