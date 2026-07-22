using UnityEngine;

namespace DoNotOpen.Prototype
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class VisionMask : MonoBehaviour
    {
        private const int RayCount = 360;
        private const float OuterRadius = 40f;
        private const float WallRevealPadding = 0.12f;

        public float Radius { get; set; } = 6f;
        public int ObstacleMask { get; set; }

        private Mesh mesh;
        private Vector3[] vertices;
        private int[] triangles;
        private Material material;

        private void Awake()
        {
            mesh = new Mesh { name = "Runtime Vision Mask" };
            mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = mesh;

            Shader shader = Shader.Find("Sprites/Default");
            material = new Material(shader)
            {
                name = "Runtime Darkness",
                color = new Color(0.01f, 0.012f, 0.02f, 0.975f),
                renderQueue = 4000
            };

            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.sortingOrder = 100;

            vertices = new Vector3[(RayCount + 1) * 2];
            triangles = new int[RayCount * 6];

            for (int i = 0; i < RayCount; i++)
            {
                int vertex = i * 2;
                int triangle = i * 6;
                triangles[triangle] = vertex;
                triangles[triangle + 1] = vertex + 1;
                triangles[triangle + 2] = vertex + 3;
                triangles[triangle + 3] = vertex;
                triangles[triangle + 4] = vertex + 3;
                triangles[triangle + 5] = vertex + 2;
            }
        }

        private void LateUpdate()
        {
            Vector2 origin = transform.position;

            for (int i = 0; i <= RayCount; i++)
            {
                float angle = i * Mathf.PI * 2f / RayCount;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                RaycastHit2D hit = Physics2D.Raycast(origin, direction, Radius, ObstacleMask);
                float distance = hit.collider == null
                    ? Radius
                    : Mathf.Min(Radius, hit.distance + WallRevealPadding);

                int vertex = i * 2;
                vertices[vertex] = direction * distance;
                vertices[vertex + 1] = direction * OuterRadius;
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
        }

        private void OnDestroy()
        {
            if (mesh != null)
            {
                Destroy(mesh);
            }

            if (material != null)
            {
                Destroy(material);
            }
        }
    }
}
