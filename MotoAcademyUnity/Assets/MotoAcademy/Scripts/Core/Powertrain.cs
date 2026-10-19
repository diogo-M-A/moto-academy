using System;

namespace MotoAcademy.Core
{
    [Serializable]
    public sealed class BikeSpec
    {
        public float mass = 220f;
        public float idleRpm = 1500f;
        public float stallRpm = 750f;
        public float redlineRpm = 9000f;
        public float peakTorque = 14.5f;
        public float wheelRadius = .31f;
        public float primary = 3f;
        public float finalDrive = 2.8f;
        public float engineInertia = .045f;
        public float[] gears = { 0f, 2.80f, 1.80f, 1.35f, 1.10f, .95f };
    }

    /// <summary>Independent, fixed-step educational clutch/engine model. SI except RPM.</summary>
    public sealed class Powertrain
    {
        public readonly BikeSpec Spec;
        public bool Running { get; private set; }
        public bool KillSwitchRun { get; private set; } = true;
        public int Gear { get; private set; }
        public float Rpm { get; private set; }
        public float WheelTorque { get; private set; }
        public float ClutchHeat { get; private set; }
        public int Stalls { get; private set; }
        public int BadShifts { get; private set; }
        public string Message { get; private set; } = "R / X para ligar o motor";
        private float stallTime;

        public Powertrain(BikeSpec spec) { Spec = spec; }
        public static float Clamp(float value, float min, float max) { return Math.Max(min, Math.Min(value, max)); }
        public float Ratio { get { return Spec.gears[Gear] * Spec.primary * Spec.finalDrive; } }
        public static float Engagement(float clutch) { return Clamp((.85f - clutch) / .65f, 0f, 1f); }

        public bool Start(float clutch)
        {
            if (!KillSwitchRun) { Message = "K / Y: coloque o corta-corrente em RUN"; return false; }
            if (Gear != 0 && clutch < .85f) { Message = "Puxe a embreagem ou selecione neutro"; return false; }
            Running = true; Rpm = Spec.idleRpm; stallTime = 0;
            Message = "Motor ligado. Use C + E para engatar a primeira.";
            return true;
        }

        public void ToggleKill()
        {
            KillSwitchRun = !KillSwitchRun;
            if (!KillSwitchRun) { Running = false; Rpm = 0; WheelTorque = 0; }
            Message = KillSwitchRun ? "Corta-corrente em RUN" : "Corta-corrente desligado";
        }

        public bool Shift(int direction, float clutch)
        {
            int next = Math.Max(0, Math.Min(Spec.gears.Length - 1, Gear + Math.Sign(direction)));
            if (next == Gear) return false;
            if (clutch < .72f) { BadShifts++; Message = "Troca recusada: puxe a embreagem"; return false; }
            Gear = next;
            Message = Gear == 0 ? "Neutro selecionado" : "Marcha " + Gear + " engatada";
            return true;
        }

        public bool Neutral(float clutch, float speed)
        {
            if (Math.Abs(speed) > 2f || clutch < .72f) { Message = "Neutro: baixa velocidade e embreagem puxada"; return false; }
            Gear = 0; Message = "Neutro selecionado"; return true;
        }

        public void Step(float dt, float speed, float throttle, float clutch)
        {
            dt = Clamp(dt, .0001f, .05f);
            throttle = Clamp(throttle, 0, 1);
            float engagement = Engagement(clutch);
            WheelTorque = 0;
            float wheelRpm = Math.Max(0, speed) / (2f * (float)Math.PI * Spec.wheelRadius) * 60f;
            float shaftRpm = wheelRpm * Ratio;
            float coupling = Gear == 0 ? 0 : Clamp((Rpm - shaftRpm) * .018f, -24f, 24f) * engagement;
            if (Running)
            {
                float curve = Clamp(1f - (float)Math.Pow((Rpm - 6000f) / 7600f, 2) * .55f, .40f, 1f);
                float combustion = throttle * Spec.peakTorque * curve;
                float idle = Clamp((Spec.idleRpm - Rpm) * .022f, 0, 6f);
                float drag = .65f + Rpm * .00027f;
                if (Rpm >= Spec.redlineRpm) combustion = 0;
                Rpm = Clamp(Rpm + (combustion + idle - drag - coupling) / Spec.engineInertia * 9.5493f * dt, 0, Spec.redlineRpm + 200);
                WheelTorque = coupling * Ratio * .88f;
                stallTime = Rpm < Spec.stallRpm ? stallTime + dt : 0;
                if (stallTime > .24f)
                {
                    Running = false; Stalls++; Rpm = 0; WheelTorque = 0;
                    Message = "Motor morreu. Puxe C, pressione R e solte C aos poucos.";
                }
            }
            else
            {
                Rpm = 0;
                // Compression braking remains when an unpowered engine is coupled.
                WheelTorque = Gear == 0 ? 0 : -Math.Min(shaftRpm * .006f, 8f) * Ratio * engagement;
            }
            float slipPower = Math.Abs(coupling * (Rpm - shaftRpm)) * .10472f;
            ClutchHeat = Clamp(ClutchHeat + (slipPower / 90000f - .025f) * dt, 0, 1);
        }

        public void Reset()
        {
            Running = false; KillSwitchRun = true; Gear = 0; Rpm = 0; WheelTorque = 0;
            stallTime = 0; ClutchHeat = 0; Message = "Moto reposicionada. R / X para ligar.";
        }
    }
}
