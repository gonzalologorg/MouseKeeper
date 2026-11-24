using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MouseKeeper
{
    // Receives the car animation's arrival event on the Animator's GameObject.
    public sealed class IntroSequence : MonoBehaviour
    {
        static bool arrivalPending;
        bool started;
        int movingAgents;

        public static bool ConsumeArrival()
        {
            bool pending = arrivalPending;
            arrivalPending = false;
            return pending;
        }

        void Start()
        {
            if (!ExpeditionSession.Initialize()) { enabled = false; return; }
            if (!ExpeditionSession.Active) SceneManager.LoadScene("Planning");
        }

        public void SpawnAgents()
        {
            if (started || !enabled || !ExpeditionSession.Active) return;
            Transform spawn = transform.Find("playerspawn");
            Transform destination = transform.Find("movetarget");
            if (spawn == null || destination == null)
            {
                Debug.LogError("Start car requires playerspawn and movetarget children.", this);
                return;
            }
            started = true;
            StartCoroutine(EnterBuilding(spawn, destination));
        }

        IEnumerator EnterBuilding(Transform spawn, Transform destination)
        {
            float eventTime = Time.time;
            foreach (int selected in ExpeditionSession.Data.selectedAgents)
            {
                AgentDefinition definition = ExpeditionSession.Catalog.agents[selected];
                GameObject agent = new GameObject(definition.profile.name + " (intro)");
                agent.transform.position = spawn.position;
                if (definition.modelPrefab != null)
                    Instantiate(definition.modelPrefab, agent.transform);
                else
                {
                    GameObject model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    model.transform.SetParent(agent.transform, false);
                    model.transform.localPosition = Vector3.up * 0.9f;
                    model.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
                    Destroy(model.GetComponent<Collider>());
                    model.GetComponent<Renderer>().material.color = definition.placeholderColor;
                }
                movingAgents++;
                StartCoroutine(MoveAgent(agent, destination.position, definition.profile.EffectiveSpeed * 0.7f));
                yield return new WaitForSeconds(0.3f);
            }
            // Keep the existing camera animation playing through its last keyframe.
            while (movingAgents > 0 || Time.time - eventTime < 5f - 1.8333334f)
                yield return null;
            arrivalPending = true;
            SceneManager.LoadScene("SampleScene");
        }

        IEnumerator MoveAgent(GameObject agent, Vector3 destination, float speed)
        {
            Vector3 direction = destination - agent.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f) agent.transform.rotation = Quaternion.LookRotation(direction);
            while ((agent.transform.position - destination).sqrMagnitude > 0.001f)
            {
                agent.transform.position = Vector3.MoveTowards(agent.transform.position, destination,
                    Mathf.Max(0.1f, speed) * Time.deltaTime);
                yield return null;
            }
            Destroy(agent);
            movingAgents--;
        }
    }
}
