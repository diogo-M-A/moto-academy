using UnityEngine;

namespace MotoAcademy
{
    public static class BikeVisual
    {
        public static void Build(MotorcycleController bike, Material paint, Material dark, Material metal, Material rubber, Material glow)
        {
            var root = new GameObject("Motocicleta • 160 Street").transform; root.SetParent(bike.transform, false); bike.visual = root;
            Geometry.Rod("Chassi inferior", root, new Vector3(0, -.26f, -.5f), new Vector3(0, -.12f, .45f), .045f, dark);
            Geometry.Rod("Chassi superior", root, new Vector3(0, .22f, -.4f), new Vector3(0, .15f, .43f), .04f, metal);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * .13f;
                Geometry.Rod("Quadro tubular", root, new Vector3(x, -.4f, -.25f), new Vector3(x, .2f, .38f), .025f, metal);
                Geometry.Rod("Balança", root, new Vector3(x, -.28f, -.1f), new Vector3(x, -.49f, -.67f), .035f, dark);
                Geometry.Rod("Suspensão traseira", root, new Vector3(x, .06f, -.36f), new Vector3(x, -.43f, -.6f), .032f, metal);
                Geometry.Rod("Mola", root, new Vector3(x, -.02f, -.40f), new Vector3(x, -.26f, -.53f), .05f, paint);
                Geometry.Rod("Garfo invertido", root, new Vector3(x, .27f, .43f), new Vector3(x, -.49f, .67f), .032f, metal);
                Geometry.Rod("Bengala", root, new Vector3(x, -.14f, .57f), new Vector3(x, -.49f, .67f), .044f, dark);
                Geometry.Rod("Pedaleira", root, new Vector3(0, -.25f, -.14f), new Vector3(side * .28f, -.25f, -.14f), .025f, metal);
            }
            Geometry.Part("Bloco do motor", PrimitiveType.Cylinder, root, new Vector3(0, -.19f, -.05f), new Vector3(.35f, .14f, .35f), dark).transform.localRotation = Quaternion.Euler(0, 0, 90);
            for (int i = 0; i < 7; i++) Geometry.Box("Aleta do cilindro", root, new Vector3(0, -.08f + i * .026f, .16f), new Vector3(.29f, .014f, .28f), metal);
            Geometry.Part("Tanque esculpido", PrimitiveType.Sphere, root, new Vector3(0, .22f, .15f), new Vector3(.48f, .36f, .70f), paint);
            Geometry.Part("Tampa combustível", PrimitiveType.Cylinder, root, new Vector3(0, .395f, .13f), new Vector3(.1f, .008f, .1f), metal);
            Geometry.Box("Banco", root, new Vector3(0, .24f, -.39f), new Vector3(.34f, .13f, .53f), rubber);
            Geometry.Box("Rabeta", root, new Vector3(0, .13f, -.65f), new Vector3(.33f, .13f, .35f), paint).transform.localRotation = Quaternion.Euler(-12, 0, 0);
            Geometry.Rod("Coletor escape", root, new Vector3(.17f, -.15f, .26f), new Vector3(.22f, -.41f, -.25f), .028f, metal);
            Geometry.Rod("Ponteira escape", root, new Vector3(.22f, -.35f, -.22f), new Vector3(.24f, -.24f, -.79f), .07f, dark);
            Geometry.Box("Protetor escape", root, new Vector3(.29f, -.25f, -.55f), new Vector3(.025f, .12f, .37f), metal);
            bike.frontWheel = Wheel(root, "Roda dianteira", .67f, rubber, metal, dark);
            bike.rearWheel = Wheel(root, "Roda traseira", -.67f, rubber, metal, dark);
            var bar = new GameObject("Guidão").transform; bar.SetParent(root, false); bar.localPosition = new Vector3(0, .43f, .43f); bike.handlebar = bar;
            Geometry.Rod("Guidão", bar, new Vector3(-.39f, 0, -.04f), new Vector3(.39f, 0, -.04f), .022f, metal);
            foreach (int side in new[] { -1, 1 })
            {
                Geometry.Rod("Manopla", bar, new Vector3(side * .25f, 0, -.04f), new Vector3(side * .39f, 0, -.04f), .031f, rubber);
                Geometry.Rod("Manete", bar, new Vector3(side * .24f, 0, .04f), new Vector3(side * .39f, 0, .04f), .009f, metal);
                Geometry.Rod("Haste retrovisor", bar, new Vector3(side * .26f, 0, 0), new Vector3(side * .38f, .25f, .03f), .012f, dark);
                Geometry.Part("Retrovisor", PrimitiveType.Sphere, bar, new Vector3(side * .38f, .26f, .03f), new Vector3(.17f, .1f, .035f), metal);
            }
            Geometry.Box("Painel digital", bar, new Vector3(0, -.01f, .09f), new Vector3(.22f, .07f, .13f), dark).transform.localRotation = Quaternion.Euler(25, 0, 0);
            Geometry.Box("Farol LED", root, new Vector3(0, .18f, .63f), new Vector3(.29f, .12f, .1f), glow);
            var lamp = new GameObject("Facho do farol"); lamp.transform.SetParent(root, false); lamp.transform.localPosition = new Vector3(0, .18f, .72f);
            bike.headlight = lamp.AddComponent<Light>(); bike.headlight.type = LightType.Spot; bike.headlight.spotAngle = 65; bike.headlight.range = 38; bike.headlight.intensity = 4;
            bike.brakeLight = Geometry.Box("Lanterna", root, new Vector3(0, .18f, -.84f), new Vector3(.23f, .05f, .03f), glow).GetComponent<Renderer>();
        }

        private static Transform Wheel(Transform parent, string name, float z, Material rubber, Material metal, Material dark)
        {
            var pivot = new GameObject(name).transform; pivot.SetParent(parent, false); pivot.localPosition = new Vector3(0, -.49f, z);
            Geometry.Torus("Pneu", pivot, Vector3.zero, .247f, .063f, rubber);
            Geometry.Torus("Aro usinado", pivot, Vector3.zero, .213f, .017f, metal);
            var hub = Geometry.Part("Cubo", PrimitiveType.Cylinder, pivot, Vector3.zero, new Vector3(.085f, .085f, .085f), dark); hub.transform.localRotation = Quaternion.Euler(0, 0, 90);
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI / 5;
                Geometry.Rod("Raio", pivot, Vector3.zero, new Vector3(0, Mathf.Cos(a) * .21f, Mathf.Sin(a) * .21f), .007f, metal);
            }
            Geometry.Torus("Disco de freio", pivot, new Vector3(.07f, 0, 0), .135f, .012f, metal);
            return pivot;
        }
    }
}
