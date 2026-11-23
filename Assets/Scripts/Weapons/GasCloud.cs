using System.Collections;
using UnityEngine;

namespace MouseKeeper
{
    public sealed class GasCloud : MonoBehaviour
    {
        PlayerController owner;
        WeaponInstance weapon;
        public void Initialize(PlayerController player, WeaponInstance source, Vector3 landing)
        {
            owner = player;
            weapon = source;
            StartCoroutine(ThrowAndDamage(landing));
        }
        IEnumerator ThrowAndDamage(Vector3 landing)
        {
            Vector3 start = transform.position;
            float elapsed = 0f;
            while (elapsed < 0.4f)
            {
                elapsed = Mathf.Min(0.4f, elapsed + Time.deltaTime);
                float t = elapsed / 0.4f;
                transform.position = Vector3.Lerp(start, landing, t) + Vector3.up * (4f * t * (1f - t));
                yield return null;
            }
            transform.position = landing;
            transform.localScale = new Vector3(3f, 0.25f, 3f);
            for (int tick = 0; tick < 12; tick++)
            {
                yield return new WaitForSeconds(0.25f);
                if (transform.parent == null) break;
                foreach (MouseEnemy mouse in transform.parent.GetComponentsInChildren<MouseEnemy>())
                {
                    Vector3 offset = mouse.transform.position - transform.position;
                    offset.y = 0f;
                    if (offset.sqrMagnitude <= 2.25f) mouse.TakeDamage(5f, owner, weapon, false, sourcePosition: transform.position);
                }
            }
            Destroy(gameObject);
        }
    }
}
