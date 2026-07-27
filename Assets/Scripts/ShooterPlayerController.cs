using UnityEngine;

namespace DoNotOpen.Prototype
{
    public sealed class ShooterPlayerController : MonoBehaviour
    {
        private const float MoveSpeed = 4.2f;
        private const int WalkFrameStart = 3;
        private const int WalkFrameCount = 5;
        private const float IdleBlinkDelay = 2.6f;
        private const float IdleBlinkFrameDuration = 0.14f;
        private Rigidbody2D body;
        private SpriteRenderer renderer;
        private Sprite[] frames;
        private Vector2 movement;
        private float animationTimer;
        private float idleTimer;
        private Vector3 visualBaseLocalPosition;
        private int frameIndex;
        private bool inputLocked;

        public Vector2 Movement { get { return movement; } }

        public void Initialize(Rigidbody2D playerBody, SpriteRenderer playerRenderer, Sprite[] playerFrames)
        {
            body = playerBody;
            renderer = playerRenderer;
            frames = playerFrames;
            visualBaseLocalPosition = renderer != null ? renderer.transform.localPosition : Vector3.zero;
        }

        public void SetInputLocked(string locked)
        {
            inputLocked = string.Equals(locked, "true", System.StringComparison.OrdinalIgnoreCase);
            movement = Vector2.zero;
        }

        private void Update()
        {
            if (inputLocked)
            {
                movement = Vector2.zero;
                idleTimer = 0f;
                frameIndex = 0;
                if (renderer != null && frames != null && frames.Length > 0)
                {
                    renderer.sprite = frames[0];
                }
                return;
            }

            movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            movement = Vector2.ClampMagnitude(movement, 1f);

            // 保留上一次朝向；左右移动时翻转整张角色图，保证向左/向右都面向移动方向。
            if (renderer != null && Mathf.Abs(movement.x) > 0.01f)
            {
                renderer.flipX = movement.x > 0f;
            }

            if (movement.sqrMagnitude < 0.01f || frames == null || frames.Length < WalkFrameStart + WalkFrameCount)
            {
                frameIndex = 0;
                animationTimer = 0f;
                if (renderer != null && frames != null && frames.Length >= 3)
                {
                    idleTimer += Time.deltaTime;
                    if (idleTimer < IdleBlinkDelay)
                    {
                        renderer.sprite = frames[0];
                    }
                    else if (idleTimer < IdleBlinkDelay + IdleBlinkFrameDuration)
                    {
                        renderer.sprite = frames[1];
                    }
                    else if (idleTimer < IdleBlinkDelay + (IdleBlinkFrameDuration * 2f))
                    {
                        renderer.sprite = frames[2];
                    }
                    else
                    {
                        idleTimer = 0f;
                        renderer.sprite = frames[0];
                    }
                }
                return;
            }

            idleTimer = 0f;
            if (frameIndex < WalkFrameStart || frameIndex >= frames.Length)
            {
                frameIndex = WalkFrameStart;
                renderer.sprite = frames[frameIndex];
            }

            animationTimer += Time.deltaTime;
            if (animationTimer >= 0.12f)
            {
                animationTimer = 0f;
                frameIndex = WalkFrameStart + ((frameIndex - WalkFrameStart + 1) % WalkFrameCount);
                renderer.sprite = frames[frameIndex];
            }
        }

        private void FixedUpdate()
        {
            if (body == null)
            {
                return;
            }

            body.MovePosition(body.position + movement * (MoveSpeed * Time.fixedDeltaTime));
        }

        private void LateUpdate()
        {
            if (renderer == null)
            {
                return;
            }

            Vector3 visualPosition = visualBaseLocalPosition;
            if (!inputLocked && movement.sqrMagnitude < 0.01f)
            {
                // 待机时只做轻微上下呼吸，不移动碰撞体本身。
                visualPosition.y += Mathf.Sin(Time.time * 2.4f) * 0.025f;
            }

            renderer.transform.localPosition = visualPosition;
        }
    }
}
