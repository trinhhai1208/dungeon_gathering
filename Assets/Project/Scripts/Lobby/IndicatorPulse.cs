using UnityEngine;

namespace DungeonGathering.Lobby
{
    /// <summary>
    /// Hiệu ứng nhấp nhô (bobbing) và chớp sáng (pulsing) cho mũi tên chỉ định nhân vật đang chọn ở sảnh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class IndicatorPulse : MonoBehaviour
    {
        [SerializeField] float bobFrequency = 6f;
        [SerializeField] float bobAmplitude = 0.10f;
        [SerializeField] float pulseSpeed = 2.5f;
        [SerializeField] float minAlpha = 0.65f;

        SpriteRenderer sr;
        Vector3 initialLocalPos;
        Color initialColor = Color.white;
        bool initialized;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            initialLocalPos = transform.localPosition;
            if (sr != null) initialColor = sr.color;
            initialized = true;
        }

        void OnEnable()
        {
            if (initialized)
            {
                transform.localPosition = initialLocalPos;
                if (sr != null) sr.color = initialColor;
            }
        }

        void Update()
        {
            float t = Time.unscaledTime;
            float y = Mathf.Sin(t * bobFrequency) * bobAmplitude;
            transform.localPosition = initialLocalPos + new Vector3(0f, y, 0f);

            if (sr != null)
            {
                float a = Mathf.Lerp(minAlpha, 1f, (Mathf.Sin(t * pulseSpeed * Mathf.PI) + 1f) * 0.5f);
                var c = initialColor;
                c.a *= a;
                sr.color = c;
            }
        }

        void OnDisable()
        {
            if (initialized)
            {
                transform.localPosition = initialLocalPos;
                if (sr != null) sr.color = initialColor;
            }
        }
    }
}
