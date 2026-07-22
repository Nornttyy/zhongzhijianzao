using UnityEngine;

namespace DoNotOpen.Prototype
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class TopDownPlayer : MonoBehaviour
    {
        public float Speed { get; set; } = 4.6f;
        public Transform FacingMarker { get; set; }

        private Rigidbody2D body;
        private Vector2 movement;
        private Vector2 spawnPosition;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            spawnPosition = transform.position;
        }

        private void Update()
        {
            movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            movement = Vector2.ClampMagnitude(movement, 1f);

            if (movement.sqrMagnitude > 0.01f && FacingMarker != null)
            {
                float angle = Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg;
                FacingMarker.localPosition = (Vector3)(movement.normalized * 0.48f);
                FacingMarker.localRotation = Quaternion.Euler(0f, 0f, angle);
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                body.position = spawnPosition;
                body.linearVelocity = Vector2.zero;
            }
        }

        private void FixedUpdate()
        {
            body.linearVelocity = movement * Speed;
        }

        private void OnDisable()
        {
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }
    }
}
