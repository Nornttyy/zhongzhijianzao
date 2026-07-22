using UnityEngine;

namespace DoNotOpen.Prototype
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ThreatPulse : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            float pulse = 0.65f + Mathf.Sin(Time.time * 2.4f) * 0.2f;
            spriteRenderer.color = new Color(0.68f, 0.24f, 0.18f, pulse);
        }
    }
}
