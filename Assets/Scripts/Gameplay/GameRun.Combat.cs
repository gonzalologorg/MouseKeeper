using System.Collections;
using UnityEngine;

namespace MouseKeeper
{
    public sealed partial class GameRun
    {
        public void SplitMouse(MouseEnemy parent)
        {
            if (currentRoom == null || parent == null || parent.Preset == null || parent.SplitGeneration >= 3) return;
            Room room = currentRoom;
            Vector3 origin = parent.transform.position;
            int offspringCount = room == bossRoom ? BossOffspringCount(room) : 2;
            for (int i = 0; i < offspringCount; i++)
            {
                Vector3 position = origin + Vector3.right * (i == 0 ? -0.45f : 0.45f) * parent.transform.localScale.x;
                position.x = Mathf.Clamp(position.x, room.center.x - room.width * 0.5f + 0.6f, room.center.x + room.width * 0.5f - 0.6f);
                position.z = Mathf.Clamp(position.z, room.center.z - room.depthSize * 0.5f + 0.6f, room.center.z + room.depthSize * 0.5f - 0.6f);
                if (SpawnConfiguredMouse(room, origin, false, parent.Preset, position, parent)) room.enemies++;
            }
        }

        public void SpawnPoisonCloud(Vector3 origin, float radius, float damagePerSecond, float duration)
        {
            if (currentRoom == null || radius <= 0f || damagePerSecond <= 0f || duration <= 0f) return;
            var cloud = new GameObject("Poison Cloud");
            cloud.transform.SetParent(currentRoom.root.transform, true);
            cloud.transform.position = origin + Vector3.up * 0.4f;
            var particles = cloud.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = true;
            main.startLifetime = 0.8f;
            main.startSpeed = 0.3f;
            main.startSize = 1.2f;
            main.startColor = new Color(0.3f, 0.9f, 0.12f, 0.6f);
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            var emission = particles.emission;
            emission.rateOverTime = 35f;
            particles.Play();
            StartCoroutine(PoisonAgents(currentRoom, cloud, origin, radius, damagePerSecond, duration));
        }

        IEnumerator PoisonAgents(Room room, GameObject cloud, Vector3 origin, float radius, float damagePerSecond, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration && currentRoom == room && !Finished)
            {
                float interval = Mathf.Min(1f, duration - elapsed);
                yield return new WaitForSeconds(interval);
                if (currentRoom != room || Finished || cloud == null) break;
                elapsed += interval;
                foreach (PlayerController agent in agents.ToArray())
                {
                    if (agent == null || agent.Dead) continue;
                    Vector3 offset = agent.transform.position - origin;
                    offset.y = 0f;
                    if (offset.sqrMagnitude <= radius * radius)
                        agent.TakeDamage(damagePerSecond * interval, sourcePosition: origin);
                }
            }
            if (cloud != null) Destroy(cloud);
        }

        public void ExplodeMouse(MouseEnemy mouse, float radius, float damage)
        {
            if (mouse == null || radius <= 0f || damage <= 0f) return;
            Vector3 origin = mouse.transform.position;
            var effect = new GameObject("Rat Explosion");
            effect.transform.position = origin + Vector3.up * 0.5f;
            var particles = effect.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false;
            main.duration = 0.3f;
            main.startLifetime = 0.4f;
            main.startSpeed = radius * 2f;
            main.startSize = 0.35f;
            main.startColor = new Color(1f, 0.45f, 0.08f);
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });
            particles.Play();
            Destroy(effect, 1f);
            foreach (PlayerController agent in agents.ToArray())
            {
                if (agent == null || agent.Dead) continue;
                Vector3 offset = agent.transform.position - origin;
                offset.y = 0f;
                if (offset.sqrMagnitude <= radius * radius)
                    agent.TakeDamage(damage, mouse, sourcePosition: origin);
            }
        }

        public void MouseDefeated(MouseEnemy mouse)
        {
            ShakeCameraOnDeath();
            if (focusedMouse == mouse) focusedMouse = null;
            if (fallbackFocus == mouse) fallbackFocus = null;
            currentRoom.defeatPositions.Add(currentRoom.root.transform.InverseTransformPoint(mouse.transform.position));
            currentRoom.enemies = Mathf.Max(0, currentRoom.enemies - 1);
            if (mouse == currentRoom.boss)
            {
                currentRoom.bossDefeated = true;
                BossDialogue();
                if (levelIndex < ExpeditionSession.FloorCount - 1)
                {
                    SpawnBossReward(currentRoom.root.transform.InverseTransformPoint(mouse.transform.position));
                    SpawnVent(currentRoom);
                }
                UpdateDoors(currentRoom);
            }
            if (currentRoom.enemies != 0 || currentRoom.spawning) return;
            CompleteCombatRoom(currentRoom);
        }

        void CompleteCombatRoom(Room room)
        {
            if (room.cleared) return;
            room.cleared = true;
            ClearBardBuff();
            SpawnRoomRewards(room);
            PlayerController weakest = null;
            foreach (PlayerController agent in agents)
                if (agent != null && !agent.Dead &&
                    (weakest == null || agent.Health / agent.MaxHealth < weakest.Health / weakest.MaxHealth))
                    weakest = agent;
            if (weakest != null && weakest.Health < weakest.MaxHealth) weakest.Heal(weakest.MaxHealth * 0.05f);
            foreach (PlayerController agent in agents)
                if (agent != null && !agent.Dead && agent.Dialogue != null)
                    agent.Dialogue.Speak(DialogueEvent.RoomCleared, 0.2f);
            clearedCombatRooms++;
            RefreshRoomLighting();
            RefreshDoors();
            RefreshMinimap();
        }

        public void RefreshMinimap()
        {
            if (minimap == null) return;
            foreach (Room room in rooms)
            {
                if (room == bossRoom && !BossRevealed()) continue;
                bool visible = room.visited;
                if (!visible)
                    foreach (Room neighbour in room.neighbours)
                        if (neighbour != null && neighbour.visited) { visible = true; break; }
                if (!visible) continue;

                PickupKind kind = PickupKind.None;
                if (room.visited && room.root != null)
                    foreach (ItemPickup pickup in room.root.GetComponentsInChildren<ItemPickup>(true))
                        if (pickup.Available && (int)pickup.Kind > (int)kind) kind = pickup.Kind;

                Color color = room == bossRoom ? Color.red :
                    kind == PickupKind.Weapon ? new Color(0.6f, 0.2f, 0.8f) :
                    kind == PickupKind.Passive ? Color.cyan : kind == PickupKind.Health ? Color.yellow :
                    kind == PickupKind.Money ? Color.green : room == currentRoom ? Color.white :
                    room.cleared ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.25f, 0.25f, 0.25f);
                Vector2 center = new Vector2(room.center.x, room.center.z);
                Vector2 size = new Vector2(room.width, room.depthSize);
                if (room.kind == RoomKind.Corridor)
                {
                    int mask = 0;
                    for (int i = 0; i < 4; i++)
                        if (room.neighbours[i] != null && (room.neighbours[i] != bossRoom || BossRevealed())) mask |= 1 << i;
                    minimap.SetCorridor(room.position, center, size, mask, color);
                }
                else minimap.SetRoom(room.position, center, size, color);
            }
            minimap.LayoutRooms();
        }

        public void DamageMiceInCurrentRoom(float amount, PlayerController attacker, WeaponInstance weapon = null)
        {
            if (currentRoom == null || currentRoom.root == null || amount <= 0f) return;
            foreach (MouseEnemy mouse in currentRoom.root.GetComponentsInChildren<MouseEnemy>())
                mouse.TakeDamage(amount, attacker, weapon);
        }

        public void ShowDamage(Vector3 position, float amount, bool isPlayer)
        {
            if (isPlayer && hexBackground != null) hexBackground.Flash();
            damageIndicators.Show(position, amount, isPlayer);
        }

    }
}
