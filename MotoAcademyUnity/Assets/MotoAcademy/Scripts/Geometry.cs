using UnityEngine;

namespace MotoAcademy
{
    public static class Geometry
    {
        public static GameObject Part(string name, PrimitiveType shape, Transform parent, Vector3 position, Vector3 scale, Material material, bool collider = false, int layer = 0)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = name; go.layer = layer;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider)
            {
                var c = go.GetComponent<Collider>();
                c.enabled = false; Object.Destroy(c);
            }
            return go;
        }
        public static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material mat, bool collider = false, int layer = 0)
            => Part(name, PrimitiveType.Cube, parent, position, scale, mat, collider, layer);

        public static GameObject Rod(string name, Transform parent, Vector3 a, Vector3 b, float radius, Material mat)
        {
            var go = Part(name, PrimitiveType.Cylinder, parent, (a + b) * .5f, new Vector3(radius * 2, Vector3.Distance(a, b) * .5f, radius * 2), mat);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
            return go;
        }

        public static GameObject MeshObject(string name, Transform parent, Vector3[] vertices, int[] triangles, Material mat, bool collider = false, int layer = 0, Vector2[] uv = null)
        {
            var mesh = new Mesh { name = name };
            mesh.vertices = vertices; mesh.triangles = triangles;
            if (uv != null) mesh.uv = uv;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject(name); go.layer = layer; go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }

        public static GameObject Torus(string name, Transform parent, Vector3 position, float radius, float tube, Material mat)
        {
            const int sides = 36, sections = 10;
            var v = new Vector3[sides * sections]; var tri = new int[sides * sections * 6];
            for (int i = 0; i < sides; i++) for (int j = 0; j < sections; j++)
            {
                float a = i * Mathf.PI * 2 / sides, b = j * Mathf.PI * 2 / sections;
                float r = radius + Mathf.Cos(b) * tube;
                v[i * sections + j] = new Vector3(Mathf.Sin(b) * tube, Mathf.Cos(a) * r, Mathf.Sin(a) * r);
                int p = (i * sections + j) * 6;
                int x = i * sections + j, y = ((i + 1) % sides) * sections + j;
                int z = ((i + 1) % sides) * sections + (j + 1) % sections, w = i * sections + (j + 1) % sections;
                tri[p] = x; tri[p + 1] = y; tri[p + 2] = z; tri[p + 3] = x; tri[p + 4] = z; tri[p + 5] = w;
            }
            var go = MeshObject(name, parent, v, tri, mat); go.transform.localPosition = position; return go;
        }

        public static void Sign(Transform parent, string text, Vector3 position, float size, Material backing, Color color)
        {
            Box("Placa", parent, position, new Vector3(size * 9, size * 2.2f, .15f), backing);
            var go = new GameObject(text); go.transform.SetParent(parent, false); go.transform.localPosition = position + Vector3.back * .09f;
            var label = go.AddComponent<TextMesh>(); label.text = text; label.fontSize = 64; label.characterSize = size * .15f;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); go.GetComponent<Renderer>().sharedMaterial = label.font.material;
            label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center; label.color = color;
        }
    }
}
