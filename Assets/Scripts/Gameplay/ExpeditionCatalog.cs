using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Catálogo de expedición")]
    public sealed class ExpeditionCatalog : ScriptableObject
    {
        public AgentDefinition[] agents;
        public WeaponDefinition[] weapons;
        public WeaponDefinition basicWeapon;
    }
}
