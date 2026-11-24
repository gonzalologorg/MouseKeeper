using UnityEngine;

namespace MouseKeeper
{
    [CreateAssetMenu(menuName = "MouseKeeper/Enemigos/Preset")]
    public sealed class EnemyPreset : ScriptableObject
    {
        [Tooltip("Prefab completo o modelo visual. Si está vacío se usa un modelo provisional.")]
        public GameObject prefab;
        [Tooltip("Plantilla opcional de controlador y animación para modelos sin MouseEnemy.")]
        public MouseEnemy controllerTemplate;
        public MouseEnemy.MouseVariant behavior;
        [Min(0.01f)] public float maxHealth = 40f;
        [Min(0f)] public float attackDamage = 8f;
        [Min(0.01f)] public float attackInterval = 1f;
        [Min(0f)] public float movementSpeed = 2.4f;
        [Min(0f)] public float explosionDamage = 15f;
        [Min(0f)] public float explosionRadius = 3f;
        [Min(0f)] public float poisonDamagePerSecond = 10f;
        [Min(0f)] public float poisonDuration = 5f;
        [Min(0f)] public float poisonRadius = 3f;

        public void ApplyTo(MouseEnemy enemy)
        {
            enemy.SetBehavior(behavior);
            enemy.maxHealth = Mathf.Max(0.01f, maxHealth);
            enemy.attackDamage = Mathf.Max(0f, attackDamage);
            enemy.attackInterval = Mathf.Max(0.01f, attackInterval);
            enemy.movementSpeed = Mathf.Max(0f, movementSpeed);
            enemy.explosionDamage = Mathf.Max(0f, explosionDamage);
            enemy.explosionRadius = Mathf.Max(0f, explosionRadius);
            enemy.poisonDamagePerSecond = Mathf.Max(0f, poisonDamagePerSecond);
            enemy.poisonDuration = Mathf.Max(0f, poisonDuration);
            enemy.poisonRadius = Mathf.Max(0f, poisonRadius);
            enemy.Preset = this;
        }
    }
}
