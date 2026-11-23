using System.Collections;
using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Passive Items/Inhaler")]
    public class InhalerPassiveItemDefinition : PassiveItemDefinition
    {
        protected override string DefaultIconResource => "ItemIcons/Inhaler";
        public InhalerPassiveItemDefinition() { movementSpeedBonus = 0f; }
        public override void OnPickUp(PlayerController player)
        {
            InhalerEffect effect = player.gameObject.AddComponent<InhalerEffect>();
            effect.Initialize(player);
            if (player.Health > 0f && player.Health <= player.MaxHealth * 0.25f) effect.Activate();
        }
        public override void OnRoomEnter(PlayerController player)
        {
            foreach (InhalerEffect effect in player.GetComponents<InhalerEffect>()) effect.ResetRoom();
        }
        public override void OnTakeDamage(PlayerController player, float amount)
        {
            if (player.Health > 0f && player.Health <= player.MaxHealth * 0.25f)
                foreach (InhalerEffect effect in player.GetComponents<InhalerEffect>()) effect.Activate();
        }
    }

    public sealed class InhalerEffect : MonoBehaviour
    {
        PlayerController player;
        bool used;
        bool active;
        public void Initialize(PlayerController owner) => player = owner;
        public void ResetRoom() => used = false;
        public void Activate()
        {
            if (used || active) return;
            used = active = true;
            StartCoroutine(Recover());
        }
        IEnumerator Recover()
        {
            player.SetEmergencySpeedMultiplier(3f);
            float heal = player.MaxHealth * 0.1f;
            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForSeconds(1f);
                if (player.Dead) break;
                player.Heal(heal);
            }
            player.SetEmergencySpeedMultiplier(1f);
            active = false;
        }
        void OnDisable()
        {
            if (player != null) player.SetEmergencySpeedMultiplier(1f);
            active = false;
        }
    }
}
