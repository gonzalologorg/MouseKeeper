using System.Collections.Generic;
using UnityEngine;

namespace MouseKeeper
{
    public sealed partial class GameRun
    {
        Room pendingCorridorEntry;
        Room pendingCorridorDestination;
        int pendingCorridorDirection;

        List<Room> CorridorRoute(Room start, Room destination)
        {
            if (start == null || destination == null) return null;
            var previous = new Dictionary<Room, Room> { [start] = null };
            var queue = new Queue<Room>();
            queue.Enqueue(start);
            while (queue.Count > 0 && !previous.ContainsKey(destination))
            {
                Room room = queue.Dequeue();
                foreach (Room neighbour in room.neighbours)
                    if (neighbour != null && neighbour.kind == RoomKind.Corridor && !previous.ContainsKey(neighbour))
                    {
                        previous[neighbour] = room;
                        queue.Enqueue(neighbour);
                    }
            }
            if (!previous.ContainsKey(destination)) return null;
            var path = new List<Room>();
            for (Room room = destination; room != null; room = previous[room]) path.Add(room);
            path.Reverse();
            return path;
        }

        void OrderCorridorSquad(Vector3 point, Room destination)
        {
            if (destination == null) return;
            ClearWeaponOrder();
            for (int i = 0; i < agents.Count; i++)
            {
                PlayerController agent = agents[i];
                if (agent == null || agent.Dead) continue;
                List<Room> route = CorridorRoute(ClosestCorridor(agent.transform.position), destination);
                if (route == null) continue;
                Vector3 offset = Quaternion.Euler(0f, i * 120f, 0f) * Vector3.forward * 0.9f;
                var waypoints = new List<Vector3>();
                if (route.Count > 1)
                    foreach (Room room in route) waypoints.Add(ProjectOntoCorridor(room, room.center + offset));
                waypoints.Add(ProjectOntoCorridor(destination, point + offset));
                agent.IssueCorridorPath(SimplifyCorridorWaypoints(agent.transform.position, waypoints));
            }
        }

        List<Vector3> SimplifyCorridorWaypoints(Vector3 start, List<Vector3> waypoints)
        {
            var simplified = new List<Vector3>();
            int first = 0;
            while (first < waypoints.Count)
            {
                int next = first;
                for (int candidate = waypoints.Count - 1; candidate > first; candidate--)
                    if (CorridorLineWalkable(start, waypoints[candidate])) { next = candidate; break; }
                start = waypoints[next];
                simplified.Add(start);
                first = next + 1;
            }
            return simplified;
        }

        bool CorridorLineWalkable(Vector3 start, Vector3 end)
        {
            Vector3 delta = end - start;
            delta.y = 0f;
            int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / 0.3f));
            for (int i = 1; i <= steps; i++)
            {
                Vector3 point = Vector3.Lerp(start, end, (float)i / steps);
                Room room = ClosestCorridor(point);
                if (room == null) return false;
                Vector3 offset = ProjectOntoCorridor(room, point) - point;
                offset.y = 0f;
                if (offset.sqrMagnitude > 0.25f * 0.25f) return false;
            }
            return true;
        }

        void UpdateCorridorContext()
        {
            if (currentRoom == null || currentRoom.kind != RoomKind.Corridor || IsTransitioning) return;
            Vector3 center = Vector3.zero;
            int count = 0;
            foreach (PlayerController agent in agents)
                if (agent != null && !agent.Dead) { center += agent.transform.position; count++; }
            if (count == 0) return;
            Room room = ClosestCorridor(center / count);
            if (room == null || room == currentRoom) return;
            currentRoom = room;
            room.visited = true;
            RefreshRoomLighting();
            RefreshMinimap();
            RefreshDoors();
        }

        bool QueueCorridorDoor(Room destination)
        {
            if (currentRoom == null || currentRoom.kind != RoomKind.Corridor || destination == null) return false;
            if (destination == bossRoom && !BossRevealed()) { ShowMessage("La puerta está cerrada."); return true; }
            for (int i = 0; i < 4; i++)
            {
                Room entry = destination.neighbours[i];
                if (entry == null || entry.kind != RoomKind.Corridor) continue;
                int direction = (i + 2) % 4;
                Vector2Int d = Directions[direction];
                Vector3 forward = new Vector3(d.x, 0f, d.y);
                float half = direction % 2 == 0 ? entry.depthSize * 0.5f : entry.width * 0.5f;
                OrderCorridorSquad(entry.center + forward * (half - 2f), entry);
                pendingCorridorEntry = entry;
                pendingCorridorDestination = destination;
                pendingCorridorDirection = direction;
                return true;
            }
            return false;
        }

        void UpdatePendingCorridorDoor()
        {
            if (pendingCorridorDestination == null || IsTransitioning) return;
            foreach (PlayerController agent in agents)
                if (agent != null && !agent.Dead && !agent.ReachedDestination) return;
            Room entry = pendingCorridorEntry;
            Room destination = pendingCorridorDestination;
            int direction = pendingCorridorDirection;
            pendingCorridorEntry = pendingCorridorDestination = null;
            if (currentRoom != entry || entry.neighbours[direction] != destination ||
                (destination == bossRoom && !BossRevealed())) return;
            StartCoroutine(TravelToRoom(destination, direction));
        }

        void ClearPendingCorridorDoor() => pendingCorridorEntry = pendingCorridorDestination = null;
    }
}
