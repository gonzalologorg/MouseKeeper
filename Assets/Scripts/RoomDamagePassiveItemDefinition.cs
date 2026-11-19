using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Passive Items/Room Damage")]
    public class RoomDamagePassiveItemDefinition : PassiveItemDefinition
    {
        [Min(0f)] public float roomDamage = 20f;

        public RoomDamagePassiveItemDefinition()
        {
            movementSpeedBonus = 0f;
        }

        public override void OnRoomEnter(PlayerController player)
        {
            player.DamageMiceInRoom(roomDamage);
        }
    }
}
