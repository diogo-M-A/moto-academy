using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MotoAcademy
{
    public sealed class AcademyBootstrap : MonoBehaviour
    {
        private void Start()
        {
            Application.targetFrameRate=60;QualitySettings.vSyncCount=1;
            Time.fixedDeltaTime=.01f;Time.maximumDeltaTime=.1f;
            Physics.defaultSolverIterations=12;Physics.defaultSolverVelocityIterations=6;
            var world=gameObject.AddComponent<WorldBuilder>();var bike=world.Build();
            var director=gameObject.AddComponent<TrainingDirector>();director.Bike=bike;director.World=world;director.Initialize();
            var cameraObject=new GameObject("Câmera de pilotagem");cameraObject.tag="MainCamera";
            cameraObject.transform.position=bike.transform.position+new Vector3(.65f,2.25f,-5.2f);
            cameraObject.transform.LookAt(bike.transform.position+Vector3.up*.3f);
            var camera=cameraObject.AddComponent<Camera>();camera.nearClipPlane=.045f;camera.farClipPlane=650;camera.allowHDR=true;camera.fieldOfView=58;
            camera.backgroundColor=new Color(.62f,.76f,.81f);cameraObject.AddComponent<AudioListener>();
            var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var ride=cameraObject.AddComponent<RideCamera>();ride.bike=bike;
            var sound=bike.gameObject.AddComponent<EngineSound>();sound.bike=bike;
            var hud=gameObject.AddComponent<AcademyHUD>();hud.Bike=bike;hud.Director=director;hud.World=world;hud.RideCamera=ride;hud.Build();
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-academy-smoke-test")>=0)
            {
                var test=gameObject.AddComponent<RuntimeSmokeTest>();test.Bike=bike;test.Director=director;test.World=world;test.Hud=hud;
            }
        }
    }
}
