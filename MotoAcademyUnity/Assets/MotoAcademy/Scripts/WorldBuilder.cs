using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MotoAcademy
{
    public sealed class WorldBuilder : MonoBehaviour
    {
        public static bool Raining;
        public Material Asphalt, Paint, Dark, Metal, Rubber, White, Teal, Grass, Gravel, Glow, Wet;
        public Transform WorldRoot { get; private set; }
        public readonly List<Vector3> Route = new List<Vector3> {
            new Vector3(0,0,-30), new Vector3(0,0,20), new Vector3(0,0,76),
            new Vector3(36,0,54), new Vector3(48,0,0), new Vector3(34,0,-58), new Vector3(0,0,-76)
        };
        private Material orange, glass, stone;
        private Light sun;
        private ParticleSystem rain;
        private readonly List<Material> materials = new List<Material>();
        public MotorcycleController Build()
        {
            Raining = false;
            WorldRoot = new GameObject("Centro de treinamento • Costa Verde").transform; WorldRoot.SetParent(transform, false);
            Asphalt = Mat("Asfalto", new Color(.20f,.23f,.25f), .03f, .25f, true);
            Paint = Mat("Coral acetinado", new Color(.94f,.24f,.13f), .35f, .65f);
            Dark = Mat("Grafite", new Color(.035f,.06f,.075f), .25f, .35f);
            Metal = Mat("Alumínio", new Color(.55f,.64f,.68f), .85f, .7f);
            Rubber = Mat("Borracha", new Color(.025f,.03f,.035f), 0, .12f);
            White = Mat("Marfim", new Color(.88f,.89f,.82f), 0, .3f);
            Teal = Mat("Verde oceano", new Color(.04f,.43f,.42f), .15f, .5f);
            Grass = Mat("Gramado", new Color(.24f,.36f,.24f), 0, .05f, true);
            Gravel = Mat("Cascalho", new Color(.48f,.41f,.30f), 0, .04f, true);
            Glow = Mat("LED", new Color(.8f,1,1), .1f, .7f); Glow.EnableKeyword("_EMISSION"); Glow.SetColor("_EmissionColor", new Color(.6f,1,1) * 3);
            Wet = Mat("Piso molhado", new Color(.075f,.12f,.14f), .3f, .93f, true);
            orange = Mat("Cone laranja", new Color(1,.38f,.08f), 0, .3f);
            glass = Mat("Vidro fumê", new Color(.14f,.29f,.32f), .65f, .95f);
            stone = Mat("Concreto", new Color(.55f,.57f,.51f), 0, .15f, true);
            Lighting(); Ground(); Campus(); Scenery();
            var go = new GameObject("Moto 160"); go.transform.position = new Vector3(0,.84f,-55);
            go.AddComponent<BikeInput>(); go.AddComponent<Rigidbody>();
            var body = go.AddComponent<CapsuleCollider>(); body.radius = .22f; body.height = 1.65f; body.direction = 2; body.center = new Vector3(0,-.06f,0);
            var bike = go.AddComponent<MotorcycleController>(); BikeVisual.Build(bike, Paint, Dark, Metal, Rubber, Glow);
            return bike;
        }

        private Material Mat(string name, Color color, float metal, float smooth, bool grain = false)
        {
            var template = Resources.Load<Material>("AcademyLit");
            var m = template != null ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.name = name; m.color = color; m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", smooth);
            if (grain)
            {
                var tex = new Texture2D(64,64,TextureFormat.RGB24,true); tex.name = name + " • ruído";
                var random = new System.Random(name.Length * 31); var pixels = new Color[64*64];
                for (int i = 0; i < pixels.Length; i++) { float n = .75f + (float)random.NextDouble() * .5f; pixels[i] = new Color(n,n,n); }
                tex.SetPixels(pixels); tex.Apply(); tex.wrapMode = TextureWrapMode.Repeat;
                m.SetTexture("_BaseMap", tex); m.SetTextureScale("_BaseMap", new Vector2(24,24));
            }
            materials.Add(m); return m;
        }

        private void Lighting()
        {
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .0017f;
            RenderSettings.fogColor = new Color(.66f,.77f,.78f); RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.53f,.69f,.78f); RenderSettings.ambientEquatorColor = new Color(.38f,.43f,.40f); RenderSettings.ambientGroundColor = new Color(.17f,.19f,.16f);
            var light = new GameObject("Sol • fim de tarde"); light.transform.rotation = Quaternion.Euler(35,-32,0);
            sun = light.AddComponent<Light>(); sun.type = LightType.Directional; sun.color = new Color(1,.88f,.72f); sun.intensity = 1.7f; sun.shadows = LightShadows.Soft; sun.shadowBias = .035f;
            RenderSettings.sun = sun;
            var sky = Resources.Load<Material>("AcademySky"); if (sky != null) RenderSettings.skybox = sky;
            var volume = new GameObject("Cor e exposição").AddComponent<Volume>(); volume.isGlobal = true;
            var savedProfile = Resources.Load<VolumeProfile>("AcademyVolume");
            if(savedProfile!=null){volume.sharedProfile=savedProfile;return;}
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile.Add<Tonemapping>().mode.Override(TonemappingMode.ACES);
            var bloom = volume.profile.Add<Bloom>(); bloom.intensity.Override(.22f); bloom.threshold.Override(1.2f);
            var vignette = volume.profile.Add<Vignette>(); vignette.intensity.Override(.20f); vignette.smoothness.Override(.5f);
            var colors = volume.profile.Add<ColorAdjustments>(); colors.postExposure.Override(.15f); colors.contrast.Override(10); colors.saturation.Override(-6);
        }

        private GameObject Pad(string name, Vector3 p, Vector3 scale, Material mat, SurfaceKind kind, float grip, float limit)
        {
            var go = Geometry.Box(name, WorldRoot, p, scale, mat, true, 8);
            var zone = go.AddComponent<SurfaceZone>(); zone.kind = kind; zone.label = name; zone.grip = grip; zone.speedLimit = limit; return go;
        }

        private void Ground()
        {
            Pad("Grama", new Vector3(0,-.18f,0), new Vector3(520,.3f,520), Grass, SurfaceKind.Grass,.42f,25);
            Pad("Pátio de treinamento", new Vector3(0,0,0), new Vector3(72,.08f,132), Asphalt, SurfaceKind.Asphalt,1,40);
            Pad("Acesso norte", new Vector3(0,.005f,70), new Vector3(14,.08f,28), Asphalt, SurfaceKind.Asphalt,1,40);
            Pad("Acesso sul", new Vector3(0,.005f,-70), new Vector3(14,.08f,28), Asphalt, SurfaceKind.Asphalt,1,40);
            RingRoad();
            Pad("Asfalto molhado", new Vector3(20,.055f,0), new Vector3(11,.09f,30), Wet, SurfaceKind.Wet,.58f,25);
            Pad("Cascalho", new Vector3(20,.055f,34), new Vector3(11,.09f,28), Gravel, SurfaceKind.Gravel,.48f,25);
            for (int z = -62; z <= 62; z += 7)
                Geometry.Box("Linha central",WorldRoot,new Vector3(0,.052f,z),new Vector3(.15f,.008f,3),White);
            for (int z = -60; z <= 60; z += 3)
            {
                Geometry.Box("Borda pista",WorldRoot,new Vector3(-7,.053f,z),new Vector3(.12f,.009f,2.4f),White);
                Geometry.Box("Borda pista",WorldRoot,new Vector3(7,.053f,z),new Vector3(.12f,.009f,2.4f),White);
            }
            for (int i=0;i<9;i++) Cone(new Vector3(-11 + (i%2==0 ? -2:2), .05f, -48+i*7));
            for (int i=0;i<10;i++)
                Geometry.Box("Faixa de frenagem",WorldRoot,new Vector3(i*.65f-3,.056f,47),new Vector3(.34f,.01f,4),White);
            Ramp();
            Geometry.Sign(WorldRoot,"01  /  ARRANCADA",new Vector3(-5,2.2f,-59),.5f,Teal,Color.white);
            Geometry.Sign(WorldRoot,"02  /  SLALOM",new Vector3(-17,2,-48),.4f,Dark,Color.white);
            Geometry.Sign(WorldRoot,"03  /  ADERÊNCIA",new Vector3(23,2,-20),.45f,Dark,Color.white);
        }

        private void RingRoad()
        {
            const int count = 192;
            var vertices = new Vector3[(count+1)*2]; var uv = new Vector2[vertices.Length]; var tri = new int[count*6];
            for(int i=0;i<=count;i++)
            {
                float a=i*Mathf.PI*2/count;
                vertices[i*2]=new Vector3(Mathf.Sin(a)*42,.045f,Mathf.Cos(a)*76);
                vertices[i*2+1]=new Vector3(Mathf.Sin(a)*54,.045f,Mathf.Cos(a)*88);
                uv[i*2]=new Vector2(0,i*.3f); uv[i*2+1]=new Vector2(1,i*.3f);
                if(i<count) { int t=i*6, p=i*2; tri[t]=p;tri[t+1]=p+1;tri[t+2]=p+2;tri[t+3]=p+1;tri[t+4]=p+3;tri[t+5]=p+2; }
                if(i%2==0)
                {
                    var marker=Geometry.Box("Sinalização do circuito",WorldRoot,new Vector3(Mathf.Sin(a)*48,.055f,Mathf.Cos(a)*82),new Vector3(.14f,.015f,1.5f),White);
                    marker.transform.rotation=Quaternion.LookRotation(new Vector3(Mathf.Cos(a)*48,0,-Mathf.Sin(a)*82));
                }
                if(i<count)
                {
                    var curb=Geometry.Box("Zebra",WorldRoot,new Vector3(Mathf.Sin(a)*54.5f,.07f,Mathf.Cos(a)*88.5f),new Vector3(1,.12f,2.5f),i%2==0?White:Paint);
                    curb.transform.rotation=Quaternion.LookRotation(new Vector3(Mathf.Cos(a)*54.5f,0,-Mathf.Sin(a)*88.5f));
                }
            }
            var road=Geometry.MeshObject("Circuito externo",WorldRoot,vertices,tri,Asphalt,true,8,uv);
            var zone=road.AddComponent<SurfaceZone>();zone.label="Circuito externo";zone.grip=1;zone.speedLimit=50;
        }

        private void Ramp()
        {
            float[] z = { 5,17,25,37 }; float[] h = { .08f,2.7f,2.7f,.08f };
            for(int i=0;i<3;i++)
            {
                Vector3[] v={ new Vector3(-29,h[i],z[i]),new Vector3(-29,h[i+1],z[i+1]),new Vector3(-19,h[i],z[i]),new Vector3(-19,h[i+1],z[i+1]) };
                var go=Geometry.MeshObject("Rampa • segmento "+i,WorldRoot,v,new[]{0,1,2,2,1,3},Asphalt,true,8);
                var zone=go.AddComponent<SurfaceZone>();zone.label="Rampa de treinamento";zone.speedLimit=20;
                for(int side=0;side<2;side++)
                {
                    float x=side==0?-29.3f:-18.7f;
                    Geometry.Rod("Guarda-corpo",WorldRoot,new Vector3(x,h[i]+.8f,z[i]),new Vector3(x,h[i+1]+.8f,z[i+1]),.055f,White);
                }
            }
            Geometry.Sign(WorldRoot,"04  /  RAMPA",new Vector3(-24,2,1),.42f,Teal,Color.white);
        }

        private void Campus()
        {
            Geometry.Box("Base do centro",WorldRoot,new Vector3(-73,.16f,-34),new Vector3(30,.4f,48),stone,true);
            Geometry.Box("Centro de pilotagem",WorldRoot,new Vector3(-78,3,-35),new Vector3(18,6,34),White,true);
            Geometry.Box("Fachada de vidro",WorldRoot,new Vector3(-68.9f,3,-35),new Vector3(.12f,4,29),glass);
            for(int i=0;i<10;i++) Geometry.Box("Brise",WorldRoot,new Vector3(-68.6f,3,-49+i*3.1f),new Vector3(.6f,4.5f,.12f),Dark);
            Geometry.Box("Cobertura flutuante",WorldRoot,new Vector3(-74,6.4f,-35),new Vector3(29,.35f,38),Dark);
            var signRoot=new GameObject("Letreiro").transform; signRoot.SetParent(WorldRoot,false); signRoot.position=new Vector3(-68,4.8f,-35); signRoot.rotation=Quaternion.Euler(0,-90,0);
            Geometry.Sign(signRoot,"MOTO / ACADEMY",Vector3.zero,.65f,Teal,Color.white);
            for(int i=0;i<4;i++)
            {
                float z=12+i*13;
                Geometry.Box("Cobertura do box",WorldRoot,new Vector3(-73,3.5f,z),new Vector3(18,.22f,10),Dark);
                for(int j=0;j<2;j++) Geometry.Rod("Pilar do box",WorldRoot,new Vector3(-79+j*14,0,z),new Vector3(-79+j*14,3.5f,z),.09f,Metal);
                Geometry.Box("Painel solar",WorldRoot,new Vector3(-73,3.65f,z),new Vector3(15,.08f,8),glass).transform.rotation=Quaternion.Euler(5,0,0);
            }
            for(int i=0;i<8;i++)
            {
                float z=-80+i*23;
                Geometry.Rod("Poste",WorldRoot,new Vector3(64,0,z),new Vector3(64,7,z),.08f,Dark);
                Geometry.Box("Luminária",WorldRoot,new Vector3(63.3f,7,z),new Vector3(1.8f,.13f,.5f),Glow);
                Geometry.Box("Barreira",WorldRoot,new Vector3(62,.45f,z),new Vector3(.7f,.9f,13),White,true);
            }
        }

        private void Scenery()
        {
            var rng=new System.Random(160);
            for(int i=0;i<140;i++)
            {
                float x=(float)rng.NextDouble()*350-175,z=(float)rng.NextDouble()*330-165;
                if(Mathf.Abs(x)<96&&Mathf.Abs(z)<105) continue;
                float height=5+(float)rng.NextDouble()*7;
                Geometry.Rod("Tronco",WorldRoot,new Vector3(x,0,z),new Vector3(x,height*.6f,z),.18f,stone);
                var crown=Geometry.Part("Copa",PrimitiveType.Sphere,WorldRoot,new Vector3(x,height*.8f,z),new Vector3(height*.65f,height*.8f,height*.65f),Grass);
                crown.transform.rotation=Quaternion.Euler(0,(float)rng.NextDouble()*360,10);
            }
            for(int i=0;i<22;i++)
            {
                float angle=i*Mathf.PI*2/22, radius=245+(float)rng.NextDouble()*40;
                float h=35+(float)rng.NextDouble()*55;
                Vector3 center=new Vector3(Mathf.Sin(angle)*radius,-10,Mathf.Cos(angle)*radius);
                Geometry.Part("Morro costeiro",PrimitiveType.Sphere,WorldRoot,center,new Vector3(100,h*2,110),Grass);
            }
            for(int i=0;i<6;i++)
            {
                float z=-47+i*17;
                Geometry.Rod("Mastro",WorldRoot,new Vector3(-57,0,z),new Vector3(-57,6,z),.04f,Metal);
                Geometry.Box("Bandeira",WorldRoot,new Vector3(-56,5,z),new Vector3(1.8f,2,.04f),i%2==0?Teal:Paint);
            }
        }

        private void Cone(Vector3 position)
        {
            var root=new GameObject("Cone");root.transform.SetParent(WorldRoot,false);root.transform.position=position;
            Geometry.Box("Base",root.transform,new Vector3(0,.04f,0),new Vector3(.55f,.08f,.55f),Rubber,true);
            const int n=16;var v=new Vector3[n+2];var tri=new int[n*6];v[n]=new Vector3(0,.72f,0);v[n+1]=new Vector3(0,.08f,0);
            for(int i=0;i<n;i++) { float a=i*Mathf.PI*2/n;v[i]=new Vector3(Mathf.Sin(a)*.23f,.08f,Mathf.Cos(a)*.23f);int t=i*6;tri[t]=i;tri[t+1]=(i+1)%n;tri[t+2]=n;tri[t+3]=i;tri[t+4]=n+1;tri[t+5]=(i+1)%n; }
            Geometry.MeshObject("Corpo",root.transform,v,tri,orange);
            Geometry.Part("Faixa refletiva",PrimitiveType.Cylinder,root.transform,new Vector3(0,.36f,0),new Vector3(.26f,.045f,.26f),White);
        }

        public void ToggleWeather()
        {
            Raining=!Raining; sun.intensity=Raining?.7f:1.7f;
            RenderSettings.fogDensity=Raining?.004f:.0017f;
            Asphalt.SetFloat("_Smoothness",Raining?.78f:.25f);
            if(rain==null)
            {
                var go=new GameObject("Chuva");rain=go.AddComponent<ParticleSystem>();
                var main=rain.main;main.startLifetime=2.2f;main.startSpeed=22;main.startSize=.025f;main.maxParticles=6000;main.startColor=new Color(.7f,.85f,.9f,.45f);
                var emission=rain.emission;emission.rateOverTime=1800;
                var shape=rain.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(70,60,1);
                go.transform.rotation=Quaternion.Euler(90,0,0);
                var renderer=rain.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=8;renderer.sharedMaterial=Glow;
            }
            if(Raining)rain.Play();else rain.Stop();
        }
        private void LateUpdate()
        {
            if(rain!=null&&Camera.main!=null) rain.transform.position=Camera.main.transform.position+Vector3.up*30;
        }
    }
}
