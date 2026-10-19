using UnityEngine;
using UnityEngine.InputSystem;

namespace MotoAcademy
{
    public sealed class BikeInput : MonoBehaviour
    {
        public float Throttle, Clutch, Steering, FrontBrake, RearBrake;
        public bool Starter, Kill, Up, Down, Neutral, ResetBike, Abs, CameraNext, ModeNext, Restart, Pause, Help, Weather;
        public bool GamepadConnected => Gamepad.current != null;
        public bool Blocked;
        public bool Automated;
        private void Update()
        {
            if(Automated)
            {
                Starter=Kill=Up=Down=Neutral=ResetBike=Abs=CameraNext=ModeNext=Restart=Pause=Help=Weather=false;
                return;
            }
            var k = Keyboard.current;
            var g = Gamepad.current;
            Pause = k != null && k.escapeKey.wasPressedThisFrame;
            Help = k != null && k.f1Key.wasPressedThisFrame;
            if (Blocked)
            {
                Throttle = FrontBrake = RearBrake = Steering = 0;
                Starter = Kill = Up = Down = Neutral = ResetBike = Abs = CameraNext = ModeNext = Restart = Weather = false;
                return;
            }
            float dt = Time.deltaTime;
            float kt = k != null && k.wKey.isPressed ? 1 : 0;
            float kc = k != null && k.cKey.isPressed ? 1 : 0;
            Throttle = Mathf.MoveTowards(Throttle, Mathf.Max(kt, g != null ? g.rightTrigger.ReadValue() : 0), dt * 2.2f);
            float clutchTarget = Mathf.Max(kc, g != null ? g.leftTrigger.ReadValue() : 0);
            Clutch = Mathf.MoveTowards(Clutch, clutchTarget, dt * (clutchTarget > Clutch ? 5f : .9f));
            float steer = k == null ? 0 : (k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0);
            if (g != null && Mathf.Abs(g.leftStick.x.ReadValue()) > .12f) steer = g.leftStick.x.ReadValue();
            Steering = Mathf.MoveTowards(Steering, steer, dt * 3.5f);
            FrontBrake = Mathf.MoveTowards(FrontBrake, (k != null && k.spaceKey.isPressed) || (g != null && g.buttonEast.isPressed) ? 1 : 0, dt * 2.5f);
            RearBrake = Mathf.MoveTowards(RearBrake, (k != null && k.sKey.isPressed) || (g != null && g.buttonSouth.isPressed) ? 1 : 0, dt * 3);
            Starter = (k != null && k.rKey.wasPressedThisFrame) || (g != null && g.buttonWest.wasPressedThisFrame);
            Kill = (k != null && k.kKey.wasPressedThisFrame) || (g != null && g.buttonNorth.wasPressedThisFrame);
            Up = (k != null && k.eKey.wasPressedThisFrame) || (g != null && g.rightShoulder.wasPressedThisFrame);
            Down = (k != null && k.qKey.wasPressedThisFrame) || (g != null && g.leftShoulder.wasPressedThisFrame);
            Neutral = (k != null && k.nKey.wasPressedThisFrame) || (g != null && g.dpad.up.wasPressedThisFrame);
            ResetBike = (k != null && k.backspaceKey.wasPressedThisFrame) || (g != null && g.selectButton.wasPressedThisFrame);
            Abs = (k != null && k.zKey.wasPressedThisFrame) || (g != null && g.rightStickButton.wasPressedThisFrame);
            CameraNext = (k != null && k.vKey.wasPressedThisFrame) || (g != null && g.leftStickButton.wasPressedThisFrame);
            ModeNext = (k != null && k.tKey.wasPressedThisFrame) || (g != null && g.startButton.wasPressedThisFrame);
            Restart = (k != null && k.pKey.wasPressedThisFrame) || (g != null && g.dpad.right.wasPressedThisFrame);
            Weather = k != null && k.f6Key.wasPressedThisFrame;
        }
    }
}
