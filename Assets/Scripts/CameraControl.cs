using UnityEngine;

namespace Tanks.Complete
{
    public class CameraControl : MonoBehaviour
    {
        public float m_DampTime = 0.12f;
        public float m_ScreenEdgeBuffer = 4f;
        public float m_MinSize = DuelCameraFraming.MinimumSize;
        public Transform[] m_Targets;
        public bool m_FollowFirstTarget;
        public static CameraControl Instance { get; private set; }

        private Camera view;
        private readonly DuelCameraFraming framing = new DuelCameraFraming();
        private float shakeIntensity;
        private float shakeRemaining;

        private void Awake()
        {
            Instance = this;
            // Unity objects override ==, so '??' can miss a destroyed or missing component.
            view = GetComponentInChildren<Camera>();
            if (view == null) view = Camera.main;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void LateUpdate()
        {
            framing.Update(view, m_Targets, m_FollowFirstTarget, Time.deltaTime);
            if (view == null || shakeRemaining <= 0f || Time.timeScale <= 0f) return;
            shakeRemaining -= Time.deltaTime;
            Vector2 shake = Random.insideUnitCircle * Mathf.Min(shakeIntensity, 0.25f);
            view.transform.position += view.transform.right * shake.x + view.transform.up * shake.y;
        }

        public void SetStartPositionAndSize()
        {
            shakeRemaining = 0f;
            shakeIntensity = 0f;
            framing.Update(view, m_Targets, m_FollowFirstTarget, 0f, true);
        }

        public static void TriggerShake(float intensity = 0.35f, float duration = 0.2f)
        {
            if (Instance == null) return;
            Instance.shakeIntensity = Mathf.Max(Instance.shakeIntensity, intensity);
            Instance.shakeRemaining = Mathf.Max(Instance.shakeRemaining, duration);
        }
    }
}
