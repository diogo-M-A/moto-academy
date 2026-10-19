using System;
using System.Collections.Generic;
using MotoAcademy.Core;

public static class CoreTests
{
    private static readonly List<string> results=new List<string>();
    private static void Check(bool condition,string name)
    {
        if(!condition)throw new Exception("FALHOU: "+name);
        results.Add("PASSOU: "+name);
    }
    private static Powertrain NewEngine(){return new Powertrain(new BikeSpec());}
    public static string[] Run()
    {
        results.Clear();
        var e=NewEngine();
        Check(e.Start(0)&&e.Running&&e.Gear==0,"Partida em neutro sem puxar embreagem");
        for(int i=0;i<1000;i++)e.Step(.01f,0,0,0);
        Check(e.Running&&e.Rpm>1250&&e.Rpm<1600,"Marcha lenta se mantém por 10 segundos");
        Check(!e.Shift(1,0)&&e.Gear==0&&e.BadShifts==1,"Troca sem embreagem é recusada e registrada");
        Check(e.Shift(1,1)&&e.Gear==1,"Primeira marcha com manete puxado");
        for(int i=0;i<300;i++)e.Step(.01f,0,0,0);
        Check(!e.Running&&e.Stalls==1,"Soltar embreagem parado faz o motor morrer");
        Check(!e.Start(0)&&!e.Running,"Partida engrenada sem embreagem é bloqueada");
        Check(e.Start(1)&&e.Running,"É possível religar após stall com embreagem puxada");
        e.ToggleKill();Check(!e.Running&&!e.Start(1),"Corta-corrente desliga e impede partida");
        e.ToggleKill();e.Start(1);
        for(int i=0;i<1500;i++)e.Step(.01f,0,1,1);
        Check(e.Running&&e.Rpm<=9200&&e.WheelTorque==0,"Limitador contém RPM e embreagem desacopla a roda");
        for(int i=0;i<9;i++)e.Shift(1,1);
        Check(e.Gear==5,"Câmbio respeita limite de cinco marchas");
        Check(!e.Neutral(1,10)&&e.Gear==5,"Neutro é bloqueado em velocidade");
        Check(e.Neutral(1,0)&&e.Gear==0,"Neutro permitido parado com embreagem");
        for(int i=0;i<10;i++)e.Shift(-1,1);
        Check(e.Gear==0,"Câmbio não passa abaixo do neutro");
        float launchSpeed=Launch(.01f);
        Check(launchSpeed>3.5f,"Arrancada progressiva alcança mais de 12,6 km/h: "+(launchSpeed*3.6f).ToString("F1"));
        float otherSpeed=Launch(.02f);
        Check(Math.Abs(launchSpeed-otherSpeed)<.8f,"Arrancada consistente em passos de 10 e 20 ms");
        e=NewEngine();e.Start(0);e.Shift(1,1);e.ToggleKill();e.Step(.01f,10,0,0);
        Check(e.WheelTorque<0,"Motor desligado engrenado oferece freio por compressão");
        e.Reset();Check(e.Gear==0&&!e.Running&&e.Rpm==0&&e.KillSwitchRun,"Reset restaura neutro e estado de partida");
        var score=new SessionScore();score.Penalize("stall","Motor morreu",12);
        Check(score.score==88&&score.events.Count==1,"Avaliação desconta e registra a ocorrência");
        for(int i=0;i<250;i++)score.Penalize("colisao","Colisão",8);
        Check(score.score==0&&score.events.Count==200,"Pontuação não fica negativa e histórico tem limite");
        score.Penalize("x","x",-100);
        Check(score.score==0,"Penalidade negativa não aumenta pontuação");
        e=NewEngine();e.Start(0);
        for(int i=0;i<20000;i++)
        {
            float t=i*.01f;
            if(i%500==0)e.Shift(i%1000==0?1:-1,1);
            e.Step(.01f,(float)Math.Abs(Math.Sin(t)*30),(float)Math.Abs(Math.Sin(t*3)),(float)Math.Abs(Math.Sin(t*.7)));
            if(!e.Running)e.Start(1);
        }
        Check(!float.IsNaN(e.Rpm)&&!float.IsInfinity(e.WheelTorque)&&e.ClutchHeat>=0&&e.ClutchHeat<=1,"200 segundos de entradas variadas mantêm estado finito");
        return results.ToArray();
    }
    private static float Launch(float dt)
    {
        var e=NewEngine();e.Start(0);e.Shift(1,1);float speed=0;
        int count=(int)Math.Round(8/dt);
        for(int i=0;i<count;i++)
        {
            float time=i*dt,clutch=Math.Max(0,1-Math.Max(0,time-.5f)/2.5f);
            e.Step(dt,speed,.45f,clutch);
            float drive=e.WheelTorque/e.Spec.wheelRadius;
            speed=Math.Max(0,speed+(drive-20-speed*speed*.24f)/e.Spec.mass*dt);
        }
        if(e.Stalls>0)throw new Exception("Arrancada progressiva causou stall.");
        return speed;
    }
}
