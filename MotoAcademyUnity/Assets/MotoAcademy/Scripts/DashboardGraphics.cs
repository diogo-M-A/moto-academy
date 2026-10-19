using UnityEngine;
using UnityEngine.UI;

namespace MotoAcademy
{
    public sealed class TachometerGraphic : MaskableGraphic
    {
        public float value;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();Vector2 center=rectTransform.rect.center;float radius=rectTransform.rect.width*.46f;
            for(int i=0;i<60;i++)
            {
                float a=Mathf.Lerp(225,-45,i/60f)*Mathf.Deg2Rad,b=Mathf.Lerp(225,-45,(i+.72f)/60)*Mathf.Deg2Rad;
                Color c=i/60f<value?(i>49?new Color(1,.32f,.2f):new Color(.32f,.91f,.77f)):new Color(.18f,.25f,.28f);
                float inner=radius-(i%5==0?18:11);
                Quad(vh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*inner,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,
                    center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*inner,c);
            }
        }
        public static void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color color)
        {
            int i=vh.currentVertCount;vh.AddVert(a,color,Vector2.zero);vh.AddVert(b,color,Vector2.zero);vh.AddVert(c,color,Vector2.zero);vh.AddVert(d,color,Vector2.zero);
            vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
        }
    }
    public sealed class CircuitMapGraphic : MaskableGraphic
    {
        public MotorcycleController bike;
        public TrainingDirector director;
        private Vector2 Point(Vector3 p)=>rectTransform.rect.center+new Vector2(p.x,p.z)*1.08f;
        private void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color color)
        {
            Vector2 side=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;
            TachometerGraphic.Quad(vh,a-side,a+side,b+side,b-side,color);
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();Color road=new Color(.29f,.39f,.40f);
            for(int i=0;i<64;i++)
            {
                float a=i*Mathf.PI*2/64,b=(i+1)*Mathf.PI*2/64;
                Line(vh,Point(new Vector3(Mathf.Sin(a)*48,0,Mathf.Cos(a)*82)),Point(new Vector3(Mathf.Sin(b)*48,0,Mathf.Cos(b)*82)),5,road);
            }
            Line(vh,Point(new Vector3(0,0,-75)),Point(new Vector3(0,0,75)),9,road);
            Line(vh,Point(new Vector3(20,0,-15)),Point(new Vector3(20,0,48)),5,new Color(.25f,.63f,.63f));
            Line(vh,Point(new Vector3(-24,0,5)),Point(new Vector3(-24,0,37)),5,new Color(.79f,.63f,.36f));
            if(bike==null)return;
            if(director.Mode==RideMode.Exam&&director.GateIndex<director.World.Route.Count)
            {
                Vector2 g=Point(director.World.Route[director.GateIndex]);
                TachometerGraphic.Quad(vh,g+new Vector2(-4,-4),g+new Vector2(-4,4),g+new Vector2(4,4),g+new Vector2(4,-4),new Color(1,.72f,.32f));
            }
            Vector2 p=Point(bike.transform.position);Vector3 f=bike.transform.forward;Vector2 dir=new Vector2(f.x,f.z),side=new Vector2(-dir.y,dir.x);
            int start=vh.currentVertCount;vh.AddVert(p+dir*8,Color.white,Vector2.zero);vh.AddVert(p-dir*5+side*5,Color.white,Vector2.zero);vh.AddVert(p-dir*5-side*5,Color.white,Vector2.zero);vh.AddTriangle(start,start+1,start+2);
        }
    }
}
