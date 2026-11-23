using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Weapons/Special/Cheese")]
    public sealed class CheeseDecoyWeaponDefinition : SpecialWeaponDefinition
    {
        protected override void PerformPrimaryAttack(PlayerController player, WeaponInstance weapon)
        {
            GameObject effect = CreateRoomEffect(player, PrimitiveType.Cube, "Cheese decoy",
                GetDeploymentPosition(player), 0.65f, new Color(1f, 0.75f, 0.1f));
            effect.AddComponent<CheeseDecoy>();
        }
    }
}
