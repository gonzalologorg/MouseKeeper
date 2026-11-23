using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Weapons/Particle")]
    public class ParticleWeaponDefinition : WeaponDefinition
    {
        [Min(0f)] public float particleSpeed = 8f;
        [Min(0f)] public float particleDuration = 1f;

        public override void PrimaryAttack(PlayerController player, WeaponInstance weapon)
        {
            if (!weapon.TrySpend(primaryCost, attackInterval)) return;
            GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            particle.name = "Weapon Particle";
            particle.GetComponent<Renderer>().sharedMaterial = PrimitiveMaterial.Shared;
            particle.transform.SetParent(player.CurrentRoom, true);
            particle.transform.position = player.transform.position + Vector3.up * 0.65f + player.AimDirection * 0.6f;
            particle.transform.localScale = Vector3.one * 0.2f;
            particle.GetComponent<Collider>().enabled = false;
            particle.AddComponent<WeaponParticle>().Initialize(player, weapon, damage, player.AimDirection,
                Mathf.Max(0f, particleSpeed), Mathf.Max(0f, particleDuration));
        }

        public override void SecondaryAttack(PlayerController player, WeaponInstance weapon) { }
    }
}
