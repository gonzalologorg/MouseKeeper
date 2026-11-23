using UnityEngine;

namespace MouseKeeper
{
    public sealed partial class GameRun
    {
        bool returnToPlanningAfterDefeat;
        int ClearedFloors => Mathf.Min(ExpeditionSession.FloorCount, fullyClearedFloors + (FloorCleared() ? 1 : 0));

        int ClearedRoomCount()
        {
            int count = 0;
            foreach (Room room in rooms)
                if (room.visited && room.cleared && !room.spawning) count++;
            return count;
        }

        string FloorProgress()
        {
            int clearedRooms = ClearedRoomCount();
            int roomPercent = rooms.Count > 0 ? Mathf.RoundToInt(100f * clearedRooms / rooms.Count) : 0;
            int floorPercent = Mathf.RoundToInt(100f * ClearedFloors / ExpeditionSession.FloorCount);
            return GameText.Format("Piso {0}/{1} | Limpieza: {2}/{3} ({4}%) | Pisos completos: {5}/{1} ({6}%)",
                levelIndex + 1, ExpeditionSession.FloorCount, clearedRooms, rooms.Count, roomPercent, ClearedFloors, floorPercent);
        }

        public void EndRun()
        {
            if (Finished) return;
            Finished = true;
            returnToPlanningAfterDefeat = ExpeditionSession.Active;
            if (ExpeditionSession.Active)
                ExpeditionSession.Settle(completedFloorCredit + (FloorCleared() ? 1f : 0f), agents.ToArray(), false);
        }

        public void ShowMessage(string text)
        {
            message = GameText.Get(text);
            messageUntil = Time.time + 2f;
        }

        static Rect FormationButtonsRect() => new Rect(Screen.width - 255f, Screen.height - 75f, 240f, 64f);

        static Rect FormationButtonRect(int index)
        {
            Rect panel = FormationButtonsRect();
            return new Rect(panel.x + index * 80f, panel.y, 78f, panel.height);
        }

        void DrawAtlasIcon(Rect rect, int index)
        {
            if (hudIcons != null)
                GUI.DrawTextureWithTexCoords(rect, hudIcons, new Rect(index / 8f, 0f, 1f / 8f, 1f));
        }

        static void DrawSpriteIcon(Rect rect, Sprite icon)
        {
            Rect source = icon.textureRect;
            Texture2D texture = icon.texture;
            GUI.DrawTextureWithTexCoords(rect, texture,
                new Rect(source.x / texture.width, source.y / texture.height,
                    source.width / texture.width, source.height / texture.height));
        }

        void DrawPassiveIcons(PlayerController agent, float y)
        {
            float start = Mathf.Clamp((Screen.width - 270f) * 0.6f, 260f, 650f);
            int capacity = Mathf.Max(0, Mathf.FloorToInt((FormationButtonsRect().x - start - 5f) / 22f));
            int shown = Mathf.Min(agent.Passives.Count, capacity);
            if (agent.Passives.Count > capacity && capacity > 0) shown--;
            for (int i = 0; i < shown; i++)
            {
                PassiveItemDefinition passive = agent.Passives[i];
                if (passive == null) continue;
                Rect rect = new Rect(start + i * 22f, y, 20f, 20f);
                Sprite icon = passive.DisplayIcon;
                if (icon != null)
                {
                    DrawSpriteIcon(rect, icon);
                }
                else
                {
                    int index = passive is RoomDamagePassiveItemDefinition ? 1 :
                        passive is RevivePassiveItemDefinition ? 2 :
                        passive is RetaliationPassiveItemDefinition ? 3 :
                        passive is StatPassiveItemDefinition ? 0 : 4;
                    DrawAtlasIcon(rect, index);
                }
                GUI.Label(rect, new GUIContent("", GameText.Get(passive.displayName)));
            }
            if (agent.Passives.Count > shown && capacity > 0)
                GUI.Label(new Rect(start + shown * 22f, y, 40f, 20f), "+" + (agent.Passives.Count - shown));
        }

        void DrawFormationButtons()
        {
            string[] labels = { "Objetivo", "Distancia", "Cercano" };
            string[] descriptions = { "1: concentrar fuego", "2: mantener distancia", "3: enemigo más cercano" };
            Color previous = GUI.backgroundColor;
            for (int i = 0; i < 3; i++)
            {
                Rect button = FormationButtonRect(i);
                GUI.backgroundColor = (int)Formation == i ? new Color(1f, 0.78f, 0.32f) : previous;
                GUI.Box(button, new GUIContent("", GameText.Get(descriptions[i])), GUI.skin.button);
                DrawAtlasIcon(new Rect(button.x + 23f, button.y + 3f, 32f, 32f), 5 + i);
                GUI.Label(new Rect(button.x + 5f, button.y + 37f, 72f, 22f), $"{i + 1} {GameText.Get(labels[i])}");
            }
            GUI.backgroundColor = previous;
        }

        GUIStyle agentHudLabel;

        void OnGUI()
        {
            if (player == null) return;
            agentHudLabel ??= new GUIStyle(GUI.skin.label) { richText = true };
#if UNITY_EDITOR
            GUI.Label(new Rect(15f, 15f, 1100f, 24f),
                "EDITOR: F1 curar | F2 karma +5 | F3 karma -5 | F4 eliminar ratas | F5 reiniciar piso | F6 caer agente");
#endif
            GUI.Label(new Rect(15f, Screen.height - 110f, 800f, 25f),
                GameText.Get("Clic derecho: mover / elegir rata, puerta, escotilla o arma"));
            for (int i = 0; i < agents.Count; i++)
            {
                PlayerController agent = agents[i];
                WeaponInstance weapon = agent.Weapons[agent.SelectedWeapon];
                string weaponText = weapon != null ? GameText.Get(weapon.Definition.displayName) : GameText.Get("Sin arma");
                string nameColor = ColorUtility.ToHtmlStringRGB(agent.Profile.LevelColor);
                string name = $"<color=#{nameColor}>{agent.Profile.NameWithLevel(10)}</color>";
                GUI.Label(new Rect(15f, Screen.height - 85f + i * 20f, 600f, 22f),
                    $"{name}: {(agent.Dead ? GameText.Format("caído (tienda ${0} o siguiente piso)", ShopRevivalPrice) : $"{agent.Health:0}/{agent.MaxHealth:0}")} | ${agent.Money} | Karma {agent.Karma:+0;-0;0} | {weaponText}", agentHudLabel);
                DrawPassiveIcons(agent, Screen.height - 85f + i * 20f);
            }
            DrawFormationButtons();
            GUI.Label(new Rect(15f, Screen.height - 25f, 650f, 22f), FloorProgress());
            if (currentRoom != null && !currentRoom.cleared)
                GUI.Label(new Rect(15f, Screen.height - 135f, 250f, 25f), GameText.Format("Plagas restantes: {0}", currentRoom.enemies));
            if (bossRoom != null && bossRoom.bossDefeated && !FloorCleared() && rooms.Count > 0)
                GUI.Label(new Rect(15f, Screen.height - 155f, 600f, 22f),
                    GameText.Format("Salida anticipada: -{0}% del contrato",
                        (100f * ClearedRoomCount() / rooms.Count / ExpeditionSession.FloorCount).ToString("0.#")));
            foreach (PlayerController agent in agents)
            {
                if (agent.Dead) continue;
                ItemPickup nearby = agent.NearbyPickup();
                if (nearby == null || string.IsNullOrEmpty(nearby.DisplayName)) continue;
                Vector3 screen = viewCamera.WorldToScreenPoint(nearby.transform.position + Vector3.up);
                if (screen.z > 0f)
                {
                    Sprite icon = nearby.DisplayIcon;
                    if (icon != null) DrawSpriteIcon(new Rect(screen.x - 100f, Screen.height - screen.y, 22f, 22f), icon);
                    GUI.Label(new Rect(screen.x - (icon != null ? 74f : 100f), Screen.height - screen.y, 200f, 22f), nearby.DisplayName);
                }
            }
            if (!string.IsNullOrEmpty(message) && Time.time < messageUntil)
                GUI.Label(new Rect(Screen.width / 2f - 140f, Screen.height - 95f, 280f, 25f), message);
            if (Finished)
            {
                var style = new GUIStyle(GUI.skin.box) { fontSize = 20, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                style.normal.textColor = Color.white;
                GUI.Box(new Rect(Screen.width / 2f - 300f, Screen.height / 2f - 55f, 600f, 110f),
                    GameText.Get("Todos los agentes han padecido,\npresiona Enter para continuar"), style);
            }
            if (!string.IsNullOrEmpty(GUI.tooltip))
                GUI.Box(new Rect(Event.current.mousePosition.x + 12f, Event.current.mousePosition.y - 28f, 180f, 24f), GUI.tooltip);
        }
    }
}
