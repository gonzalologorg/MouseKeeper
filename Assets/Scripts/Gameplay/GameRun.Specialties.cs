using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace MouseKeeper
{
    public sealed partial class GameRun
    {
        Room previousRoom;
        int previousRoomDirection = -1;
        bool bardUsed;
        bool locksmithUsed;
        float medicCooldown;

        void ResetFloorSpecialties()
        {
            bardUsed = false;
            locksmithUsed = false;
            previousRoom = null;
            previousRoomDirection = -1;
            ClearBardBuff();
        }

        void ClearBardBuff()
        {
            foreach (PlayerController agent in agents)
                if (agent != null) agent.SetBardBuff(false);
        }

        public bool TryProtectMedicPosition(PlayerController protector, out Vector3 destination)
        {
            destination = protector.transform.position;
            if (currentRoom == null || currentRoom.cleared || currentRoom.root == null || protector.Dead ||
                protector.Specialty == Specialty.Medic) return false;
            PlayerController medic = null;
            foreach (PlayerController agent in agents)
                if (agent != null && !agent.Dead && agent.Specialty == Specialty.Medic) { medic = agent; break; }
            if (medic == null) return false;
            MouseEnemy threat = null;
            float bestDistance = 8f * 8f;
            foreach (MouseEnemy mouse in currentRoom.root.GetComponentsInChildren<MouseEnemy>())
            {
                if (mouse == null || !mouse.IsTargetable) continue;
                Vector3 offset = mouse.transform.position - medic.transform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude < bestDistance) { bestDistance = offset.sqrMagnitude; threat = mouse; }
            }
            if (threat == null) return false;
            Vector3 towardThreat = threat.transform.position - medic.transform.position;
            towardThreat.y = 0f;
            if (towardThreat.sqrMagnitude < 0.01f) return false;
            Vector3 forward = towardThreat.normalized;
            Vector3 side = new Vector3(-forward.z, 0f, forward.x);
            int index = agents.IndexOf(protector);
            float flank = protector.Specialty == Specialty.Tank ? 0f : (index % 2 == 0 ? -0.65f : 0.65f);
            destination = ClampToCurrentRoom(medic.transform.position + forward * Mathf.Min(1.5f, towardThreat.magnitude * 0.6f) + side * flank);
            return true;
        }

        public bool TryTankPosition(PlayerController tank, MouseEnemy target, out Vector3 destination)
        {
            destination = tank.transform.position;
            if (currentRoom == null || currentRoom.cleared) return false;
            Vector3 center = Vector3.zero;
            int count = 0;
            foreach (PlayerController ally in agents)
                if (ally != null && !ally.Dead && ally != tank) { center += ally.transform.position; count++; }
            if (count == 0) return false;
            center /= count;
            Vector3 towardEnemy = target.transform.position - center;
            towardEnemy.y = 0f;
            if (towardEnemy.sqrMagnitude < 0.01f) return false;
            destination = ClampToCurrentRoom(center + towardEnemy.normalized * Mathf.Min(2f, towardEnemy.magnitude * 0.7f));
            return (destination - tank.transform.position).sqrMagnitude > 1f;
        }

        void UpdateSpecialties()
        {
            if (currentRoom == null || Finished) return;
            bool critical = false;
            PlayerController bard = null, locksmith = null, medic = null;
            foreach (PlayerController agent in agents)
            {
                if (agent == null || agent.Dead) continue;
                if (agent.Health <= agent.MaxHealth * 0.25f) critical = true;
                if (agent.Specialty == Specialty.Bard) bard = agent;
                if (agent.Specialty == Specialty.Locksmith) locksmith = agent;
                if (agent.Specialty == Specialty.Medic) medic = agent;
            }
            if (!currentRoom.cleared && critical)
            {
                if (!bardUsed && bard != null)
                {
                    bardUsed = true;
                    foreach (PlayerController agent in agents)
                        if (agent != null && !agent.Dead) { agent.Heal(agent.MaxHealth * 0.3f); agent.SetBardBuff(true); }
                    StartCoroutine(BardSphere(currentRoom));
                }
                if (!locksmithUsed && locksmith != null && previousRoom != null && previousRoomDirection >= 0)
                {
                    locksmithUsed = true;
                    StartCoroutine(EscapeRoom());
                    return;
                }
            }
            if (!currentRoom.cleared || medic == null || medic.Money <= 0) return;
            medicCooldown -= Time.deltaTime;
            if (medicCooldown > 0f) return;
            foreach (PlayerController patient in agents)
            {
                if (patient == null || patient.Dead || patient == medic || patient.Health >= patient.MaxHealth * 0.6f) continue;
                if (medic.TrySpendMoney(1)) { patient.Heal(2f); medic.ChangeKarma(1); medicCooldown = 0.5f; }
                break;
            }
        }

        IEnumerator BardSphere(Room room)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Bard encouragement";
            sphere.transform.SetParent(room.root.transform, false);
            sphere.transform.localPosition = Vector3.up * 1.5f;
            sphere.transform.localScale = Vector3.one * (Mathf.Sqrt(room.width * room.width + room.depthSize * room.depthSize) + 1f);
            Destroy(sphere.GetComponent<Collider>());
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) { Destroy(sphere); yield break; }
            Material material = new Material(shader);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            sphere.GetComponent<Renderer>().material = material;
            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                material.SetColor("_BaseColor", new Color(0.6f, 0.3f, 1f, 0.25f * Mathf.Clamp01(elapsed / 0.5f)));
                yield return null;
            }
            Destroy(sphere);
            Destroy(material);
        }

        IEnumerator EscapeRoom()
        {
            Room abandoned = currentRoom;
            Room destination = previousRoom;
            int direction = previousRoomDirection;
            IsTransitioning = true;
            abandoned.spawning = false;
            abandoned.spawnGeneration++;
            ClearWeaponOrder();
            ClearBardBuff();
            foreach (PlayerController agent in agents)
                if (agent != null && !agent.Dead) { agent.EscapeInvulnerable = true; agent.RoomExit(); }
            RoomDoor exit = abandoned.doors[direction];
            RoomDoor entrance = destination.doors[(direction + 2) % 4];
            if (exit != null) exit.SetOpen(true);
            if (entrance != null) entrance.SetOpen(true);
            yield return new WaitForSeconds(0.55f);
            Vector2Int d = Directions[direction];
            Vector3 forward = new Vector3(d.x, 0f, d.y);
            Vector3 side = new Vector3(-forward.z, 0f, forward.x);
            float halfSize = direction % 2 == 0 ? abandoned.depthSize * 0.5f : abandoned.width * 0.5f;
            Vector3 doorway = abandoned.center + forward * halfSize;
            for (int i = 0; i < agents.Count; i++)
                if (!agents[i].Dead) agents[i].SetDestination(doorway + forward * 2f + side * ((i - 1) * 0.8f));
            float timeout = 5f;
            while (timeout > 0f)
            {
                bool arrived = true;
                foreach (PlayerController agent in agents) if (!agent.Dead && !agent.ReachedDestination) arrived = false;
                if (arrived) break;
                timeout -= Time.deltaTime;
                yield return null;
            }
            for (int i = 0; i < agents.Count; i++)
                if (agents[i] != null && !agents[i].Dead && !agents[i].ReachedDestination)
                    RescueTransitionAgent(agents[i], doorway + forward * 2f + side * ((i - 1) * 0.8f));
            if (exit != null) exit.SetOpen(false);
            if (entrance != null) entrance.SetOpen(false);
            yield return new WaitForSeconds(0.55f);
            foreach (MouseEnemy mouse in abandoned.root.GetComponentsInChildren<MouseEnemy>())
                if (mouse != null) { mouse.gameObject.SetActive(false); Destroy(mouse.gameObject); }
            abandoned.defeatPositions.Clear();
            if (abandoned.vent != null) { Destroy(abandoned.vent.gameObject); abandoned.vent = null; }
            foreach (ItemPickup pickup in abandoned.root.GetComponentsInChildren<ItemPickup>())
                if (pickup != null) Destroy(pickup.gameObject);
            abandoned.enemies = MouseCountForRoom(abandoned);
            abandoned.spawning = abandoned.enemies > 0;
            abandoned.cleared = false;
            abandoned.visited = false;
            abandoned.boss = null;
            abandoned.bossDefeated = false;
            currentRoom = destination;
            previousRoom = null;
            previousRoomDirection = -1;
            FitCameraToRoom(destination);
            for (int i = 0; i < agents.Count; i++)
            {
                PlayerController agent = agents[i];
                if (agent == null) continue;
                agent.EscapeInvulnerable = false;
                agent.CancelMoveOrder();
            }
            IsTransitioning = false;
            ArriveInRoom(destination);
        }
    }
}
