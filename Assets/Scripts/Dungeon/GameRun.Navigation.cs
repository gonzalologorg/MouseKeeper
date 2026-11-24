using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace MouseKeeper
{
    public sealed partial class GameRun
    {
        NavMeshData navigationData;
        NavMeshDataInstance navigationInstance;

        void BuildFloorNavigation()
        {
            RemoveFloorNavigation();
            var sources = new List<NavMeshBuildSource>();
            var roomSources = new List<NavMeshBuildSource>();
            var markups = new List<NavMeshBuildMarkup>();
            Physics.SyncTransforms();
            Bounds bounds = new Bounds(rooms[0].center, Vector3.one);
            foreach (Room room in rooms)
            {
                bounds.Encapsulate(room.center + new Vector3(room.width * 0.5f + 2f, 5f, room.depthSize * 0.5f + 2f));
                bounds.Encapsulate(room.center - new Vector3(room.width * 0.5f + 2f, 5f, room.depthSize * 0.5f + 2f));
                roomSources.Clear();
                NavMeshBuilder.CollectSources(room.root.transform, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, markups, roomSources);
                foreach (NavMeshBuildSource source in roomSources)
                {
                    Collider collider = source.component as Collider;
                    if (collider == null || !collider.enabled || collider.isTrigger || !collider.gameObject.activeInHierarchy ||
                        collider.GetComponentInParent<RoomDoor>() != null ||
                        collider.GetComponentInParent<PlayerController>() != null ||
                        collider.GetComponentInParent<MouseEnemy>() != null ||
                        collider.GetComponentInParent<ItemPickup>() != null) continue;
                    sources.Add(source);
                }
            }
            NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
            settings.agentRadius = 0.4f;
            settings.agentClimb = 0.25f;
            navigationData = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            if (navigationData == null)
            {
                Debug.LogError("No se pudo generar la navegación del piso.", this);
                return;
            }
            navigationInstance = NavMesh.AddNavMeshData(navigationData);
        }

        void RemoveFloorNavigation()
        {
            foreach (PlayerController agent in agents)
                if (agent != null && agent.TryGetComponent(out NavigationMotor motor)) motor.ResetNavigation();
            if (navigationInstance.valid) navigationInstance.Remove();
            if (navigationData != null) Destroy(navigationData);
            navigationData = null;
        }
    }
}
