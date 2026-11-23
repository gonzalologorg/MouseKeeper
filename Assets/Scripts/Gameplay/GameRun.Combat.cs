using UnityEngine;

namespace MouseKeeper
{
    public sealed partial class GameRun
    {
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
            RefreshDoors();
            RefreshMinimap();
        }

        public void RefreshMinimap()
        {
            if (minimap == null) return;
            foreach (Room room in rooms)
            {
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
                minimap.SetRoom(room.position, new Vector2(room.center.x, room.center.z),
                    new Vector2(room.width, room.depthSize), color);
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
            damageIndicators.Show(position, amount, isPlayer);
        }

    }
}
