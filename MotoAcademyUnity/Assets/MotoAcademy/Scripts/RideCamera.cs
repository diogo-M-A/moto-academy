using UnityEngine;

namespace MotoAcademy
{
    [DefaultExecutionOrder(30)]
    public sealed class RideCamera : MonoBehaviour
    {
        public MotorcycleController bike;
        public int Mode { get; private set; }
        private Vector3 velocity;
        private Camera lens;
        public string ViewName => Mode==0?"PERSEGUIÇÃO":Mode==1?"PILOTO":"CINEMÁTICA";
        private void Awake(){lens=GetComponent<Camera>();}
        public void Next(){Mode=(Mode+1)%3;velocity=Vector3.zero;}
        private void LateUpdate()
        {
            if(bike==null)return;
            if(bike.Input.CameraNext)Next();
            Vector3 target=bike.transform.position+Vector3.up*.25f;
            Vector3 desired;
            if(Mode==1)
            {
                desired=bike.visual.TransformPoint(new Vector3(0,.76f,-.12f));
                transform.position=desired;
                transform.rotation=Quaternion.Slerp(transform.rotation,bike.transform.rotation*Quaternion.Euler(5,0,bike.Lean*.35f),1-Mathf.Exp(-Time.deltaTime*12));
            }
            else
            {
                Vector3 local=Mode==0?new Vector3(.65f,2.25f,-5.2f):new Vector3(-5.5f,2.5f,-7);
                desired=bike.transform.TransformPoint(local);
                Vector3 ray=desired-target;
                if(Physics.SphereCast(target,.2f,ray.normalized,out RaycastHit hit,ray.magnitude,1<<8,QueryTriggerInteraction.Ignore))desired=target+ray.normalized*Mathf.Max(.6f,hit.distance-.2f);
                transform.position=Vector3.SmoothDamp(transform.position,desired,ref velocity,.14f);
                transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(target+bike.transform.forward*1.8f-transform.position),1-Mathf.Exp(-Time.deltaTime*9));
            }
            lens.fieldOfView=Mathf.Lerp(lens.fieldOfView,(Mode==1?72:58)+Mathf.Clamp(bike.Speed*.12f,0,12),Time.deltaTime*2);
        }
    }
}
