using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MouseKeeper
{
    public sealed partial class GameRun
    {
        void SpawnVent(Room room)
        {
            var vent = new GameObject("Next Level Vent");
            vent.transform.SetParent(room.root.transform, false);
            Visual(vent.transform, PrimitiveType.Cube, new Vector3(0f, 0.06f, 0f),
                new Vector3(1.8f, 0.12f, 1.8f), new Color(0.45f, 0.45f, 0.5f));
            for (int i = -2; i <= 2; i++)
                Visual(vent.transform, PrimitiveType.Cube, new Vector3(i * 0.3f, 0.13f, 0f),
                    new Vector3(0.12f, 0.03f, 1.4f), new Color(0.12f, 0.12f, 0.14f));
            BoxCollider trigger = vent.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = Vector3.up;
            trigger.size = new Vector3(2f, 2f, 2f);
            vent.AddComponent<PickupGlow>().Initialize(false);
            room.vent = trigger;
        }

        bool CanUseVent()
        {
            if (Finished || IsTransitioning || currentRoom != bossRoom || currentRoom == null ||
                !currentRoom.bossDefeated || !CanLeave(currentRoom) || currentRoom.vent == null) return false;
            foreach (PlayerController agent in agents)
                if (!agent.Dead && currentRoom.vent.bounds.Contains(agent.transform.position + Vector3.up * 0.5f))
                    return true;
            return false;
        }

        public bool TryUseVent()
        {
            if (!CanUseVent()) return false;
            StartNextLevel();
            return true;
        }

        void StartNextLevel()
        {
            ClearWeaponOrder();
            RecordCompletedFloor();
            foreach (PlayerController agent in agents)
                if (agent != null && agent.Dialogue != null) agent.Dialogue.ResetFloor();
            ReturnDownedAgents();
            foreach (PlayerController agent in agents)
                if (!agent.Dead) agent.RoomExit();
            levelIndex++;
            GenerateFloor();
        }

        void GenerateFloor()
        {
            floorGore?.ReleaseFloor();
            foreach (Room room in rooms)
            {
                if (room.root == null) continue;
                room.root.SetActive(false);
                Destroy(room.root);
            }
            foreach (Object asset in levelAssets)
                if (asset != null) Destroy(asset);
            levelAssets.Clear();
            rooms.Clear();
            currentRoom = null;
            bossRoom = null;
            if (minimap != null) minimap.ClearRooms();
            Finished = false;
            message = null;
            GenerateMaze();
            foreach (Room room in rooms)
            {
                currentRoom = room;
                BuildRoom(room);
            }
            currentRoom = null;
            EnterRoom(rooms[0], -1);
        }

        bool pendingVent;
        bool pendingExit;

        void RecordCompletedFloor()
        {
            if (FloorCleared())
            {
                fullyClearedFloors++;
                completedFloorCredit += 1f;
            }
            else
                completedFloorCredit += 1f - (float)ClearedRoomCount() / rooms.Count;
        }

        bool FloorCleared()
        {
            if (bossRoom == null || !bossRoom.bossDefeated) return false;
            foreach (Room room in rooms)
                if (!room.visited || !room.cleared || room.spawning) return false;
            return true;
        }

        bool CanUseExit()
        {
            if (Finished || IsTransitioning || currentRoom != bossRoom || !currentRoom.bossDefeated || !CanLeave(currentRoom) ||
                currentRoom.exitDoor == null) return false;
            Vector3 position = ExitApproach(currentRoom);
            foreach (PlayerController agent in agents)
                if (!agent.Dead && (agent.transform.position - position).sqrMagnitude < 2.5f * 2.5f)
                    return true;
            return false;
        }

        Vector3 ExitApproach(Room room)
        {
            Vector2Int direction = Directions[room.exitDirection];
            return room.exitDoor.transform.position - new Vector3(direction.x, 0f, direction.y) * 1.5f;
        }

        IEnumerator TravelClearedRoute(Room destination)
        {
            var previous = new Dictionary<Room, Room>();
            var queue = new Queue<Room>();
            previous[currentRoom] = null;
            queue.Enqueue(currentRoom);
            while (queue.Count > 0 && !previous.ContainsKey(destination))
            {
                Room room = queue.Dequeue();
                foreach (Room neighbour in room.neighbours)
                    if (neighbour != null && neighbour.visited && CanLeave(neighbour) && !previous.ContainsKey(neighbour))
                    {
                        previous[neighbour] = room;
                        queue.Enqueue(neighbour);
                    }
            }
            if (!previous.ContainsKey(destination))
            {
                ShowMessage("No hay un camino despejado hasta esa sala.");
                yield break;
            }
            var path = new Stack<Room>();
            for (Room room = destination; previous[room] != null; room = previous[room]) path.Push(room);
            while (path.Count > 0)
            {
                Room next = path.Pop();
                int direction = System.Array.IndexOf(currentRoom.neighbours, next);
                yield return TravelToRoom(next, direction);
            }
        }

        void TravelToClearedRoom(Room destination)
        {
            if (destination == currentRoom) return;
            if (!destination.visited || !CanLeave(destination))
            {
                ShowMessage("Solo puedes seleccionar salas ya despejadas.");
                return;
            }
            if (!CanLeave(currentRoom) || transitionDelay > 0f)
            {
                ShowMessage("Limpia la sala antes de salir.");
                return;
            }
            pendingVent = false;
            pendingExit = false;
            StartCoroutine(TravelClearedRoute(destination));
        }

#if UNITY_EDITOR
        bool HandleEditorDebugKeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return false;
            if (keyboard.f1Key.wasPressedThisFrame) ApplyEditorDebug(1);
            else if (keyboard.f2Key.wasPressedThisFrame) ApplyEditorDebug(2);
            else if (keyboard.f3Key.wasPressedThisFrame) ApplyEditorDebug(3);
            else if (keyboard.f4Key.wasPressedThisFrame) ApplyEditorDebug(4);
            else if (keyboard.f5Key.wasPressedThisFrame) ApplyEditorDebug(5);
            else if (keyboard.f6Key.wasPressedThisFrame) ApplyEditorDebug(6);
            else return false;
            return true;
        }

        void ApplyEditorDebug(int command)
        {
            if (Finished || currentRoom == null) { ShowMessage("Depuración no disponible en este momento."); return; }
            switch (command)
            {
                case 1:
                    foreach (PlayerController agent in agents)
                        if (agent != null && !agent.Dead && agent.Health < agent.MaxHealth)
                            agent.Heal(agent.MaxHealth - agent.Health);
                    ShowMessage("Agentes vivos curados.");
                    break;
                case 2:
                case 3:
                    foreach (PlayerController agent in agents)
                    {
                        if (agent == null) continue;
                        int before = agent.Karma;
                        agent.ChangeKarma(command == 2 ? 5 : -5);
                        if (before < -4 && agent.Karma >= -4)
                            foreach (PlayerController observer in agents)
                                if (observer != null) observer.ForgetReprimand(agent);
                    }
                    ShowMessage(command == 2 ? "Karma +5." : "Karma -5.");
                    break;
                case 4:
                    if (IsTransitioning) { ShowMessage("Espera a terminar la transición."); return; }
                    DebugClearRoom();
                    ShowMessage("Ratas de la sala eliminadas.");
                    break;
                case 5:
                    StopAllCoroutines();
                    ClearWeaponOrder();
                    pendingVent = pendingExit = false;
                    IsTransitioning = false;
                    foreach (PlayerController agent in agents)
                    {
                        if (agent == null) continue;
                        if (agent.Dialogue != null) agent.Dialogue.ResetFloor();
                        if (!agent.Dead) agent.RoomExit();
                    }
                    GenerateFloor();
                    ShowMessage("Piso reiniciado.");
                    break;
                case 6:
                    var living = new List<PlayerController>();
                    foreach (PlayerController agent in agents)
                        if (agent != null && !agent.Dead) living.Add(agent);
                    if (living.Count == 0) { ShowMessage("No quedan agentes vivos."); return; }
                    living[Random.Range(0, living.Count)].DebugKill();
                    break;
                default: Debug.LogError("Comando de depuración de expedición desconocido."); break;
            }
        }

        void DebugClearRoom()
        {
            if (currentRoom.cleared) return;
            MouseEnemy[] mice = currentRoom.root.GetComponentsInChildren<MouseEnemy>();
            bool interruptedSpawn = currentRoom.spawning;
            if (interruptedSpawn)
            {
                currentRoom.spawning = false;
                currentRoom.enemies = 0;
                foreach (MouseEnemy mouse in mice)
                    if (mouse != null && !mouse.IsDefeated) currentRoom.enemies++;
                foreach (PlayerController agent in agents)
                    if (agent != null && !agent.Dead) agent.RoomEnter();
            }
            foreach (MouseEnemy mouse in mice)
                if (mouse != null && !mouse.IsDefeated) mouse.TakeDamage(float.MaxValue, player);
            if (currentRoom.enemies == 0 && !currentRoom.cleared) CompleteCombatRoom(currentRoom);
        }
#endif

        bool speedingUp;
        float previousTimeScale;

        void SetFastForward(bool enabled)
        {
            if (speedingUp == enabled) return;
            speedingUp = enabled;
            if (enabled)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = previousTimeScale * 2f;
            }
            else Time.timeScale = previousTimeScale;
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            SetFastForward(!Finished && currentRoom != null && Application.isFocused &&
                keyboard != null && keyboard.spaceKey.isPressed);
#if UNITY_EDITOR
            if (HandleEditorDebugKeys()) return;
#endif
            if (Finished)
            {
                if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
                {
                    if (returnToPlanningAfterDefeat) SceneManager.LoadScene("Planning");
                    else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
                return;
            }
            transitionDelay -= Time.deltaTime;
            if (IsTransitioning || currentRoom == null) return;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
                    SetFormation(SquadFormation.Focus);
                else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
                    SetFormation(SquadFormation.KeepDistance);
                else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
                    SetFormation(SquadFormation.Nearest);
            }
            TryBuyShopRevival();
            if (weaponAssignee != null && (orderedWeapon == null || !orderedWeapon.Available))
            {
                ClearWeaponOrder();
                ShowMessage("El arma ya no está disponible.");
            }
            if (pendingVent && CanUseVent())
            {
                pendingVent = false;
                StartNextLevel();
                return;
            }
            if (pendingExit && CanUseExit())
            {
                pendingExit = false;
                Finished = true;
                RecordCompletedFloor();
                ExpeditionSession.Settle(completedFloorCredit, agents.ToArray(), true);
                SceneManager.LoadScene("Planning");
                return;
            }
            Mouse mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 mousePosition = mouse.position.ReadValue();
            Vector2 guiPosition = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
            if (mouse.leftButton.wasPressedThisFrame)
                for (int i = 0; i < 3; i++)
                    if (FormationButtonRect(i).Contains(guiPosition))
                    {
                        SetFormation((SquadFormation)i);
                        return;
                    }
            if (!mouse.rightButton.wasPressedThisFrame || FormationButtonsRect().Contains(guiPosition)) return;
            if (minimap != null && minimap.ContainsScreenPoint(mousePosition))
            {
                if (minimap.TryGetRoomAtScreenPoint(mousePosition, out Vector2Int position))
                    foreach (Room room in rooms)
                        if (room.position == position) { TravelToClearedRoom(room); break; }
                return;
            }
            Ray ray = viewCamera.ScreenPointToRay(mousePosition);
            Plane ground = new Plane(Vector3.up, currentRoom.center);
            bool hitsGround = ground.Raycast(ray, out float distance);
            RaycastHit[] hits = Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hitsGround && hit.distance > distance + 0.01f) break;
                MouseEnemy enemy = hit.collider.GetComponentInParent<MouseEnemy>();
                if (enemy != null && FocusMouse(enemy)) return;
                ItemPickup pickup = hit.collider.GetComponentInParent<ItemPickup>();
                if (pickup != null && pickup.Available && pickup.Kind == PickupKind.Weapon &&
                    pickup.transform.IsChildOf(currentRoom.root.transform))
                {
                    AssignWeaponPickup(pickup);
                    return;
                }
                RoomDoor door = hit.collider.GetComponentInParent<RoomDoor>();
                if (door != null)
                {
                    if (door == currentRoom.exitDoor)
                    {
                        if (!currentRoom.bossDefeated || !CanLeave(currentRoom)) ShowMessage("Derrota al jefe y despeja esta sala.");
                        else
                        {
                            pendingExit = true;
                            pendingVent = false;
                            OrderSquad(ExitApproach(currentRoom));
                        }
                        return;
                    }
                    for (int i = 0; i < 4; i++)
                        if (currentRoom.doors[i] == door && currentRoom.neighbours[i] != null)
                        {
                            if (!CanLeave(currentRoom) || transitionDelay > 0f)
                                ShowMessage("Limpia la sala antes de salir.");
                            else
                            {
                                pendingVent = false;
                                pendingExit = false;
                                StartCoroutine(TravelToRoom(currentRoom.neighbours[i], i));
                            }
                            return;
                        }
                }
                if (currentRoom.vent != null && hit.collider == currentRoom.vent && currentRoom.cleared)
                {
                    pendingVent = true;
                    pendingExit = false;
                    OrderSquad(currentRoom.vent.transform.position);
                    return;
                }
            }
            if (hitsGround)
            {
                Vector3 point = ray.GetPoint(distance);
                foreach (Room room in rooms)
                {
                    if (room == currentRoom || !room.visited || !CanLeave(room)) continue;
                    Vector3 target = point - room.center;
                    if (Mathf.Abs(target.x) > room.width * 0.5f || Mathf.Abs(target.z) > room.depthSize * 0.5f) continue;
                    TravelToClearedRoom(room);
                    return;
                }
                Vector3 local = point - currentRoom.center;
                if (Mathf.Abs(local.x) <= currentRoom.width * 0.5f && Mathf.Abs(local.z) <= currentRoom.depthSize * 0.5f)
                {
                    pendingVent = false;
                    pendingExit = false;
                    OrderSquad(point);
                }
            }
        }

        void OrderSquad(Vector3 point)
        {
            ClearWeaponOrder();
            for (int i = 0; i < agents.Count; i++)
            {
                if (agents[i].Dead) continue;
                Vector3 offset = Quaternion.Euler(0f, i * 120f, 0f) * Vector3.forward * 0.9f;
                agents[i].IssueMoveOrder(ClampToCurrentRoom(point + offset));
            }
        }

        const float DeathShakeDuration = 0.25f;
        float deathShakeRemaining;
        Vector3 cameraShakeOffset;

        void ShakeCameraOnDeath() => deathShakeRemaining = DeathShakeDuration;

        void LateUpdate()
        {
            if (viewCamera == null) return;

            Transform cameraTransform = viewCamera.transform;
            Vector3 position = cameraTransform.position - cameraShakeOffset;
            if (player != null)
            {
                if (currentRoom != null && Mathf.Abs(viewCamera.aspect - cameraAspect) > 0.001f)
                    FitCameraToRoom(currentRoom);
                position = Vector3.Lerp(position, cameraFocus - cameraTransform.forward * cameraDistance,
                    1f - Mathf.Exp(-8f * Time.deltaTime));
            }

            cameraShakeOffset = Vector3.zero;
            if (deathShakeRemaining > 0f)
            {
                deathShakeRemaining = Mathf.Max(0f, deathShakeRemaining - Time.deltaTime);
                float remaining = deathShakeRemaining / DeathShakeDuration;
                float phase = (1f - remaining) * (4f * Mathf.PI);
                cameraShakeOffset = (cameraTransform.right * Mathf.Sin(phase) +
                    cameraTransform.up * (0.45f * Mathf.Sin(phase * 1.5f))) * (0.11f * remaining * remaining);
            }
            cameraTransform.position = position + cameraShakeOffset;
        }

        void OnDisable()
        {
            SetFastForward(false);
            if (viewCamera != null) viewCamera.transform.position -= cameraShakeOffset;
            cameraShakeOffset = Vector3.zero;
            deathShakeRemaining = 0f;
        }

    }
}
