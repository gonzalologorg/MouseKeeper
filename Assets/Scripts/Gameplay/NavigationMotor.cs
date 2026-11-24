using UnityEngine;
using UnityEngine.AI;

namespace MouseKeeper
{
    // The NavMeshAgent plans and avoids other actors; CharacterController remains
    // responsible for movement, collisions, knockback and jumping.
    [RequireComponent(typeof(CharacterController))]
    public sealed class NavigationMotor : MonoBehaviour
    {
        NavMeshAgent navigator;
        Vector3 lastTarget;
        float repathAt;
        NavMeshPath path;

        void Awake()
        {
            EnsureInitialized();
        }

        void EnsureInitialized()
        {
            if (path == null) path = new NavMeshPath();
            if (navigator != null) return;
            navigator = GetComponent<NavMeshAgent>();
            if (navigator == null) navigator = gameObject.AddComponent<NavMeshAgent>();
            navigator.updatePosition = false;
            navigator.updateRotation = false;
            navigator.radius = 0.4f;
            navigator.height = 1.8f;
            navigator.speed = 4f;
            navigator.acceleration = 40f;
            navigator.stoppingDistance = 0.1f;
            navigator.autoRepath = true;
            navigator.enabled = false;
        }

        public bool DirectionTo(Vector3 destination, float speed, out Vector3 direction)
        {
            direction = Vector3.zero;
            EnsureInitialized();
            if (!NavMesh.SamplePosition(transform.position, out NavMeshHit start, 1.5f, NavMesh.AllAreas)) return false;
            if (!navigator.enabled) navigator.enabled = true;
            if (!navigator.isOnNavMesh && !navigator.Warp(start.position)) return false;
            navigator.isStopped = false;
            if ((navigator.nextPosition - start.position).sqrMagnitude > 1f)
            {
                if (!navigator.Warp(start.position)) return false;
                repathAt = 0f;
            }
            navigator.nextPosition = start.position;
            navigator.speed = Mathf.Max(0.1f, speed);
            if (Time.time >= repathAt || (destination - lastTarget).sqrMagnitude > 1f || !navigator.hasPath)
            {
                repathAt = Time.time + 0.3f;
                lastTarget = destination;
                if (!NavMesh.SamplePosition(destination, out NavMeshHit target, 1.5f, NavMesh.AllAreas) ||
                    !NavMesh.CalculatePath(start.position, target.position, NavMesh.AllAreas, path) ||
                    path.status != NavMeshPathStatus.PathComplete || !navigator.SetPath(path))
                {
                    navigator.ResetPath();
                    return false;
                }
            }
            direction = navigator.desiredVelocity;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f && navigator.hasPath)
            {
                direction = navigator.steeringTarget - start.position;
                direction.y = 0f;
            }
            if (direction.sqrMagnitude < 0.0001f) return false;
            direction.Normalize();
            return true;
        }

        public void ResetNavigation()
        {
            if (navigator != null) navigator.enabled = false;
            repathAt = 0f;
        }

        public void Pause()
        {
            if (navigator != null && navigator.enabled && navigator.isOnNavMesh) navigator.isStopped = true;
        }
    }
}
