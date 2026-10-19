using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace MotoAcademy
{
    /// <summary>Opt-in integration checks in the actual Windows player, never active in ordinary play.</summary>
    public sealed class RuntimeSmokeTest : MonoBehaviour
    {
        public MotorcycleController Bike;
        public TrainingDirector Director;
        public WorldBuilder World;
        public AcademyHUD Hud;
        [Serializable] private sealed class Report
        {
            public string unityVersion;
            public string graphicsDevice;
            public string timestamp;
            public int passed,failed;
            public List<string> checks=new List<string>();
            public List<string> runtimeErrors=new List<string>();
        }
        private readonly Report report=new Report();
        private string output;
        private bool finished;
        private void OnEnable(){Application.logMessageReceived+=RecordError;}
        private void OnDisable(){Application.logMessageReceived-=RecordError;}
        private void RecordError(string condition,string stack,LogType type)
        {
            if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)report.runtimeErrors.Add(condition+"\n"+stack);
        }
        private void Check(bool ok,string detail)
        {
            if(ok)report.passed++;else report.failed++;
            string line=(ok?"PASS ":"FAIL ")+detail;report.checks.Add(line);Debug.Log("ACADEMY_TEST "+line);
        }
        private IEnumerator Start()
        {
            Application.runInBackground=true;
            var args=Environment.GetCommandLineArgs();int arg=Array.IndexOf(args,"-academy-test-output");
            output=arg>=0&&arg+1<args.Length?args[arg+1]:Path.Combine(Application.persistentDataPath,"SmokeTest");
            Directory.CreateDirectory(output);
            report.unityVersion=Application.unityVersion;report.graphicsDevice=SystemInfo.graphicsDeviceName;report.timestamp=DateTime.Now.ToString("O");
            yield return new WaitForSecondsRealtime(.5f);
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"01-menu.png"));
            Hud.BeginAutomatedRide();
            yield return new WaitForSeconds(1.5f);
            Check(Bike.Grounded&&Bike.transform.position.y>.65f&&Bike.transform.position.y<1.1f,"Suspensão estabiliza no pátio: y="+Bike.transform.position.y);
            Bike.Input.Automated=false;
            yield return TapKey(Key.R);
            Check(Bike.Engine.Running,"Tecla R passa pelo Input System e liga o motor em neutro");
            if(!Bike.Engine.Running)Bike.Engine.Start(0);
            var camera=Camera.main.GetComponent<RideCamera>();int initialCamera=camera.Mode;
            yield return TapKey(Key.V);
            Check(camera.Mode!=(initialCamera),"Tecla V alterna a câmera pelo Input System");
            while(camera.Mode!=initialCamera)camera.Next();
            yield return TapKey(Key.Escape);
            Check(Time.timeScale==0,"Esc pausa a sessão pelo teclado");
            yield return TapKey(Key.Escape);
            Check(Time.timeScale>0,"Esc retoma a sessão pelo teclado");
            if(Time.timeScale==0)Hud.BeginAutomatedRide();
            Bike.Input.Automated=true;
            yield return new WaitForSeconds(1);
            Check(Bike.Engine.Running&&Bike.Speed<1,"Marcha lenta sem movimento");
            Bike.Input.Clutch=1;Bike.Engine.Shift(1,1);Bike.Input.Throttle=.5f;
            yield return new WaitForSeconds(.6f);
            float elapsed=0;
            while(elapsed<2.5f){elapsed+=Time.deltaTime;Bike.Input.Clutch=Mathf.Clamp01(1-elapsed/2.5f);yield return null;}
            yield return new WaitForSeconds(5);
            Check(Bike.Engine.Running&&Bike.Speed>12,"Arrancada progressiva em física real: "+Bike.Speed.ToString("F1")+" km/h");
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"02-pilotagem.png"));
            float initialX=Bike.transform.position.x;Bike.Input.Steering=.35f;
            yield return new WaitForSeconds(1.5f);
            Check(Bike.transform.position.x>initialX+.5f&&Mathf.Abs(Bike.Lean)>2,"Direção produz trajetória e inclinação");
            Bike.Input.Steering=0;Bike.Input.Throttle=0;Bike.Input.Clutch=1;Bike.Input.FrontBrake=1;Bike.Input.RearBrake=1;
            yield return new WaitForSeconds(5);
            Check(Bike.Speed<1&&Bike.Engine.Running,"Frenagem dos dois eixos para sem stall: "+Bike.Speed.ToString("F2"));
            Place(new Vector3(20,.94f,0));
            yield return new WaitForSeconds(1.5f);
            Check(Bike.Surface!=null&&Bike.Surface.kind==SurfaceKind.Wet&&Bike.Grip<.7f,"Contato reconhece asfalto molhado");
            Place(new Vector3(20,.94f,34));
            yield return new WaitForSeconds(1.5f);
            Check(Bike.Surface!=null&&Bike.Surface.kind==SurfaceKind.Gravel,"Contato reconhece cascalho");
            Place(new Vector3(-24,3.55f,21));
            yield return new WaitForSeconds(2);
            Check(Bike.Grounded&&Bike.SurfaceName.Contains("Rampa")&&Bike.transform.position.y>3.2f,"Suspensão sustenta a moto no topo da rampa");
            Place(new Vector3(48,.86f,0));
            yield return new WaitForSeconds(1.5f);
            Check(Bike.Grounded&&Bike.SurfaceName=="Circuito externo","Malha do circuito externo tem colisão superior");
            Place(new Vector3(0,.86f,-50));
            yield return new WaitForSeconds(1);
            Bike.Input.Automated=false;
            yield return TapKey(Key.F6);
            Bike.Input.Automated=true;
            yield return new WaitForSeconds(2);
            Check(WorldBuilder.Raining&&Bike.Grip<.8f,"Chuva reduz aderência do asfalto");
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"03-chuva.png"));
            World.ToggleWeather();
            Director.Save();
            Check(File.Exists(Director.SavePath)&&File.ReadAllText(Director.SavePath).Contains("savedAt"),"Sessão gravada em JSON");
            Finish();
        }
        private void Place(Vector3 position)
        {
            Bike.Body.position=position;Bike.Body.rotation=Quaternion.identity;Bike.Body.linearVelocity=Vector3.zero;Bike.Body.angularVelocity=Vector3.zero;
        }
        private IEnumerator TapKey(Key key)
        {
            var keyboard=Keyboard.current??InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return null;
        }
        private void Update()
        {
            if(!finished&&Time.realtimeSinceStartup>180)
            {
                Check(false,"Tempo máximo de integração excedido");Finish();
            }
        }
        private void Finish()
        {
            if(finished)return;finished=true;
            if(report.runtimeErrors.Count>0)Check(false,"Ocorreram "+report.runtimeErrors.Count+" erros no player");
            File.WriteAllText(Path.Combine(output,"runtime-report.json"),JsonUtility.ToJson(report,true));
            Debug.Log("ACADEMY_TEST_RESULT "+report.passed+" passed / "+report.failed+" failed");
            StartCoroutine(QuitAfterCapture());
        }
        private IEnumerator QuitAfterCapture()
        {
            // Complete the screenshot readback and leave EndOfFrame before tearing down the player window.
            yield return null;
            yield return new WaitForSecondsRealtime(1);
            Application.Quit(report.failed==0?0:1);
        }
    }
}
