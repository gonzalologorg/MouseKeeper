using UnityEngine;

namespace MouseKeeper
{
    public abstract class SpecialWeaponDefinition : WeaponDefinition
    {
        public sealed override void PrimaryAttack(PlayerController player, WeaponInstance weapon)
        {
            if (weapon.TrySpend(primaryCost, GetPrimaryInterval(player)))
                PerformPrimaryAttack(player, weapon);
        }

        public override void SecondaryAttack(PlayerController player, WeaponInstance weapon) { }

        protected virtual float GetPrimaryInterval(PlayerController player) => attackInterval;

        protected abstract void PerformPrimaryAttack(PlayerController player, WeaponInstance weapon);

        protected Vector3 GetDeploymentPosition(PlayerController player)
        {
            return player.ClampToCurrentRoom(player.transform.position + player.AimDirection *
                Mathf.Clamp(player.TargetDistance, 1f, Mathf.Min(4f, range)));
        }

        protected static GameObject CreateRoomEffect(PlayerController player, PrimitiveType primitive,
            string name, Vector3 position, float scale, Color color)
        {
            GameObject effect = GameObject.CreatePrimitive(primitive);
            effect.name = name;
            effect.transform.SetParent(player.CurrentRoom, true);
            effect.transform.position = position;
            effect.transform.localScale = Vector3.one * scale;

            Renderer renderer = effect.GetComponent<Renderer>();
            renderer.sharedMaterial = PrimitiveMaterial.Shared;
            var tint = new MaterialPropertyBlock();
            tint.SetColor("_BaseColor", color);
            renderer.SetPropertyBlock(tint);
            effect.GetComponent<Collider>().enabled = false;
            return effect;
        }

        protected bool TryGetTargetDirection(PlayerController player, MouseEnemy mouse, out Vector3 direction)
        {
            direction = mouse.transform.position - player.transform.position;
            direction.y = 0f;
            return mouse.IsTargetable && direction.magnitude <= range &&
                Vector3.Angle(player.AimDirection, direction) <= 45f;
        }
    }
}
