using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MouseKeeper
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        public float baseMovementSpeed = 5f;
        public float baseMaxHealth = 100f;
        public float baseEnergyRecharge = 1f;
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public float MovementSpeed { get; private set; }
        public float EnergyRecharge { get; private set; }
        public int Money { get; private set; }
        public int SelectedWeapon { get; private set; }
        public IReadOnlyList<PassiveItemDefinition> Passives => passives;
        public WeaponInstance[] Weapons { get; } = new WeaponInstance[2];
        public bool Dead { get; private set; }
        public Vector3 AimDirection { get; private set; } = Vector3.forward;

        readonly List<PassiveItemDefinition> passives = new List<PassiveItemDefinition>();
        CharacterController controller;
        Camera viewCamera;
        GameRun run;

        public void Initialize(GameRun gameRun, Camera camera)
        {
            run = gameRun;
            viewCamera = camera;
            controller = GetComponent<CharacterController>();
            MovementSpeed = baseMovementSpeed;
            MaxHealth = baseMaxHealth;
            EnergyRecharge = baseEnergyRecharge;
            Health = MaxHealth;
        }

        void Update()
        {
            if (Dead || run == null || run.Finished) return;
            float dt = Time.deltaTime;
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard == null) return;

            Vector2 input = Vector2.zero;
            if (keyboard.wKey.isPressed) input.y++;
            if (keyboard.sKey.isPressed) input.y--;
            if (keyboard.dKey.isPressed) input.x++;
            if (keyboard.aKey.isPressed) input.x--;
            input = Vector2.ClampMagnitude(input, 1f);
            Vector3 forward = Vector3.Scale(viewCamera.transform.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 right = Vector3.Scale(viewCamera.transform.right, new Vector3(1, 0, 1)).normalized;
            controller.Move((forward * input.y + right * input.x) * MovementSpeed * dt + Vector3.down * 2f * dt);

            if (mouse != null)
            {
                Ray ray = viewCamera.ScreenPointToRay(mouse.position.ReadValue());
                Plane ground = new Plane(Vector3.up, transform.position);
                if (ground.Raycast(ray, out float distance))
                {
                    Vector3 direction = ray.GetPoint(distance) - transform.position;
                    direction.y = 0f;
                    if (direction.sqrMagnitude > 0.01f)
                    {
                        AimDirection = direction.normalized;
                        transform.rotation = Quaternion.LookRotation(AimDirection);
                    }
                }
            }

            if (keyboard.digit1Key.wasPressedThisFrame) SelectedWeapon = 0;
            if (keyboard.digit2Key.wasPressedThisFrame) SelectedWeapon = 1;
            foreach (WeaponInstance weapon in Weapons)
                if (weapon != null) weapon.Tick(dt, EnergyRecharge);
            WeaponInstance current = Weapons[SelectedWeapon];
            if (current != null)
            {
                current.Definition.Think(this, current, dt);
                if (mouse != null && (current.Definition.HoldPrimaryAttack
                    ? mouse.leftButton.isPressed : mouse.leftButton.wasPressedThisFrame))
                    current.Definition.PrimaryAttack(this, current);
                if (mouse != null && mouse.rightButton.isPressed) current.Definition.SecondaryAttack(this, current);
                if (keyboard.rKey.wasPressedThisFrame) current.Definition.Reload(this, current);
            }
            if (keyboard.eKey.wasPressedThisFrame && !run.TryUseVent()) PickUpNearest();
        }

        void PickUpNearest()
        {
            ItemPickup nearest = null;
            float best = 2.25f;
            foreach (ItemPickup pickup in FindObjectsByType<ItemPickup>(FindObjectsSortMode.None))
            {
                if (!pickup.CanPickUp(this)) continue;
                float distance = (pickup.transform.position - transform.position).sqrMagnitude;
                if (distance < best) { best = distance; nearest = pickup; }
            }
            if (nearest != null) nearest.PickUp(this);
        }

        public void Equip(WeaponInstance weapon)
        {
            int slot = Weapons[0] == null ? 0 : Weapons[1] == null ? 1 : SelectedWeapon;
            if (Weapons[slot] != null)
            {
                WeaponInstance dropped = Weapons[slot];
                run.SpawnWeaponPickup(dropped, transform.position + AimDirection * 1.2f);
                dropped.Definition.OnDrop(this, dropped);
            }
            Weapons[slot] = weapon;
            SelectedWeapon = slot;
            weapon.Definition.OnPickup(this, weapon);
        }

        public void AddPassive(PassiveItemDefinition passive)
        {
            passives.Add(passive);
            MovementSpeed += passive.movementSpeedBonus;
            MaxHealth += passive.maxHealthBonus;
            Health = Mathf.Min(MaxHealth, Health + Mathf.Max(0f, passive.maxHealthBonus));
            EnergyRecharge += passive.energyRechargeBonus;
            passive.OnPickUp(this);
        }

        public void Fire(float damage, float range, WeaponInstance weapon = null)
        {
            Fire(damage, range, AimDirection, weapon);
        }

        public void Fire(float damage, float range, Vector3 direction, WeaponInstance weapon)
        {
            Vector3 origin = transform.position + Vector3.up * 0.65f + AimDirection * 0.6f;
            if (Physics.Raycast(origin, direction, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Ignore))
            {
                MouseEnemy enemy = hit.collider.GetComponentInParent<MouseEnemy>();
                if (enemy != null) enemy.TakeDamage(damage, this, weapon);
            }
        }

        public void TakeDamage(float amount)
        {
            if (Dead || amount <= 0f) return;
            Health = Mathf.Max(0f, Health - amount);
            Vector3 indicatorPosition = transform.position;
            indicatorPosition.y = controller.bounds.max.y + 0.2f;
            run.ShowDamage(indicatorPosition, amount, true);
            foreach (PassiveItemDefinition passive in passives) passive.OnTakeDamage(this, amount);
            if (Health > 0f) return;
            foreach (PassiveItemDefinition passive in passives.ToArray())
            {
                if (!passives.Contains(passive)) continue;
                passive.OnDeath(this);
                if (Health > 0f) return;
            }
            Dead = true;
            run.EndRun();
        }

        public bool ConsumePassiveAndHeal(PassiveItemDefinition passive, float amount)
        {
            if (Dead || Health > 0f || amount <= 0f || MaxHealth - passive.maxHealthBonus <= 0f ||
                !passives.Remove(passive)) return false;
            MovementSpeed -= passive.movementSpeedBonus;
            MaxHealth -= passive.maxHealthBonus;
            EnergyRecharge -= passive.energyRechargeBonus;
            Health = Mathf.Min(MaxHealth, amount);
            return true;
        }

        public void Heal(float amount)
        {
            if (amount <= 0f)
            {
                Debug.LogError("Healing amount must be positive.", this);
                return;
            }
            if (Dead) return;
            Health = Mathf.Min(MaxHealth, Health + amount);
        }

        public void AddMoney(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogError("Money amount must be positive.", this);
                return;
            }
            Money += amount;
        }

        public bool TrySpendMoney(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogError("Purchase amount must be positive.", this);
                return false;
            }
            if (Money < amount) return false;
            Money -= amount;
            return true;
        }

        public void ShowPurchaseFailure(int price)
        {
            run.ShowMessage($"Not enough money (${price} required).");
        }

        public void DamageMiceInRoom(float amount)
        {
            run.DamageMiceInCurrentRoom(amount, this);
        }

        public Transform CurrentRoom => run.CurrentRoom;

        public void NotifyItemPickupChanged()
        {
            run.RefreshMinimap();
        }

        public void NotifyMouseKill(MouseEnemy mouse)
        {
            foreach (PassiveItemDefinition passive in passives) passive.OnMouseKill(this, mouse);
        }

        public void RoomEnter()
        {
            foreach (PassiveItemDefinition passive in passives) passive.OnRoomEnter(this);
        }

        public void RoomExit()
        {
            foreach (PassiveItemDefinition passive in passives) passive.OnRoomExit(this);
        }
    }
}
