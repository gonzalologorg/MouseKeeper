using UnityEngine;

namespace MouseKeeper
{
    public sealed class WeaponInstance
    {
        public WeaponDefinition Definition { get; }
        public float Energy { get; private set; }
        public bool IsReloading => reloadRemaining > 0f;
        float cooldown;
        float reloadRemaining;

        public WeaponInstance(WeaponDefinition definition)
        {
            Definition = definition;
            Energy = Mathf.Max(0f, definition.maxEnergy);
        }

        public bool TrySpend(float cost, float interval)
        {
            cost = Mathf.Max(0f, cost);
            if (cooldown > 0f || IsReloading || Energy < cost) return false;
            Energy -= cost;
            cooldown = Mathf.Max(0f, interval);
            return true;
        }

        public void StartReload(float duration)
        {
            if (IsReloading || Energy >= Definition.maxEnergy) return;
            reloadRemaining = Mathf.Max(0.01f, duration);
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
