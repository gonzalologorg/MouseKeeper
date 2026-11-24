using UnityEngine;

namespace MouseKeeper
{
    public sealed class RoomLayout : MonoBehaviour
    {
        public enum RewardKind { Health, Money, Weapon, Passive }
        public enum DoorSide { North, Right, South, Left }

        [System.Serializable]
        public sealed class Reward
        {
            public RewardKind kind;
            public Transform position;
            [Range(0f, 1f)] public float chance = 1f;
            public WeaponDefinition weapon;
            public PassiveItemDefinition passive;
        }

        [Tooltip("Tamaño mínimo de la sala para colocar esta plantilla sin escalarla.")]
        public Vector2 minimumRoomSize = new Vector2(10f, 10f);
        [Tooltip("Una plantilla con estructura requiere una sala del mismo tamaño y una conexión en el lado indicado.")]
        public bool hasRoomShell;
        public DoorSide doorSide = DoorSide.Left;
        [Tooltip("Estructura de referencia visible al editar; el generador la reemplaza por la estructura funcional.")]
        public GameObject editorShell;
        [Tooltip("-1 usa la cantidad normal del generador.")]
        [Min(-1)] public int enemyCount = -1;
        public Transform[] hideSpots;
        public bool replaceDefaultRewards;
        public Reward[] rewards;

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(minimumRoomSize.x, 0.05f, minimumRoomSize.y));
        }
    }
}
