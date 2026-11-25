using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MouseKeeper
{
    public sealed partial class GameRun
    {
        void GenerateMaze()
        {
            widthPhase = Random.Range(0, 3);
            depthPhase = Random.Range(0, 3);
            var occupied = new Dictionary<Vector2Int, Room>();
            var start = new Room { position = Vector2Int.zero };
            rooms.Add(start);
            occupied.Add(start.position, start);
            int extraRooms = ExpeditionSession.ExtraRooms(ExpeditionSession.Difficulty);
            int desired = Random.Range(5, 9) + (levelIndex >= ExpeditionSession.FloorCount - extraRooms ? 1 : 0);
            var chambers = new List<Room> { start };
            var spine = new List<Room>();
            Room previous = start;
            int turn = Random.value < 0.5f ? 0 : 2;
            bool bent = Random.value < 0.8f;
            // One continuous hallway forms the backbone; rooms branch directly off it.
            for (int i = 0; i < 5; i++)
            {
                int direction = bent && i >= 3 ? turn : 1;
                Vector2Int position = previous.position + Directions[direction];
                var corridor = new Room { position = position, depth = previous.depth + 1, kind = RoomKind.Corridor };
                previous.neighbours[direction] = corridor;
                corridor.neighbours[(direction + 2) % 4] = previous;
                rooms.Add(corridor);
                occupied.Add(position, corridor);
                spine.Add(corridor);
                previous = corridor;
            }

            // Prefer a different hallway segment for each room before adding a second room.
            int offset = Random.Range(0, spine.Count);
            while (chambers.Count < desired)
            {
                bool added = false;
                for (int n = 0; n < spine.Count && chambers.Count < desired; n++)
                {
                    Room corridor = spine[(n + offset) % spine.Count];
                    int firstDirection = Random.Range(0, 4);
                    for (int j = 0; j < 4; j++)
                    {
                        int direction = (firstDirection + j) % 4;
                        Vector2Int position = corridor.position + Directions[direction];
                        if (position.x < 0 || corridor.neighbours[direction] != null || occupied.ContainsKey(position)) continue;
                        var chamber = new Room { position = position, depth = corridor.depth + 1 };
                        corridor.neighbours[direction] = chamber;
                        chamber.neighbours[(direction + 2) % 4] = corridor;
                        rooms.Add(chamber);
                        chambers.Add(chamber);
                        occupied.Add(position, chamber);
                        added = true;
                        break;
                    }
                }
                if (!added)
                {
                    rooms.Clear();
                    GenerateMaze();
                    return;
                }
                offset = (offset + 1) % spine.Count;
            }

            for (int i = 0; i < rooms.Count; i++)
            {
                Room room = rooms[i];
                room.width = AxisSize(room.position.x, true);
                room.depthSize = AxisSize(room.position.y, false);
                room.center = new Vector3(AxisCenter(room.position.x, true), 0f, AxisCenter(room.position.y, false));
                room.hasPillars = room.kind != RoomKind.Corridor && i % 2 == 1;
            }

            float AxisSize(int index, bool horizontal)
            {
                int phase = horizontal ? widthPhase : depthPhase;
                int variant = ((index + phase) % 3 + 3) % 3 - 1;
                return Mathf.Max(8f, (horizontal ? roomWidth : roomDepth) + variant * 4f);
            }

            float AxisCenter(int index, bool horizontal)
            {
                float center = 0f;
                int step = index < 0 ? -1 : 1;
                for (int i = 0; i != index; i += step)
                    center += step * (AxisSize(i, horizontal) + AxisSize(i + step, horizontal)) * 0.5f;
                return center;
            }
            bossRoom = start;
            foreach (Room room in rooms)
            {
                if (room.kind == RoomKind.Corridor) continue;
                if (room.depth <= bossRoom.depth) continue;
                bool hasChildren = false;
                foreach (Room neighbour in room.neighbours)
                    if (neighbour != null && neighbour.depth > room.depth) { hasChildren = true; break; }
                if (hasChildren) continue;
                bool hasExterior = false;
                foreach (Vector2Int direction in Directions)
                    if (!occupied.ContainsKey(room.position + direction)) { hasExterior = true; break; }
                if (levelIndex != ExpeditionSession.FloorCount - 1 || hasExterior) bossRoom = room;
            }
            if (bossRoom == start)
            {
                rooms.Clear();
                GenerateMaze();
                return;
            }
            var specialRooms = new List<Room>(chambers);
            specialRooms.Remove(start);
            specialRooms.Remove(bossRoom);
            if (specialRooms.Count > 0)
            {
                int index = Random.Range(0, specialRooms.Count);
                specialRooms[index].kind = levelIndex % 2 == 0 ? RoomKind.Treasure : RoomKind.Shop;
            }
            if (levelIndex == ExpeditionSession.FloorCount - 1)
                for (int i = 0; i < 4; i++)
                    if (!occupied.ContainsKey(bossRoom.position + Directions[i])) { bossRoom.exitDirection = i; break; }
        }

        void EnterRoom(Room room, int entryDirection)
        {
            currentRoom = room;
            Vector3 position = room.root.transform.position;
            if (entryDirection >= 0)
            {
                Vector2Int direction = Directions[entryDirection];
                position += new Vector3(direction.x * (room.width / 2f - 2f), 0f,
                    direction.y * (room.depthSize / 2f - 2f));
            }
            position.y = 0.05f;
            PlaceSquad(position, entryDirection);
            FitCameraToRoom(room);
            ArriveInRoom(room);
        }

        IEnumerator EnterFromIntro(Room room)
        {
            EnterRoom(room, 3);
            IsTransitioning = true;
            RoomDoor entrance = room.doors[3];
            if (entrance != null) entrance.SetOpen(true);
            Vector3 doorway = room.center + Vector3.left * (room.width * 0.5f - 0.6f);
            doorway.y = 0.05f;
            PlaceSquad(doorway, 3);
            for (int i = 0; i < agents.Count; i++)
            {
                float offset = (i - (agents.Count - 1) * 0.5f) * 1.1f;
                agents[i].SetDestination(room.center + Vector3.left * (room.width * 0.5f - 2.5f) + Vector3.forward * offset);
            }
            bool arrived;
            float elapsed = 0f;
            do
            {
                arrived = true;
                elapsed += Time.deltaTime;
                for (int i = 0; i < agents.Count; i++)
                {
                    PlayerController agent = agents[i];
                    if (agent == null || agent.Dead || agent.ReachedDestination) continue;
                    if (elapsed >= 5f)
                    {
                        float offset = (i - (agents.Count - 1) * 0.5f) * 1.1f;
                        RescueTransitionAgent(agent, room.center + Vector3.left * (room.width * 0.5f - 2.5f) + Vector3.forward * offset, "intro");
                    }
                    else arrived = false;
                }
                yield return null;
            } while (!arrived);
            if (entrance != null) entrance.SetOpen(false);
            IsTransitioning = false;
        }

        void PlaceSquad(Vector3 position, int entryDirection = -1)
        {
            for (int i = 0; i < agents.Count; i++)
            {
                PlayerController agent = agents[i];
                if (agent.Dead) continue;
                CharacterController controller = agent.GetComponent<CharacterController>();
                controller.enabled = false;
                float offset = (i - (agents.Count - 1) * 0.5f) * 1.1f;
                agent.transform.position = position + (entryDirection % 2 == 1
                    ? new Vector3(0f, 0f, offset) : new Vector3(offset, 0f, 0f));
                controller.enabled = true;
                agent.SetDestination(agent.transform.position);
            }
            transitionDelay = 0.5f;
        }

        IEnumerator TravelToRoom(Room next, int direction)
        {
            if (next == bossRoom && !BossRevealed()) yield break;
            ClearWeaponOrder();
            IsTransitioning = true;
            Room previous = currentRoom;
            if (previous.kind != RoomKind.Corridor)
                foreach (PlayerController agent in agents)
                    if (!agent.Dead) agent.RoomExit();
            Vector2Int d = Directions[direction];
            Vector3 forward = new Vector3(d.x, 0f, d.y);
            Vector3 side = new Vector3(-forward.z, 0f, forward.x);
            float halfSize = direction % 2 == 0 ? previous.depthSize * 0.5f : previous.width * 0.5f;
            Vector3 doorway = previous.center + forward * halfSize;
            RoomDoor departureDoor = previous.doors[direction];
            RoomDoor arrivalDoor = next.doors[(direction + 2) % 4];
            if (departureDoor != null) { departureDoor.SetHint(false); departureDoor.SetOpen(true); }
            if (arrivalDoor != null) arrivalDoor.SetOpen(true);
            if (departureDoor != null || arrivalDoor != null) yield return new WaitForSeconds(0.55f);
            float[] travelTime = new float[agents.Count];
            bool[] rescued = new bool[agents.Count];
            for (int phase = 0; phase < 2; phase++)
            {
                for (int i = 0; i < agents.Count; i++)
                    if (agents[i] != null && !agents[i].Dead && !rescued[i])
                        agents[i].SetDestination(doorway + forward * (phase == 0 ? -1.6f : 2f) + side * ((i - 1) * 0.8f));
                bool arrived;
                do
                {
                    arrived = true;
                    for (int i = 0; i < agents.Count; i++)
                    {
                        PlayerController agent = agents[i];
                        if (agent == null || agent.Dead || rescued[i] || agent.ReachedDestination) continue;
                        travelTime[i] += Time.deltaTime;
                        if (travelTime[i] >= 5f)
                        {
                            RescueTransitionAgent(agent, doorway + forward * 2f + side * ((i - 1) * 0.8f), "room_transition");
                            rescued[i] = true;
                        }
                        else arrived = false;
                    }
                    yield return null;
                } while (!arrived);
            }
            currentRoom = next;
            previousRoom = previous;
            previousRoomDirection = (direction + 2) % 4;
            FitCameraToRoom(next);
            transitionDelay = 0.5f;
            IsTransitioning = false;
            ArriveInRoom(next);
        }

        void RescueTransitionAgent(PlayerController agent, Vector3 position, string reason)
        {
            position.y = 0.05f;
            agent.RecoverNavigationPosition(position, true);
            BalanceLog("agent_unstuck", agent, null, reason, "", 5f);
        }

        void FitCameraToRoom(Room room)
        {
            if (room.kind == RoomKind.Corridor)
            {
                cameraAspect = viewCamera.aspect;
                cameraDistance = 30f;
                viewCamera.orthographicSize = 11f;
                viewCamera.farClipPlane = Mathf.Max(viewCamera.farClipPlane, 250f);
                cameraFocus = room.center;
                return;
            }
            Vector3 right = viewCamera.transform.right;
            cameraAspect = viewCamera.aspect;
            Vector3 up = viewCamera.transform.up;
            Vector3 forward = viewCamera.transform.forward;
            float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
            float minDepth = float.PositiveInfinity, maxDepth = float.NegativeInfinity;
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    for (int y = 0; y <= 1; y++)
                    {
                        Vector3 point = room.center + new Vector3(x * room.width * 0.5f, y * 3.9f, z * room.depthSize * 0.5f);
                        float px = Vector3.Dot(point, right);
                        float py = Vector3.Dot(point, up);
                        float depth = Vector3.Dot(point, forward);
                        minX = Mathf.Min(minX, px); maxX = Mathf.Max(maxX, px);
                        minY = Mathf.Min(minY, py); maxY = Mathf.Max(maxY, py);
                        minDepth = Mathf.Min(minDepth, depth); maxDepth = Mathf.Max(maxDepth, depth);
                    }
            Vector3 horizontalUp = new Vector3(up.x, 0f, up.z);
            cameraFocus = right * ((minX + maxX) * 0.5f) +
                horizontalUp * ((minY + maxY) * 0.5f / horizontalUp.sqrMagnitude);
            cameraDistance = Mathf.Max(20f, Vector3.Dot(cameraFocus, forward) - minDepth + 10f);
            viewCamera.farClipPlane = Mathf.Max(viewCamera.farClipPlane,
                cameraDistance + maxDepth - Vector3.Dot(cameraFocus, forward) + 10f);
            viewCamera.orthographicSize = Mathf.Max((maxY - minY) * 0.5f,
                (maxX - minX) * 0.5f / Mathf.Max(0.1f, viewCamera.aspect)) + 1f;
        }

        void ArriveInRoom(Room room)
        {
            TrackBalanceRoom();
            focusedMouse = null;
            fallbackFocus = null;
            room.visited = true;
            RefreshRoomLighting();
            RefreshMinimap();
            RefreshDoors();
            while (TryBuyShopRevival()) { }
            if (room.spawning)
            {
                if (room == bossRoom) StartCoroutine(SpawnBossEncounter(room));
                else StartCoroutine(SpawnRoomMice(room));
            }
            else if (room.kind != RoomKind.Corridor)
                foreach (PlayerController agent in agents)
                    if (!agent.Dead) agent.RoomEnter();
        }

        void BuildRoom(Room room)
        {
            room.root = new GameObject((room.kind == RoomKind.Combat ? "Room " : room.kind + " Room ") + room.position);
            room.root.transform.SetParent(transform);
            room.root.transform.position = room.center;
            if (room.kind == RoomKind.Combat && room != rooms[0] && room != bossRoom)
            {
                var choices = new List<RoomLayout>();
                if (roomLayouts != null)
                    foreach (RoomLayout layout in roomLayouts)
                        if (layout != null && (layout.hasRoomShell
                            ? Mathf.Approximately(layout.minimumRoomSize.x, room.width) &&
                              Mathf.Approximately(layout.minimumRoomSize.y, room.depthSize) &&
                              room.neighbours[(int)layout.doorSide] != null &&
                              room.neighbours[(int)layout.doorSide].kind == RoomKind.Corridor
                            : layout.minimumRoomSize.x <= room.width && layout.minimumRoomSize.y <= room.depthSize))
                            choices.Add(layout);
                if (choices.Count > 0)
                {
                    RoomLayout layout = Instantiate(choices[Random.Range(0, choices.Count)], room.root.transform);
                    if (layout.editorShell != null) layout.editorShell.SetActive(false);
                    if (layout.hasRoomShell) room.hasPillars = false;
                }
                else if (roomPrefab != null) Instantiate(roomPrefab, room.root.transform);
            }
            else if (room.kind != RoomKind.Corridor && roomPrefab != null) Instantiate(roomPrefab, room.root.transform);
            room.floor = Block(room.root.transform, "Floor", new Vector3(0f, -0.5f, 0f),
                room.kind == RoomKind.Corridor ? new Vector3(CorridorWidth, 1f, CorridorWidth) : new Vector3(room.width, 1f, room.depthSize),
                new Color(0.24f, 0.25f, 0.28f)).GetComponent<Collider>();
            int windowDirection = Random.Range(0, 4);
            for (int i = 0; i < 4; i++)
                BuildWall(room, i, i == windowDirection || i == (windowDirection + 2) % 4);
            if (room.kind == RoomKind.Corridor) BuildCorridor(room);
            foreach (Transform wall in room.walls)
                foreach (Renderer renderer in wall.GetComponentsInChildren<Renderer>(true))
                    room.wallRendererStates[renderer] = renderer.enabled;
            BuildPillars(room);
            SetWorldLayer(room.root);
            BuildFloorShadows(room);
            if (room.kind == RoomKind.Corridor)
            {
                room.cleared = true;
                UpdateDoors(room);
                return;
            }
            if (room == rooms[0])
            {
                room.cleared = true;
                UpdateDoors(room);
                return;
            }
            if (room.kind != RoomKind.Combat)
            {
                room.cleared = true;
                UpdateDoors(room);
                if (room.kind == RoomKind.Shop) PopulateShop();
                else PopulateTreasureRoom();
                return;
            }
            int count = MouseCountForRoom(room);
            RoomLayout roomLayout = room.root.GetComponentInChildren<RoomLayout>();
            if (room != bossRoom && roomLayout != null && roomLayout.enemyCount >= 0)
                count = roomLayout.enemyCount;
            room.enemies = count;
            room.spawning = count > 0;
            room.cleared = count == 0;
            UpdateDoors(room);
        }

        public const float CorridorWidth = 7f;

        void BuildCorridor(Room room)
        {
            float half = CorridorWidth * 0.5f;
            Color floorColor = new Color(0.24f, 0.25f, 0.28f);
            for (int i = 0; i < 4; i++)
            {
                bool horizontal = i % 2 == 1;
                float sign = i < 2 ? 1f : -1f;
                if (room.neighbours[i] == null)
                {
                    Vector3 center = horizontal ? new Vector3(sign * half, 0f, 0f) : new Vector3(0f, 0f, sign * half);
                    BuildCorridorWall(room, center, CorridorWidth, horizontal);
                    continue;
                }
                float length = (horizontal ? room.width : room.depthSize) * 0.5f - half;
                float middle = sign * (half + length * 0.5f);
                Vector3 floor = horizontal ? new Vector3(middle, -0.5f, 0f) : new Vector3(0f, -0.5f, middle);
                Vector3 floorSize = horizontal ? new Vector3(length, 1f, CorridorWidth) : new Vector3(CorridorWidth, 1f, length);
                Block(room.root.transform, "Corridor floor", floor, floorSize, floorColor);
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 wall = horizontal ? new Vector3(middle, 0f, side * half) : new Vector3(side * half, 0f, middle);
                    BuildCorridorWall(room, wall, length, !horizontal);
                }
            }
            PopulateCorridor(room);
        }

        void PopulateCorridor(Room room)
        {
            GameObject[] prefabs = roomArchitecture.corridorPrefabs;
            if (prefabs == null || prefabs.Length == 0) return;
            var available = new List<GameObject>();
            foreach (GameObject prefab in prefabs)
                if (prefab != null) available.Add(prefab);
            if (available.Count == 0) return;
            int firstDirection = Random.Range(0, 4);
            int firstPrefab = Random.Range(0, available.Count);
            int firstSide = Random.value < 0.5f ? -1 : 1;
            for (int d = 0; d < 4; d++)
            {
                int direction = (firstDirection + d) % 4;
                // Room connections lead to doors; leave those approaches clear.
                Room neighbour = room.neighbours[direction];
                if (neighbour == null || neighbour.kind != RoomKind.Corridor) continue;
                bool horizontal = direction % 2 == 1;
                float length = (horizontal ? room.width : room.depthSize) * 0.5f - CorridorWidth * 0.5f;
                if (length < 1f) continue;
                float sign = direction < 2 ? 1f : -1f;
                Vector3 along = horizontal ? Vector3.right : Vector3.forward;
                Vector3 midpoint = along * (sign * (CorridorWidth * 0.5f + length * 0.5f));
                for (int s = 0; s < 2; s++)
                {
                    int side = s == 0 ? firstSide : -firstSide;
                    Vector3 outward = (horizontal ? Vector3.forward : Vector3.right) * side;
                    for (int p = 0; p < available.Count; p++)
                        if (TryPlaceCorridorProp(room, available[(firstPrefab + p) % available.Count],
                            midpoint, along, outward, length)) return;
                }
            }
        }

        bool TryPlaceCorridorProp(Room room, GameObject prefab, Vector3 midpoint, Vector3 along, Vector3 outward, float length)
        {
            var placement = new GameObject("Corridor decoration");
            placement.transform.SetParent(room.root.transform, false);
            placement.transform.localPosition = midpoint;
            // The prefab faces south (-Z); point it away from this wall.
            placement.transform.localRotation = Quaternion.LookRotation(outward, Vector3.up);
            GameObject decoration = Instantiate(prefab, placement.transform, false);
            Vector3 position = decoration.transform.localPosition;
            decoration.transform.localPosition = new Vector3(0f, position.y, 0f);

            Renderer[] renderers = decoration.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) { placement.SetActive(false); Destroy(placement); return false; }
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            float alongExtent = Vector3.Dot(bounds.extents, new Vector3(Mathf.Abs(along.x), 0f, Mathf.Abs(along.z)));
            float depthExtent = Vector3.Dot(bounds.extents, new Vector3(Mathf.Abs(outward.x), 0f, Mathf.Abs(outward.z)));
            if (alongExtent * 2f > length - 0.2f || depthExtent * 2f > CorridorWidth * 0.32f)
            {
                placement.SetActive(false);
                Destroy(placement);
                return false;
            }
            float centerAlong = Vector3.Dot(bounds.center - (room.center + midpoint), along);
            float wallEdge = Vector3.Dot(bounds.center - room.center, outward) + depthExtent;
            placement.transform.localPosition += -along * centerAlong + outward * (CorridorWidth * 0.5f - 0.08f - wallEdge);
            return true;
        }

        void BuildCorridorWall(Room room, Vector3 position, float length, bool alongZ)
        {
            var wall = new GameObject("Corridor inner wall");
            wall.transform.SetParent(room.root.transform, false);
            wall.transform.localPosition = position;
            wall.transform.localRotation = Quaternion.Euler(0f, alongZ ? 90f : 0f, 0f);
            BuildWallSpan(wall.transform, -length * 0.5f, length, false);
        }

        static void SetWorldLayer(GameObject root)
        {
            int world = LayerMask.NameToLayer("World");
            if (world < 0)
            {
                Debug.LogError("La capa World no está configurada.");
                return;
            }
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = world;
        }

        int MouseCountForRoom(Room room)
        {
            bool hasBoss = room == bossRoom;
            if (hasBoss) return 1;
            if (!hasBoss && micePerRoom <= 0) return 0;

            int survivors = 0;
            int totalRating = 0;
            foreach (PlayerController agent in agents)
            {
                if (agent == null || agent.Dead || agent.Profile == null) continue;
                survivors++;
                totalRating += agent.Profile.Rating;
            }
            float averageRating = survivors == 0 ? 3f : (float)totalRating / survivors;
            float squadScale = Mathf.Lerp(0.7f, 1.1f, Mathf.InverseLerp(1f, 5f, averageRating));
            if (survivors > 0) squadScale *= Mathf.Lerp(0.75f, 1f, Mathf.InverseLerp(1f, ExpeditionSession.SquadSize, survivors));

            float relativeArea = room.width * room.depthSize / (roomWidth * roomDepth);
            float areaScale = Mathf.Clamp(relativeArea, 0.8f, 1.25f);
            float baseline = micePerRoom * 0.8f + levelIndex * 0.5f;
            float difficulty = ExpeditionSession.EnemyCountMultiplier(ExpeditionSession.Difficulty);
            int previousCount = 3 * Mathf.Clamp(Mathf.FloorToInt(baseline * areaScale * squadScale * difficulty + 0.5f), 1, 14);
            return Mathf.FloorToInt(previousCount * 1.25f + 0.5f);
        }

        const int BossRatLimit = 5;
        const float BossRatSpawnInterval = 2.66f;

        static float BossHealthMultiplier(int livingAgents) => 3f * Mathf.Max(1, livingAgents);

        static int BossRatCount(Room room)
        {
            int livingBoss = room.boss != null && !room.boss.IsDefeated ? 1 : 0;
            return Mathf.Max(0, room.enemies - livingBoss);
        }

        static int BossOffspringCount(Room room) => Mathf.Clamp(BossRatLimit - (BossRatCount(room) - 1), 0, 2);

        internal void QueueBossDamageSpawns(MouseEnemy mouse, float remainingHealth)
        {
            Room room = currentRoom;
            if (room == null || room.boss != mouse || mouse.maxHealth <= 0f || room.bossDefeated) return;
            int crossed = Mathf.Clamp(Mathf.FloorToInt((1f - Mathf.Clamp01(remainingHealth / mouse.maxHealth)) * 10f + 0.00001f), 0, 10);
            int added = crossed - room.bossDamageThresholds;
            if (added <= 0) return;
            room.bossDamageThresholds = crossed;
            room.pendingBossRats += added;
            room.bossSpawnOrigin = mouse.transform.position;
            room.spawning = true;
        }

        bool SpawnBossDamageRat(Room room)
        {
            EnemyPreset preset = enemySpawnPool.Choose(levelIndex, Random.value);
            if (preset == null) return false;
            Vector2 offset = Random.insideUnitCircle.normalized * 1.8f;
            Vector3 position = room.bossSpawnOrigin + new Vector3(offset.x, 0f, offset.y);
            position.x = Mathf.Clamp(position.x, room.center.x - room.width * 0.5f + 1f, room.center.x + room.width * 0.5f - 1f);
            position.z = Mathf.Clamp(position.z, room.center.z - room.depthSize * 0.5f + 1f, room.center.z + room.depthSize * 0.5f - 1f);
            return SpawnConfiguredMouse(room, position, false, preset, position, null);
        }

        IEnumerator SpawnBossEncounter(Room room)
        {
            int generation = ++room.spawnGeneration;
            if (!SpawnMouse(room, room.center, true))
            {
                room.enemies = 0;
                room.spawning = false;
                Debug.LogError("No se pudo crear el jefe de la sala.", this);
                yield break;
            }
            room.spawning = false;
            foreach (PlayerController agent in agents)
                if (agent != null && !agent.Dead) agent.RoomEnter();
            UpdateDoors(room);

            float naturalSpawnTimer = BossRatSpawnInterval;
            while (!Finished && currentRoom == room && room.spawnGeneration == generation && !room.cleared)
            {
                if (!IsTransitioning)
                {
                    while (room.pendingBossRats > 0 && BossRatCount(room) < BossRatLimit)
                    {
                        room.pendingBossRats--;
                        if (SpawnBossDamageRat(room)) room.enemies++;
                    }
                    room.spawning = room.pendingBossRats > 0;
                    if (room.bossDefeated && !room.spawning)
                    {
                        if (room.enemies == 0 && !room.cleared) CompleteCombatRoom(room);
                        yield break;
                    }
                    if (!room.bossDefeated && room.boss != null && !room.boss.IsDefeated)
                    {
                        naturalSpawnTimer -= Time.deltaTime;
                        if (naturalSpawnTimer <= 0f)
                        {
                            naturalSpawnTimer = BossRatSpawnInterval;
                            if (room.pendingBossRats == 0 && BossRatCount(room) < BossRatLimit && SpawnMouse(room, room.center, false)) room.enemies++;
                        }
                    }
                }
                yield return null;
            }
        }

        IEnumerator SpawnRoomMice(Room room)
        {
            int count = room.enemies;
            RoomLayout layout = room.root.GetComponentInChildren<RoomLayout>();
            for (int i = 0; i < count; i++)
            {
                Vector3 hide = new Vector3(i % 2 == 0 ? -room.width / 2f + 1.5f : room.width / 2f - 1.5f,
                    0f, i % 4 < 2 ? -room.depthSize / 2f + 1.5f : room.depthSize / 2f - 1.5f);
                hide = room.root.transform.TransformPoint(hide);
                if (layout != null && layout.hideSpots != null && layout.hideSpots.Length > 0)
                {
                    Transform spot = layout.hideSpots[i % layout.hideSpots.Length];
                    if (spot != null) hide = spot.position;
                }
                if (!SpawnMouse(room, hide, false)) room.enemies--;
                if (i + 1 < count)
                {
                    yield return new WaitForSeconds(Mathf.Max(0.01f, mouseSpawnInterval));
                    if (Finished || currentRoom != room || !room.spawning) yield break;
                }
            }
            room.spawning = false;
            foreach (PlayerController agent in agents)
                if (!agent.Dead) agent.RoomEnter();
            if (room.enemies == 0 && !room.cleared) CompleteCombatRoom(room);
            UpdateDoors(room);
        }

        void BuildWall(Room room, int direction, bool includeWindow)
        {
            bool horizontal = direction == 0 || direction == 2;
            float length = horizontal ? room.width : room.depthSize;
            float edge = (horizontal ? room.depthSize : room.width) / 2f;
            float sign = direction < 2 ? 1f : -1f;
            Vector3 center = horizontal ? new Vector3(0f, 0f, edge * sign) : new Vector3(edge * sign, 0f, 0f);
            var side = new GameObject("Wall " + direction);
            side.transform.SetParent(room.root.transform, false);
            room.walls[direction] = side.transform;
            side.transform.localPosition = center;
            side.transform.localRotation = Quaternion.Euler(0f, horizontal ? 0f : 90f, 0f);
            bool isExit = room.exitDirection == direction;
            bool isEntrance = levelIndex == 0 && room == rooms[0] && direction == 3;
            if (room.kind == RoomKind.Corridor)
            {
                Room neighbour = room.neighbours[direction];
                if (neighbour == null || neighbour.kind == RoomKind.Corridor) return;
                float corridorSide = (CorridorWidth - 3.9f) * 0.5f;
                BuildWallSpan(side.transform, -CorridorWidth * 0.5f, corridorSide, false);
                BuildWallSpan(side.transform, 1.95f, corridorSide, false);
                var corridorDoorway = new GameObject("Doorway");
                corridorDoorway.transform.SetParent(side.transform, false);
                RoomDoor corridorDoor = corridorDoorway.AddComponent<RoomDoor>();
                corridorDoor.Initialize(roomArchitecture.arch, roomArchitecture.door);
                room.doors[direction] = corridorDoor;
                return;
            }
            if (room.neighbours[direction] == null && !isExit && !isEntrance)
            {
                BuildWallSpan(side.transform, -length * 0.5f, length, includeWindow);
                return;
            }
            // The corridor owns the shared doorway and the wall immediately beside it.
            // Leave that entire span free here so its surfaces are not drawn twice.
            bool corridorNeighbour = room.neighbours[direction] != null &&
                room.neighbours[direction].kind == RoomKind.Corridor;
            float archWidth = corridorNeighbour ? CorridorWidth : 3.9f;
            float section = (length - archWidth) * 0.5f;
            BuildWallSpan(side.transform, -length * 0.5f, section, includeWindow);
            BuildWallSpan(side.transform, archWidth * 0.5f, section, false);
            var doorway = new GameObject("Doorway");
            doorway.transform.SetParent(side.transform, false);
            RoomDoor door = doorway.AddComponent<RoomDoor>();
            door.Initialize(roomArchitecture.arch, roomArchitecture.door);
            if (corridorNeighbour)
                foreach (Renderer renderer in doorway.GetComponentsInChildren<Renderer>(true))
                    renderer.enabled = false;
            if (isExit) room.exitDoor = door;
            else room.doors[direction] = door;
        }

        void BuildWallSpan(Transform parent, float start, float length, bool includeWindow)
        {
            const float moduleWidth = 5.278f;
            int count = Mathf.Max(1, Mathf.CeilToInt(length / moduleWidth));
            float width = length / count;
            int windowIndex = includeWindow ? Random.Range(0, count) : -1;
            for (int i = 0; i < count; i++)
            {
                GameObject[] choices = i == windowIndex ? roomArchitecture.windows : roomArchitecture.walls;
                GameObject model = Instantiate(choices[Random.Range(0, choices.Length)], parent);
                model.transform.localPosition = new Vector3(start + width * (i + 0.5f), 0f, 0f);
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = new Vector3(width / moduleWidth, 1f, 1f);
            }
            if (roomArchitecture.lamp != null && length >= 3f && (count > 1 || windowIndex < 0) &&
                (parent.name == "Wall 0" || parent.name == "Wall 1"))
            {
                int lampIndex = count / 2;
                if (lampIndex == windowIndex) lampIndex = (lampIndex + 1) % count;
                GameObject lamp = Instantiate(roomArchitecture.lamp, parent);
                lamp.transform.localPosition = new Vector3(start + width * (lampIndex + 0.5f), 2.4f, -0.12f);
                lamp.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }
            var colliderObject = new GameObject("Wall Collider");
            colliderObject.transform.SetParent(parent, false);
            BoxCollider wall = colliderObject.AddComponent<BoxCollider>();
            wall.center = new Vector3(start + length * 0.5f, 1.95f, 0f);
            wall.size = new Vector3(length, 3.9f, 0.2f);
        }

        void BuildPillars(Room room)
        {
            if (!room.hasPillars) return;
            GameObject prefab = roomArchitecture.pillars[Random.Range(0, roomArchitecture.pillars.Length)];
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    GameObject pillar = Instantiate(prefab, room.root.transform);
                    pillar.transform.localPosition = new Vector3(x * (room.width * 0.5f - 0.65f),
                        0f, z * (room.depthSize * 0.5f - 0.65f));
                    BoxCollider collider = pillar.AddComponent<BoxCollider>();
                    collider.center = new Vector3(0f, 1.95f, 0f);
                    collider.size = new Vector3(0.65f, 3.9f, 0.65f);
                }
        }

        void UpdateDoors(Room room)
        {
            for (int i = 0; i < room.doors.Length; i++)
            {
                RoomDoor door = room.doors[i];
                Room neighbour = room.neighbours[i];
                if (door == null || neighbour == null) continue;
                bool locked = neighbour == bossRoom && !BossRevealed();
                bool open = !locked && room.visited && neighbour.visited && CanLeave(room) && CanLeave(neighbour);
                door.SetOpen(open);
                door.SetHint(!locked && room == currentRoom && room.visited && CanLeave(room) && !neighbour.visited);
            }
            if (room.exitDoor != null) room.exitDoor.SetOpen(room.bossDefeated && CanLeave(room));
        }

        void RefreshDoors()
        {
            foreach (Room room in rooms) UpdateDoors(room);
        }

        bool CanLeave(Room room) => !room.spawning && room.cleared;

        bool SpawnMouse(Room room, Vector3 hide, bool boss)
        {
            EnemyPreset preset = boss ? enemySpawnPool.boss : enemySpawnPool.Choose(levelIndex, Random.value);
            return SpawnConfiguredMouse(room, hide, boss, preset, null, null);
        }

        bool SpawnConfiguredMouse(Room room, Vector3 hide, bool boss, EnemyPreset preset, Vector3? spawnPosition, MouseEnemy parent)
        {
            if (preset == null)
            {
                Debug.LogError("No hay enemigos con peso positivo para el piso " + (levelIndex + 1) + ".", this);
                return false;
            }

            GameObject root;
            if (preset.prefab != null) root = Instantiate(preset.prefab);
            else
            {
                root = new GameObject(boss ? "Boss Mouse" : preset.name);
                Visual(root.transform, PrimitiveType.Capsule, new Vector3(0f, 0.5f, 0f),
                    boss ? new Vector3(1.1f, 0.8f, 1.1f) : new Vector3(0.55f, 0.5f, 0.55f),
                    boss ? new Color(0.7f, 0.2f, 0.2f) : new Color(0.55f, 0.45f, 0.4f));
            }
            MouseEnemy mouse = root.GetComponent<MouseEnemy>();
            if (mouse == null)
            {
                CharacterController controller = root.GetComponent<CharacterController>();
                if (controller == null) controller = root.AddComponent<CharacterController>();
                CharacterController source = preset.controllerTemplate != null
                    ? preset.controllerTemplate.GetComponent<CharacterController>() : null;
                controller.center = source != null ? source.center : new Vector3(0f, 0.5f, 0f);
                controller.height = source != null ? source.height : 1f;
                controller.radius = source != null ? source.radius : 0.35f;
                if (source != null)
                {
                    controller.skinWidth = source.skinWidth;
                    controller.stepOffset = source.stepOffset;
                }
                mouse = root.AddComponent<MouseEnemy>();
                Animator sourceAnimator = preset.controllerTemplate != null
                    ? preset.controllerTemplate.GetComponentInChildren<Animator>() : null;
                if (sourceAnimator != null)
                {
                    Animator modelAnimator = root.GetComponentInChildren<Animator>();
                    if (modelAnimator == null) modelAnimator = root.AddComponent<Animator>();
                    modelAnimator.runtimeAnimatorController = sourceAnimator.runtimeAnimatorController;
                }
            }
            preset.ApplyTo(mouse);
            if (parent != null) root.transform.localScale = parent.transform.localScale * 0.67f;
            root.transform.position = spawnPosition ?? ChooseSpawnPoint(room);
            root.transform.SetParent(room.root.transform);
            Physics.SyncTransforms();
            CharacterController mouseController = root.GetComponent<CharacterController>();
            float bottom = mouseController.bounds.min.y;
            root.transform.position += Vector3.up * (room.floor.bounds.max.y + 0.02f - bottom);
            float levelMultiplier = Mathf.Pow(1.05f, levelIndex);
            mouse.maxHealth *= levelMultiplier;
            if (boss)
            {
                int livingAgents = 0;
                foreach (PlayerController agent in agents) if (agent != null && !agent.Dead) livingAgents++;
                mouse.maxHealth *= BossHealthMultiplier(livingAgents) * ExpeditionSession.BossDifficultyMultiplier(ExpeditionSession.Difficulty);
            }
            mouse.attackDamage *= levelMultiplier;
            mouse.attackInterval /= levelMultiplier;
            mouse.Initialize(this, player, hide, Mathf.Max(1, room.depth));
            if (parent != null)
            {
                mouse.InitializeOffspring(parent);
                Physics.SyncTransforms();
                root.transform.position += Vector3.up * (room.floor.bounds.max.y + 0.02f - mouseController.bounds.min.y);
            }
            foreach (MouseEnemy other in room.root.GetComponentsInChildren<MouseEnemy>())
            {
                if (other == mouse) continue;
                foreach (Collider ownCollider in root.GetComponentsInChildren<Collider>())
                    foreach (Collider otherCollider in other.GetComponentsInChildren<Collider>())
                        if (ownCollider.enabled && otherCollider.enabled && !ownCollider.isTrigger && !otherCollider.isTrigger)
                            Physics.IgnoreCollision(ownCollider, otherCollider);
            }
            if (boss) room.boss = mouse;
            RecordBalanceSpawn(room, mouse, boss);
            return true;
        }

        public Vector3 RandomPointInRoom()
        {
            Vector3 origin = currentRoom != null ? currentRoom.root.transform.position : Vector3.zero;
            float width = currentRoom != null ? currentRoom.width : roomWidth;
            float depth = currentRoom != null ? currentRoom.depthSize : roomDepth;
            return origin + new Vector3(Random.Range(-width / 2f + 2f, width / 2f - 2f), 0.05f,
                Random.Range(-depth / 2f + 2f, depth / 2f - 2f));
        }

        Vector3 ChooseSpawnPoint(Room room)
        {
            Vector3 best = RandomPointInRoom();
            float bestScore = -1f;
            MouseEnemy[] mice = room.root.GetComponentsInChildren<MouseEnemy>();
            for (int i = 0; i < 16; i++)
            {
                Vector3 candidate = RandomPointInRoom();
                PlayerController nearest = NearestAgent(candidate);
                float distance = nearest != null ? (candidate - nearest.transform.position).sqrMagnitude : 0f;
                foreach (MouseEnemy mouse in mice)
                    if ((candidate - mouse.transform.position).sqrMagnitude < 1.5f * 1.5f)
                        distance *= 0.1f;
                if (distance > bestScore) { best = candidate; bestScore = distance; }
            }
            return best;
        }

    }
}
