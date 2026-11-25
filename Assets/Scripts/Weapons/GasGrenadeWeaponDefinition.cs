using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Weapons/Special/GasGrenade")]
    public sealed class GasGrenadeWeaponDefinition : SpecialWeaponDefinition
    {
        public override float EstimatedDps => 60f / Mathf.Max(0.05f, attackInterval);
        protected override void PerformPrimaryAttack(PlayerController player, WeaponInstance weapon)
        {
            Vector3 landing = GetDeploymentPosition(player);
            GameObject effect = CreateRoomEffect(player, PrimitiveType.Sphere, "Gas cloud",
                player.transform.position + Vector3.up, 0.3f, new Color(0.35f, 0.8f, 0.25f));
            effect.AddComponent<GasCloud>().Initialize(player, weapon, landing);
        }
    }
}
