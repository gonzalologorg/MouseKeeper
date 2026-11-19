using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Passive Items/Movement and Health")]
    public class StatPassiveItemDefinition : PassiveItemDefinition
    {
        public StatPassiveItemDefinition()
        {
            maxHealthBonus = 25f;
        }
    }
}
