using UnityEngine;
using System.Collections.Generic;

namespace MouseKeeper
{
    public sealed partial class GameRun
    {
        static float HealthDropChance(float healthFraction)
        {
            return healthFraction < 0.25f ? 20f * 2f / 3f : 0f;
        }

        void SpawnRoomRewards(Room room)
        {
            RoomLayout layout = room.root.GetComponentInChildren<RoomLayout>();
            if (layout != null)
            {
                SpawnLayoutRewards(room, layout);
                if (layout.replaceDefaultRewards)
                {
                    room.defeatPositions.Clear();
                    return;
                }
            }
            if (room.defeatPositions.Count == 0) return;
            float roll = Random.Range(0, 1000000) * 0.0001f;
            float lowestHealth = 1f;
            foreach (PlayerController agent in agents)
                if (!agent.Dead) lowestHealth = Mathf.Min(lowestHealth, agent.Health / agent.MaxHealth);
            if (roll < HealthDropChance(lowestHealth))
            {
                Vector3 position = RewardPosition(room.defeatPositions[Random.Range(0, room.defeatPositions.Count)]);
                SpawnVial(healthVialPrefab, position, true, true);
            }
            else
            {
                foreach (Vector3 defeatPosition in room.defeatPositions)
                    if (Random.value < 0.4f)
                        SpawnVial(cashVialPrefab, RewardPosition(defeatPosition), false, true);
            }
            room.defeatPositions.Clear();
        }

        void SpawnLayoutRewards(Room room, RoomLayout layout)
        {
            if (layout.rewards == null) return;
            foreach (RoomLayout.Reward reward in layout.rewards)
            {
                if (reward == null || Random.value >= reward.chance) continue;
                Vector3 world = reward.position != null ? reward.position.position : layout.transform.position;
                Vector3 local = room.root.transform.InverseTransformPoint(world);
                switch (reward.kind)
                {
                    case RoomLayout.RewardKind.Health:
                        SpawnVial(healthVialPrefab, local, true, true);
                        break;
                    case RoomLayout.RewardKind.Money:
                        SpawnVial(cashVialPrefab, local, false, true);
                        break;
                    case RoomLayout.RewardKind.Weapon:
                        if (reward.weapon != null) SpawnWeaponPickup(new WeaponInstance(reward.weapon), world);
                        break;
                    case RoomLayout.RewardKind.Passive:
                        if (reward.passive != null) SpawnPassivePickup(reward.passive, local);
                        break;
                }
            }
        }

        void SpawnBossReward(Vector3 defeatPosition)
        {
            bool injured = false;
            foreach (PlayerController agent in agents)
                if (agent != null && !agent.Dead && agent.Health < agent.MaxHealth * 0.5f)
                    injured = true;
            Vector3 position = RewardPosition(defeatPosition);
            if (injured)
            {
                ItemPickup pickup = SpawnVial(healthVialPrefab, position, true, true);
                if (pickup != null) pickup.SetLowestHealthPriority();
            }
            else
            {
                PassiveItemDefinition passive = RandomDefinition(passiveItems);
                if (passive != null) SpawnPassivePickup(passive, position);
            }
        }

        Vector3 RewardPosition(Vector3 defeatPosition)
        {
            return new Vector3(
                Mathf.Clamp(defeatPosition.x, -currentRoom.width / 2f + 1.5f, currentRoom.width / 2f - 1.5f),
                0.3f,
                Mathf.Clamp(defeatPosition.z, -currentRoom.depthSize / 2f + 1.5f, currentRoom.depthSize / 2f - 1.5f));
        }

        T RandomDefinition<T>(T[] definitions) where T : Object
        {
            int count = 0;
            foreach (T definition in definitions)
                if (definition != null) count++;
            if (count == 0)
            {
                Debug.LogError($"No {typeof(T).Name} is configured for a reward.", this);
                return null;
            }

            int selected = Random.Range(0, count);
            foreach (T definition in definitions)
                if (definition != null && selected-- == 0) return definition;
            return null;
        }

        WeaponDefinition RandomWeapon()
        {
            bool allBasic = agents.Count == ExpeditionSession.SquadSize && ExpeditionSession.Catalog != null;
            WeaponDefinition basic = allBasic ? ExpeditionSession.Catalog.basicWeapon : null;
            foreach (PlayerController agent in agents)
                if (agent.Weapons[0] == null || agent.Weapons[0].Definition != basic) allBasic = false;
            var choices = new List<WeaponDefinition>();
            foreach (WeaponDefinition weapon in weapons)
                if (weapon != null && (!allBasic || weapon != basic)) choices.Add(weapon);
            if (choices.Count == 0)
            {
                Debug.LogWarning("No hay armas disponibles para generar en esta sala.", this);
                return null;
            }
            return choices[Random.Range(0, choices.Count)];
        }

        void PopulateShop()
        {
            Vector3 healthPosition = new Vector3(-2.25f, 0.3f, 0f);
            Vector3 passivePosition = new Vector3(0f, 0.3f, 0f);
            Vector3 weaponPosition = new Vector3(2.25f, 0.3f, 0f);
            SetShopPrice(SpawnVial(healthVialPrefab, healthPosition, true, false), healthPosition, 3);
            PassiveItemDefinition passive = RandomDefinition(passiveItems);
            if (passive != null) SetShopPrice(SpawnPassivePickup(passive, passivePosition), passivePosition, 6);
            WeaponDefinition weapon = RandomWeapon();
            if (weapon != null) SetShopPrice(SpawnWeaponPickup(new WeaponInstance(weapon),
                currentRoom.root.transform.TransformPoint(weaponPosition)), weaponPosition, 9);
            CreateShopLabel(new Vector3(0f, 0f, 2.5f), GameText.Format("Revivir ${0}\nAvaricia < 7", ShopRevivalPrice));
        }

        void PopulateTreasureRoom()
        {
            Vector3 position = new Vector3(0f, 0.3f, 0f);
            if (Random.Range(0, 2) == 0)
            {
                WeaponDefinition weapon = RandomWeapon();
                if (weapon != null) SpawnWeaponPickup(new WeaponInstance(weapon), currentRoom.root.transform.TransformPoint(position));
            }
            else
            {
                PassiveItemDefinition passive = RandomDefinition(passiveItems);
                if (passive != null) SpawnPassivePickup(passive, position);
            }
        }

        internal static int ShopItemPrice(int price, ExpeditionDifficulty difficulty, int floorIndex)
        {
            int difficultyStep = difficulty switch
            {
                ExpeditionDifficulty.Easy => 0,
                ExpeditionDifficulty.Hard => 2,
                ExpeditionDifficulty.Swarm => 3,
                _ => 1
            };
            int percentage = 125 + 10 * (difficultyStep + Mathf.Max(0, floorIndex));
            return (price * percentage + 99) / 100;
        }

        void SetShopPrice(ItemPickup pickup, Vector3 position, int price)
        {
            if (pickup == null) return;
            price = ShopItemPrice(price, ExpeditionSession.Difficulty, levelIndex);
            GameObject label = CreateShopLabel(position, "$" + price);
            pickup.SetPrice(price, label);
        }

        GameObject CreateShopLabel(Vector3 position, string value)
        {
            var label = new GameObject("Shop " + value);
            label.transform.SetParent(currentRoom.root.transform, false);
            label.transform.localPosition = new Vector3(position.x, 0.02f, position.z - 0.9f);
            label.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            TextMesh text = label.AddComponent<TextMesh>();
            text.text = value;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 64;
            text.characterSize = 0.08f;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                Debug.LogError("The built-in font for shop prices is unavailable.", this);
            else
                label.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
            return label;
        }

        ItemPickup SpawnVial(GameObject prefab, Vector3 position, bool isHealth, bool automatic)
        {
            GameObject item;
            if (prefab != null)
            {
                item = Instantiate(prefab, currentRoom.root.transform);
                item.transform.localPosition = new Vector3(position.x, 0f, position.z);
                if ((isHealth && item.GetComponent<HealthVial>() == null) ||
                    (!isHealth && item.GetComponent<MoneyVial>() == null) || item.GetComponent<ItemPickup>() == null)
                {
                    Debug.LogError("Vial prefab is missing its vial or ItemPickup component.", prefab);
                    Destroy(item);
                    return null;
                }
            }
            else
            {
                Debug.LogWarning($"No {(isHealth ? "health" : "cash")} vial prefab configured; using a placeholder.", this);
                item = Block(currentRoom.root.transform, isHealth ? "Health Vial" : "Cash Vial", position,
                    Vector3.one * 0.55f, isHealth ? Color.yellow : Color.green);
                item.GetComponent<Collider>().isTrigger = true;
                if (isHealth) item.AddComponent<HealthVial>();
                else item.AddComponent<MoneyVial>();
                item.AddComponent<ItemPickup>();
            }
            ItemPickup pickup = item.GetComponent<ItemPickup>();
            if (automatic)
            {
                Collider trigger = item.GetComponent<Collider>();
                if (trigger == null || !trigger.enabled || !trigger.isTrigger)
                {
                    Debug.LogError("An automatic vial pickup requires an enabled trigger collider.", item);
                    Destroy(item);
                    return null;
                }
                pickup.SetAutomaticCollection();
            }
            return pickup;
        }

        public ItemPickup SpawnWeaponPickup(WeaponInstance weapon, Vector3 position)
        {
            GameObject item = new GameObject("Weapon Pickup");
            item.transform.SetParent(currentRoom.root.transform, false);
            item.transform.localPosition = currentRoom.root.transform.InverseTransformPoint(position);
            GameObject model = WeaponVisual.CreateModel(weapon.Definition, item.transform);
            Bounds bounds = new Bounds(Vector3.up * 0.5f, Vector3.one);
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
            {
                Bounds world = renderer.bounds;
                for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                        for (int z = -1; z <= 1; z += 2)
                            bounds.Encapsulate(item.transform.InverseTransformPoint(world.center +
                                Vector3.Scale(world.extents, new Vector3(x, y, z))));
            }
            BoxCollider clickTarget = item.AddComponent<BoxCollider>();
            clickTarget.isTrigger = true;
            clickTarget.center = bounds.center;
            clickTarget.size = bounds.size + Vector3.one * 0.2f;
            ItemPickup pickup = item.AddComponent<ItemPickup>();
            pickup.weapon = weapon;
            RefreshMinimap();
            return pickup;
        }

        ItemPickup SpawnPassivePickup(PassiveItemDefinition passive, Vector3 position)
        {
            GameObject item = Block(currentRoom.root.transform, "Passive Pickup", position,
                new Vector3(0.55f, 0.55f, 0.55f), Color.cyan);
            Destroy(item.GetComponent<Collider>());
            ItemPickup pickup = item.AddComponent<ItemPickup>();
            pickup.passive = passive;
            return pickup;
        }

    }
}
