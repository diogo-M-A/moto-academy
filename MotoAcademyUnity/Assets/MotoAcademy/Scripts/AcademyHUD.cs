using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MotoAcademy
{
    [DefaultExecutionOrder(40)]
    public sealed class AcademyHUD : MonoBehaviour
    {
        public MotorcycleController Bike;
        public TrainingDirector Director;
        public RideCamera RideCamera;
        public WorldBuilder World;
        private readonly Color ink=new Color(.045f,.075f,.09f,.95f),muted=new Color(.55f,.66f,.67f),mint=new Color(.38f,.91f,.77f),white=new Color(.94f,.96f,.93f);
        private Font font;
        private Transform root,menu,help;
        private Text speed,gear,rpm,lessonTitle,instruction,feedback,score,surface,stats,mode,lessonIndex,physics,connection,engineState,menuStatus;
        private RectTransform throttleBar,clutchBar,frontBar,rearBar,progressBar;
        private TachometerGraphic tach;
        private CircuitMapGraphic map;
        private bool paused;
        private float refresh;
        public void Build()
        {
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas=new GameObject("Interface • Moto Academy",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;root=canvas.transform;
            var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            Panel(root,"Identidade",48,965,410,68,ink);
            Label(root,"M /",68,973,75,52,35,mint,FontStyle.Bold);
            Label(root,"MOTO ACADEMY",150,998,290,26,23,white,FontStyle.Bold);
            Label(root,"COSTA VERDE  /  CENTRO DE TREINAMENTO",151,977,290,18,11,muted);
            mode=Label(root,"",500,990,640,32,19,white,FontStyle.Bold);
            Button(root,"ESC  /  MENU",1620,978,250,48,()=>TogglePause(),false);
            Panel(root,"Instrutor",48,610,420,314,ink);
            lessonIndex=Label(root,"",72,876,370,25,12,mint,FontStyle.Bold);
            lessonTitle=Label(root,"",72,817,365,58,28,white,FontStyle.Bold);
            instruction=Label(root,"",72,670,365,144,18,white);
            Panel(root,"Progresso",72,644,365,4,new Color(.2f,.28f,.29f));
            progressBar=Panel(root,"Progresso preenchido",72,644,0,4,mint);
            Label(root,"T  alternar modo     P  reiniciar objetivo",72,620,365,18,12,muted);
            Panel(root,"Telemetria",48,170,420,245,ink);
            Label(root,"CONTROLE É PROGRESSO",72,374,365,23,13,mint,FontStyle.Bold);
            throttleBar=Bar("ACELERADOR", "W / RT",331,mint);
            clutchBar=Bar("EMBREAGEM", "C / LT",286,new Color(1,.72f,.36f));
            frontBar=Bar("FREIO DIANTEIRO", "ESPAÇO / B",241,new Color(.9f,.48f,.37f));
            rearBar=Bar("FREIO TRASEIRO", "S / A",196,new Color(.9f,.48f,.37f));
            Panel(root,"Mapa",1600,650,270,290,ink);
            Label(root,"CIRCUITO / AO VIVO",1620,907,230,19,12,mint,FontStyle.Bold);
            var mapRect=Rect(root,"Mapa vetorial",1600,660,270,240);map=mapRect.gameObject.AddComponent<CircuitMapGraphic>();map.bike=Bike;map.director=Director;map.raycastTarget=false;
            score=Label(root,"",1630,610,240,29,22,white,FontStyle.Bold,TextAnchor.MiddleRight);
            var dial=Rect(root,"Conta-giros",1510,85,360,360);tach=dial.gameObject.AddComponent<TachometerGraphic>();tach.raycastTarget=false;
            speed=Label(root,"000",1550,223,280,112,91,white,FontStyle.Bold,TextAnchor.MiddleCenter);
            Label(root,"km/h",1590,200,200,30,18,muted,FontStyle.Normal,TextAnchor.MiddleCenter);
            gear=Label(root,"N",1645,135,90,65,43,mint,FontStyle.Bold,TextAnchor.MiddleCenter);
            rpm=Label(root,"",1535,100,310,23,13,muted,FontStyle.Normal,TextAnchor.MiddleCenter);
            engineState=Label(root,"",1510,64,360,24,14,mint,FontStyle.Bold,TextAnchor.MiddleCenter);
            surface=Label(root,"",1450,520,420,60,18,white,FontStyle.Normal,TextAnchor.MiddleRight);
            physics=Label(root,"",1450,445,420,75,14,muted,FontStyle.Normal,TextAnchor.MiddleRight);
            Panel(root,"Feedback",560,60,800,88,ink);
            feedback=Label(root,"",584,72,750,64,18,white,FontStyle.Normal,TextAnchor.MiddleCenter);
            stats=Label(root,"",560,160,800,28,14,white,FontStyle.Normal,TextAnchor.MiddleCenter);
            connection=Label(root,"",48,110,580,25,13,muted);
            Label(root,"Q / E  marchas     N  neutro     R  partida     K  corta-corrente     Z  ABS     V  câmera     F6  chuva     F1  ajuda",48,34,1400,24,14,white);
            BuildMenu();BuildHelp();SetPaused(true);
        }

        private RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();
            r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);return r;
        }
        private RectTransform Panel(Transform parent,string name,float x,float y,float w,float h,Color color)
        {var r=Rect(parent,name,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return r;}
        private Text Label(Transform parent,string content,float x,float y,float w,float h,int size,Color color,FontStyle style=FontStyle.Normal,TextAnchor align=TextAnchor.UpperLeft)
        {
            var r=Rect(parent,content,x,y,w,h);var text=r.gameObject.AddComponent<Text>();text.font=font;text.text=content;text.fontSize=size;text.color=color;text.fontStyle=style;text.alignment=align;text.raycastTarget=false;text.supportRichText=false;return text;
        }
        private void Button(Transform parent,string caption,float x,float y,float w,float h,UnityEngine.Events.UnityAction action,bool primary=true)
        {
            var r=Panel(parent,caption,x,y,w,h,primary?mint:new Color(.14f,.22f,.24f));r.GetComponent<Image>().raycastTarget=true;
            var button=r.gameObject.AddComponent<Button>();button.targetGraphic=r.GetComponent<Image>();button.onClick.AddListener(action);
            var colors=button.colors;colors.highlightedColor=new Color(.8f,1,.94f);colors.pressedColor=new Color(.5f,.75f,.7f);button.colors=colors;
            Label(r,caption,12,0,w-24,h,16,primary?ink:white,FontStyle.Bold,TextAnchor.MiddleCenter);
        }
        private RectTransform Bar(string title,string key,float y,Color color)
        {
            Label(root,title,72,y+13,230,20,12,white,FontStyle.Bold);Label(root,key,295,y+13,148,20,11,muted,FontStyle.Normal,TextAnchor.MiddleRight);
            Panel(root,title+" trilho",72,y,365,5,new Color(.17f,.25f,.27f));return Panel(root,title+" valor",72,y,0,5,color);
        }
        private void BuildMenu()
        {
            menu=Panel(root,"Menu",0,0,1920,1080,new Color(.015f,.035f,.045f,.90f));menu.GetComponent<Image>().raycastTarget=true;
            Panel(menu,"Cartão",450,145,1020,800,ink);
            Label(menu,"MOTO / ACADEMY",500,858,900,60,42,white,FontStyle.Bold);
            Label(menu,"APRENDA OS COMANDOS. ENCONTRE O SEU RITMO.",502,823,880,25,16,mint);
            Label(menu,"Uma moto manual de 160 cc. Dez aulas. Um circuito para explorar.",502,770,870,36,20,white);
            Label(menu,"R para ligar • C + E para engatar a primeira\nW para acelerar • solte C gradualmente para sair\nA / D para guiar • Espaço + S para frear\nPuxe C antes de parar para não deixar o motor morrer.",502,577,520,175,20,white);
            Button(menu,"CONTINUAR AULAS",1070,693,340,58,()=>StartMode(RideMode.Lessons));
            Button(menu,"PILOTAGEM LIVRE",1070,619,340,58,()=>StartMode(RideMode.FreeRide),false);
            Button(menu,"INICIAR AVALIAÇÃO",1070,545,340,58,()=>StartMode(RideMode.Exam),false);
            Button(menu,"RETOMAR",1070,471,340,58,()=>SetPaused(false),false);
            Button(menu,"ABS • ALTERNAR",502,489,245,48,()=>Bike.absEnabled=!Bike.absEnabled,false);
            Button(menu,"CHUVA • ALTERNAR",765,489,245,48,()=>World.ToggleWeather(),false);
            Label(menu,"REVISAR AULAS DESBLOQUEADAS",502,435,880,25,13,mint,FontStyle.Bold);
            for(int i=0;i<10;i++)
            {
                int lesson=i;Button(menu,(i+1).ToString("00"),502+i*90,377,76,45,()=>{if(lesson<=Director.Unlocked){Director.SelectLesson(lesson);SetPaused(false);}},false);
            }
            Button(menu,"CONTROLES / AJUDA",502,285,290,48,()=>help.gameObject.SetActive(true),false);
            Button(menu,"SALVAR SESSÃO",810,285,260,48,()=>Director.Save(),false);
            Button(menu,"SAIR",1088,285,322,48,()=>{Director.Save();Application.Quit();},false);
            menuStatus=Label(menu,"",502,224,900,46,14,muted);
            Label(menu,"Simulação didática com equilíbrio assistido. A prática real exige instrução adequada.",502,172,900,35,13,muted);
        }
        private void BuildHelp()
        {
            help=Panel(root,"Ajuda",0,0,1920,1080,new Color(.02f,.04f,.05f,.98f));help.GetComponent<Image>().raycastTarget=true;
            Label(help,"NO CONTROLE",400,910,1100,65,44,white,FontStyle.Bold);
            Label(help,"TECLADO",400,838,500,30,17,mint,FontStyle.Bold);Label(help,"CONTROLE XBOX",1000,838,500,30,17,mint,FontStyle.Bold);
            Label(help,"W  Acelerar\nC  Puxar embreagem (soltura gradual)\nA / D  Direção\nEspaço  Freio dianteiro\nS  Freio traseiro\nQ / E  Reduzir / subir marcha\nN  Neutro em baixa velocidade\nR  Partida   •   K  Corta-corrente\nZ  ABS   •   V  Câmera\nBackspace  Reposicionar moto\nT  Aulas / livre / avaliação\nP  Reiniciar objetivo ou avaliação\nF6  Chuva   •   Esc  Menu / pausa",400,336,550,480,21,white);
            Label(help,"RT  Acelerador analógico\nLT  Embreagem analógica\nAnalógico esquerdo  Direção\nB  Freio dianteiro\nA  Freio traseiro\nLB / RB  Reduzir / subir marcha\nD-pad ↑  Neutro\nX  Partida   •   Y  Corta-corrente\nR3  ABS   •   L3  Câmera\nView  Reposicionar moto\nMenu  Aulas / livre / avaliação\nD-pad →  Reiniciar objetivo",1000,336,560,480,21,white);
            Label(help,"A embreagem cheia significa manete puxado: motor desacoplado. A faixa de atrito fica entre 35% e 70%.\nO marcador dourado no mapa indica o próximo portal. Na avaliação, cruze-os em ordem e pare ao final.",400,221,1120,80,19,muted);
            Button(help,"VOLTAR",400,125,1120,60,()=>help.gameObject.SetActive(false));help.gameObject.SetActive(false);
        }
        private void StartMode(RideMode mode){Director.SetMode(mode);SetPaused(false);}
        public void BeginAutomatedRide(){StartMode(RideMode.FreeRide);Bike.Input.Automated=true;}
        private void TogglePause(){SetPaused(!paused);}
        private void SetPaused(bool value)
        {
            paused=value;Bike.Input.Blocked=value;Time.timeScale=value?0:1;menu.gameObject.SetActive(value);
            if(!value)help.gameObject.SetActive(false);Cursor.visible=true;Cursor.lockState=CursorLockMode.None;
            if(value)Director.Save();
        }
        private void Update()
        {
            if(Bike==null||menu==null)return;
            if(Bike.Input.Pause)TogglePause();
            if(Bike.Input.Help){if(!paused)SetPaused(true);help.gameObject.SetActive(!help.gameObject.activeSelf);}
            if(Bike.Input.Weather)World.ToggleWeather();
            refresh+=Time.unscaledDeltaTime;if(refresh<.06f)return;refresh=0;
            speed.text=Mathf.RoundToInt(Bike.Speed).ToString("000");gear.text=Bike.Engine.Gear==0?"N":Bike.Engine.Gear.ToString();
            rpm.text=Mathf.RoundToInt(Bike.Engine.Rpm)+" RPM  /  9.000";tach.value=Bike.Engine.Rpm/9000;tach.SetVerticesDirty();map.SetVerticesDirty();
            engineState.text=(Bike.Engine.Running?"MOTOR LIGADO":"MOTOR DESLIGADO")+"    •    ABS "+(Bike.absEnabled?"ON":"OFF");
            mode.text=Director.Mode==RideMode.Lessons?"ACADEMIA  /  TREINAMENTO GUIADO":Director.Mode==RideMode.FreeRide?"EXPLORE  /  PILOTAGEM LIVRE":"DESAFIO  /  AVALIAÇÃO PRÁTICA";
            lessonIndex.text=Director.Mode==RideMode.Lessons?"INSTRUTOR  /  AULA "+(Director.Lesson+1).ToString("00")+" DE 10":"CENTRO DE TREINAMENTO";
            lessonTitle.text=Director.Mode==RideMode.Lessons?Director.Titles[Director.Lesson]:Director.Mode==RideMode.FreeRide?"O circuito é seu.":Director.Score.examComplete?"Percurso concluído": "Siga os portais";
            instruction.text=Director.Mode==RideMode.Lessons?Director.Instructions[Director.Lesson]:Director.Mode==RideMode.FreeRide?"Pratique no pátio, explore o circuito externo e compare os pisos. A rampa fica à esquerda; asfalto molhado e cascalho à direita.":Director.Score.examComplete?"Pontuação: "+Director.Score.score+" / 100\nA avaliação está salva. P para recomeçar.":"Checkpoint "+Mathf.Min(Director.GateIndex+1,7)+" / 7\nDistância: "+Mathf.RoundToInt(Director.GateDistance)+" m\nCruze os portais em ordem. Respeite os limites de cada zona e pare ao finalizar.";
            progressBar.sizeDelta=new Vector2(365*(Director.Mode==RideMode.Lessons?Director.Progress:Director.Mode==RideMode.Exam?Director.GateIndex/7f:0),4);
            feedback.text=Director.Feedback;score.text=Director.Score.score+" / 100 PONTOS";
            surface.text=Bike.SurfaceName.ToUpperInvariant()+"\nLIMITE "+Mathf.RoundToInt(Bike.Limit)+" km/h";
            physics.text="INCLINAÇÃO "+Mathf.Abs(Bike.Lean).ToString("0")+"°   /   ADERÊNCIA "+(Bike.GripUse*100).ToString("0")+"%\n"+(Bike.AbsActive?"ABS ATUANDO":Bike.WheelLocked?"RODA BLOQUEADA":Bike.TractionLimited?"TRAÇÃO NO LIMITE":"CARGA DIANTEIRA "+(Bike.FrontLoad*100).ToString("0")+"%")+"\nAQUECIMENTO EMBREAGEM "+(Bike.Engine.ClutchHeat*100).ToString("0")+"%";
            stats.text=TimeSpanText(Director.Score.seconds)+"   /   "+(Director.Score.distance/1000).ToString("0.00")+" km   /   "+RideCamera.ViewName;
            connection.text=(Bike.Input.GamepadConnected?"CONTROLE CONECTADO":"TECLADO ATIVO")+"   •   "+(WorldBuilder.Raining?"CHUVA":"TEMPO ABERTO");
            Fill(throttleBar,Bike.Input.Throttle);Fill(clutchBar,Bike.Input.Clutch);Fill(frontBar,Bike.Input.FrontBrake);Fill(rearBar,Bike.Input.RearBrake);
            menuStatus.text="Aulas liberadas: "+(Director.Unlocked+1)+" / 10   •   ABS "+(Bike.absEnabled?"ligado":"desligado")+"   •   "+Director.SaveStatus;
        }
        private void Fill(RectTransform bar,float value){bar.sizeDelta=new Vector2(365*Mathf.Clamp01(value),5);}
        private string TimeSpanText(float time)=>((int)time/60).ToString("00")+":"+((int)time%60).ToString("00");
        private void OnDestroy(){Time.timeScale=1;}
    }
}
