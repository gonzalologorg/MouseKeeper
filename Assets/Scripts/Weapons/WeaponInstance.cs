using UnityEngine;

namespace MouseKeeper
{
    public sealed class WeaponInstance
    {
        public WeaponDefinition Definition { get; }
        public float Energy { get; private set; }
        public bool IsReloading => reloadRemaining > 0f;
        public float PrimaryInterval => Mathf.Max(0f, Definition.attackInterval) / attackSpeed;
        float cooldown;
        float reloadRemaining;
        float attackSpeed = 1f;
        float reloadSpeed = 1f;

        public WeaponInstance(WeaponDefinition definition, float attackSpeed = 1f, int agility = 5)
        {
            Definition = definition;
            Energy = Mathf.Max(0f, definition.maxEnergy);
            Configure(attackSpeed, agility);
        }

        public void Configure(float speed, int agility, float reloadMultiplier = 1f)
        {
            attackSpeed = Mathf.Max(0.1f, speed);
            reloadSpeed = Mathf.Lerp(0.75f, 1.5f, (Mathf.Clamp(agility, 1, 10) - 1) / 9f) * reloadMultiplier;
        }

        public bool TrySpend(float cost, float interval)
        {
            cost = Mathf.Max(0f, cost);
            if (cooldown > 0f || IsReloading || Energy < cost) return false;
            Energy -= cost;
            cooldown = Mathf.Max(0f, interval) / attackSpeed;
            return true;
        }

        public void StartReload(float duration)
        {
            if (IsReloading || Energy >= Definition.maxEnergy) return;
            reloadRemaining = Mathf.Max(0.01f, duration / reloadSpeed);
        }

        public void Tick(float deltaTime, float recharge)
        {
            cooldown = Mathf.Max(0f, cooldown - deltaTime);
            if (IsReloading)
            {
                reloadRemaining -= deltaTime;
                if (reloadRemaining <= 0f) Energy = Mathf.Max(0f, Definition.maxEnergy);
            }
            else
                Energy = Mathf.Min(Mathf.Max(0f, Definition.maxEnergy), Energy + Mathf.Max(0f, recharge) * deltaTime);
        }
    }
}
