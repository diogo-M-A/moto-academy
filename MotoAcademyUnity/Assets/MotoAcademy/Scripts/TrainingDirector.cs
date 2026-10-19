using System;
using System.IO;
using MotoAcademy.Core;
using UnityEngine;

namespace MotoAcademy
{
    public enum RideMode { Lessons, FreeRide, Exam }
    [DefaultExecutionOrder(20)]
    public sealed class TrainingDirector : MonoBehaviour
    {
        public MotorcycleController Bike;
        public WorldBuilder World;
        public RideMode Mode { get; private set; }
        public SessionScore Score { get; private set; } = new SessionScore();
        public int Lesson { get; private set; }
        public int Unlocked { get; private set; }
        public float Progress { get; private set; }
        public bool LessonComplete { get; private set; }
        public string Feedback { get; private set; } = "Bem-vindo ao centro de treinamento.";
        public string SaveStatus { get; private set; } = "Salvamento automático ativo";
        public int GateIndex { get; private set; }
        public float GateDistance => Mode == RideMode.Exam && GateIndex < World.Route.Count ? Vector3.Distance(Bike.transform.position, World.Route[GateIndex]) : 0;
        public string SavePath => Path.Combine(Application.persistentDataPath, "avaliacao_sessao.json");
        public readonly string[] Titles = {
            "Primeiro contato", "Zona de atrito", "Uma saída suave", "Encontrando o ritmo", "Redução consciente",
            "Parada com controle", "O desenho da curva", "Frenagem progressiva", "Saída em subida", "Lendo o piso"
        };
        public readonly string[] Instructions = {
            "Em neutro (N), pressione R para ligar. Observe a marcha lenta.",
            "Segure C e pressione E para a 1ª. Acelere levemente e module C: mantenha a embreagem entre 35–70% por 2 segundos, abaixo de 12 km/h.",
            "Em 1ª, acelere com W e solte C gradualmente. Sustente 12–25 km/h com o motor ligado por 2 segundos.",
            "Parta em 1ª, alcance 15 km/h, puxe C e pressione E para a 2ª. Mantenha a velocidade com a embreagem solta.",
            "Alcance a 3ª marcha. Diminua a velocidade e use C + Q para voltar à 2ª, ainda em movimento.",
            "Alcance 15 km/h. Use Espaço + S aos poucos e puxe C antes de parar. Mantenha o motor ligado.",
            "Use A / D entre 15–40 km/h. Sustente curvas com mais de 8° de inclinação por 3 segundos no total.",
            "Alcance 30 km/h. Combine Espaço + S, puxe C e pare com o motor ligado e sem travar as rodas.",
            "Encontre a rampa à esquerda do pátio. Suba em 1ª mantendo o motor ligado e mais de 4 km/h por 2 segundos.",
            "Na faixa à direita, percorra 10 metros no asfalto molhado e 10 metros no cascalho, abaixo de 25 km/h."
        };
        private float held, advance, speedTimer, rpmTimer, lockTimer, saveTimer, lowRpmTimer, brakingTimer, wetDistance, gravelDistance;
        private bool armed, sawThird, sawFirst, usedBoth, lockedDuringBrake;
        private int lastStalls, lastShifts;
        private Vector3 previousPosition;
        private Transform gatesRoot;
        private Renderer[] gateRenderers;
        private float feedbackUntil;

        public void Initialize()
        {
            Unlocked = Mathf.Clamp(PlayerPrefs.GetInt("MotoAcademy.Unlocked",0),0,Titles.Length-1);
            Lesson = Mathf.Clamp(PlayerPrefs.GetInt("MotoAcademy.Lesson",0),0,Unlocked);
            Bike.Incident += Register;
            BuildGates(); NewSession();
        }

        private void BuildGates()
        {
            gatesRoot = new GameObject("Checkpoints de avaliação").transform;
            gateRenderers = new Renderer[World.Route.Count];
            for (int i=0;i<World.Route.Count;i++)
            {
                var root = new GameObject("Checkpoint " + (i+1)).transform;
                root.SetParent(gatesRoot,false); root.position=World.Route[i];
                Vector3 prev=i==0?new Vector3(0,0,-55):World.Route[i-1];
                root.rotation=Quaternion.LookRotation((World.Route[i]-prev).normalized);
                foreach(int side in new[]{-1,1})
                    Geometry.Box("Baliza",root,new Vector3(side*5,1.6f,0),new Vector3(.18f,3.2f,.18f),World.Teal);
                gateRenderers[i]=Geometry.Box("Portal luminoso",root,new Vector3(0,3.2f,0),new Vector3(10.3f,.15f,.25f),World.Glow).GetComponent<Renderer>();
                Geometry.Sign(root,"CHECKPOINT " + (i+1),new Vector3(0,3.7f,0),.35f,World.Teal,Color.white);
            }
            gatesRoot.gameObject.SetActive(false);
        }

        public void SetMode(RideMode mode)
        {
            Save(); Mode=mode; Time.timeScale=1;
            gatesRoot.gameObject.SetActive(mode==RideMode.Exam);
            if(mode==RideMode.Exam) { Bike.ResetToSpawn(false); NewSession(); }
            else if(mode==RideMode.Lessons) ResetLesson();
            Feedback=mode==RideMode.Exam?"Avaliação iniciada. Cruze os 7 portais em ordem e depois pare.":mode==RideMode.FreeRide?"Explore o circuito. T retorna às aulas.":Instructions[Lesson];
            feedbackUntil=Time.time+6;
        }

        public void SelectLesson(int index)
        {
            if(index>Unlocked)return;
            Lesson=Mathf.Clamp(index,0,Titles.Length-1); Mode=RideMode.Lessons;
            gatesRoot.gameObject.SetActive(false); ResetLesson();
        }

        public void ResetLesson()
        {
            held=advance=wetDistance=gravelDistance=0; Progress=0;
            armed=sawThird=sawFirst=usedBoth=lockedDuringBrake=LessonComplete=false;
            Feedback=Instructions[Lesson]; feedbackUntil=Time.time+3;
        }

        public void NewSession()
        {
            Score=new SessionScore(); GateIndex=0;
            lastStalls=Bike.Engine.Stalls;lastShifts=Bike.Engine.BadShifts;
            speedTimer=rpmTimer=lockTimer=lowRpmTimer=brakingTimer=0;
            previousPosition=Bike.transform.position;
            UpdateGateColors();
        }

        private void Update()
        {
            if(Bike==null)return;
            if(Bike.Input.ModeNext)SetMode((RideMode)(((int)Mode+1)%3));
            if(Bike.Input.Restart)
            {
                if(Mode==RideMode.Exam)SetMode(Mode);else if(Mode==RideMode.Lessons)ResetLesson();else NewSession();
            }
            if(Time.timeScale==0)return;
            float dt=Time.deltaTime;
            bool finished=Mode==RideMode.Exam&&Score.examComplete;
            if(!finished)
            {
                Score.seconds+=dt;Score.distance+=Bike.Speed/3.6f*dt;Score.topSpeed=Mathf.Max(Score.topSpeed,Bike.Speed);
                Evaluate(dt);
                if(Mode==RideMode.Lessons)Teach(dt);
                if(Mode==RideMode.Exam)Examine();
            }
            previousPosition=Bike.transform.position;
            saveTimer+=dt;if(saveTimer>8){Save();saveTimer=0;}
            if(Time.time>feedbackUntil&&!LessonComplete&&!finished)Feedback=Bike.Engine.Message;
        }

        private void Teach(float dt)
        {
            if(LessonComplete)
            {
                advance+=dt;if(advance>3&&Lesson<Titles.Length-1){Lesson++;ResetLesson();SaveProgress();} return;
            }
            var e=Bike.Engine;var input=Bike.Input; bool ok=false;
            switch(Lesson)
            {
                case 0: ok=e.Running&&e.Gear==0;break;
                case 1: ok=e.Running&&e.Gear==1&&input.Clutch>=.35f&&input.Clutch<=.70f&&Bike.Speed<12;break;
                case 2: ok=e.Running&&e.Gear==1&&Bike.Speed>=12&&Bike.Speed<=25&&input.Clutch<.2f;break;
                case 3:
                    if(e.Gear==1&&Bike.Speed>=15)sawFirst=true;
                    ok=sawFirst&&e.Running&&e.Gear==2&&input.Clutch<.2f&&Bike.Speed>=12;break;
                case 4:
                    if(e.Gear>=3)sawThird=true;
                    ok=sawThird&&e.Running&&e.Gear==2&&Bike.Speed>6&&input.Clutch<.25f;break;
                case 5:
                    if(Bike.Speed>=15)armed=true;
                    ok=armed&&Bike.Speed<1&&e.Running&&input.Clutch>.7f;break;
                case 6: ok=e.Running&&Bike.Speed>=15&&Bike.Speed<=40&&Mathf.Abs(Bike.Lean)>8;break;
                case 7:
                    if(Bike.Speed>=30&&!armed){armed=true;usedBoth=false;lockedDuringBrake=false;}
                    if(armed&&input.FrontBrake>.2f&&input.RearBrake>.2f)usedBoth=true;
                    if(armed&&Bike.WheelLocked)lockedDuringBrake=true;
                    if(armed&&Bike.Speed<1&&lockedDuringBrake){armed=false;Feedback="Roda travou. Tente novamente com frenagem progressiva.";feedbackUntil=Time.time+4;}
                    ok=armed&&usedBoth&&!lockedDuringBrake&&Bike.Speed<1&&e.Running&&input.Clutch>.7f;break;
                case 8: ok=e.Running&&e.Gear==1&&Bike.SurfaceName.Contains("Rampa")&&Bike.Speed>4;break;
                case 9:
                    if(Bike.Speed<25&&Bike.Surface!=null)
                    {
                        if(Bike.Surface.kind==SurfaceKind.Wet)wetDistance+=Bike.Speed/3.6f*dt;
                        if(Bike.Surface.kind==SurfaceKind.Gravel)gravelDistance+=Bike.Speed/3.6f*dt;
                    }
                    Progress=(Mathf.Clamp01(wetDistance/10)+Mathf.Clamp01(gravelDistance/10))*.5f;
                    if(wetDistance>=10&&gravelDistance>=10)Complete();return;
            }
            float needed=Lesson==1||Lesson==2||Lesson==8?2:Lesson==6?3: .5f;
            held=ok?held+dt:Lesson==6?held:0;
            Progress=Mathf.Clamp01(held/needed);if(Progress>=1)Complete();
        }

        private void Complete()
        {
            LessonComplete=true;advance=0;Score.completedLessons++;
            Unlocked=Mathf.Max(Unlocked,Mathf.Min(Lesson+1,Titles.Length-1));
            Feedback=Lesson==Titles.Length-1?"Treinamento concluído. Experimente a avaliação no menu.":"Objetivo concluído. Próxima aula em 3 segundos.";
            feedbackUntil=Time.time+4;SaveProgress();Save();
        }

        private void Evaluate(float dt)
        {
            if(Bike.Engine.Stalls>lastStalls)
            {
                Register("Motor morreu: module a embreagem e preserve RPM",12);held=0;
                lastStalls=Bike.Engine.Stalls;
            }
            if(Bike.Engine.BadShifts>lastShifts){Register("Troca sem embreagem",5);lastShifts=Bike.Engine.BadShifts;}
            speedTimer=Bike.Speed>Bike.Limit+2?speedTimer+dt:0;
            rpmTimer=Bike.Engine.Running&&Bike.Engine.Rpm>8200?rpmTimer+dt:0;
            lockTimer=Bike.WheelLocked?lockTimer+dt:0;
            lowRpmTimer=Bike.Engine.Running&&Bike.Engine.Gear>=2&&Bike.Speed>6&&Bike.Engine.Rpm<1300&&Bike.Input.Throttle>.2f&&Bike.Input.Clutch<.2f?lowRpmTimer+dt:0;
            brakingTimer=Bike.Speed>3&&Bike.Input.Throttle>.45f&&(Bike.Input.FrontBrake>.3f||Bike.Input.RearBrake>.3f)?brakingTimer+dt:0;
            if(speedTimer>3){Register("Excesso de velocidade na zona",6);speedTimer=0;}
            if(rpmTimer>2){Register("RPM excessiva sustentada",4);rpmTimer=0;}
            if(lockTimer>1){Register("Roda bloqueada: reduza a pressão no freio",7);lockTimer=0;}
            if(lowRpmTimer>1.5f){Register("Marcha alta para a rotação: reduza uma marcha",5);lowRpmTimer=0;}
            if(brakingTimer>2){Register("Aceleração e frenagem simultâneas sustentadas",5);brakingTimer=0;}
        }

        public void Register(string detail,int penalty)
        {
            if(Mode==RideMode.Exam&&Score.examComplete)return;
            Score.Penalize("conducao",detail,penalty);Feedback=detail+"  −"+penalty+" pontos";feedbackUntil=Time.time+5;
        }

        private void Examine()
        {
            if(GateIndex<World.Route.Count)
            {
                var gate=gatesRoot.GetChild(GateIndex);
                float before=Vector3.Dot(previousPosition-gate.position,gate.forward),now=Vector3.Dot(Bike.transform.position-gate.position,gate.forward);
                Vector3 local=gate.InverseTransformPoint(Bike.transform.position);
                if(before<0&&now>=0&&Mathf.Abs(local.x)<4.8f&&Bike.Grounded&&Bike.transform.position.y<2)
                {
                    GateIndex++;Score.checkpoints=GateIndex;UpdateGateColors();
                    Feedback=GateIndex==World.Route.Count?"Percurso concluído. Pare com o motor ligado para finalizar.":"Checkpoint "+GateIndex+" validado. Siga ao próximo portal.";
                    feedbackUntil=Time.time+4;
                }
            }
            else if(Bike.Speed<1&&Bike.Engine.Running&&(Bike.Input.Clutch>.7f||Bike.Engine.Gear==0))
            {
                Score.examComplete=true;
                int best=PlayerPrefs.GetInt("MotoAcademy.BestScore",0);if(Score.score>best)PlayerPrefs.SetInt("MotoAcademy.BestScore",Score.score);
                Feedback="Avaliação concluída • "+Score.score+" / 100 pontos. P para tentar novamente.";Save();
            }
        }

        private void UpdateGateColors()
        {
            if(gateRenderers==null)return;
            for(int i=0;i<gateRenderers.Length;i++)gateRenderers[i].sharedMaterial=i==GateIndex?World.Glow:i<GateIndex?World.Teal:World.Dark;
        }

        private void SaveProgress()
        {
            PlayerPrefs.SetInt("MotoAcademy.Unlocked",Unlocked);PlayerPrefs.SetInt("MotoAcademy.Lesson",Lesson);PlayerPrefs.Save();
        }
        public void Save()
        {
            try
            {
                Score.savedAt=DateTime.Now.ToString("O");
                Directory.CreateDirectory(Application.persistentDataPath);
                string temp=SavePath+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(Score,true));
                if(File.Exists(SavePath))File.Copy(SavePath,SavePath+".bak",true);
                File.Copy(temp,SavePath,true);File.Delete(temp);SaveProgress();
                SaveStatus="Sessão salva • "+DateTime.Now.ToString("HH:mm");
            }
            catch(Exception ex) when(ex is IOException||ex is UnauthorizedAccessException)
            { SaveStatus="Não foi possível salvar a sessão";Debug.LogWarning(ex.Message); }
        }
        private void OnApplicationQuit(){Save();}
        private void OnApplicationPause(bool paused){if(paused&&Bike!=null)Save();}
    }
}
