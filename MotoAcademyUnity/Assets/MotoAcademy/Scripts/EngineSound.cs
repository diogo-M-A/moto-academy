using System.Threading;
using UnityEngine;

namespace MotoAcademy
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class EngineSound : MonoBehaviour
    {
        public MotorcycleController bike;
        private float phase, frequency, amplitude, wind;
        private int sampleRate;
        private uint noise=197;
        private void Awake()
        {
            sampleRate=AudioSettings.outputSampleRate;
            var source=GetComponent<AudioSource>();source.loop=true;source.spatialBlend=0;source.volume=.22f;
            source.clip=AudioClip.Create("Motor procedural",sampleRate,1,sampleRate,false);source.Play();
        }
        private void Update()
        {
            if(bike==null)return;
            Volatile.Write(ref frequency,bike.Engine.Rpm/120f);
            Volatile.Write(ref amplitude,Time.timeScale>0&&bike.Engine.Running?.20f+bike.Input.Throttle*.35f:0);
            Volatile.Write(ref wind,Time.timeScale>0?Mathf.Clamp01(bike.Speed/100)*.09f:0);
        }
        private void OnAudioFilterRead(float[] data,int channels)
        {
            // Audio thread: plain floats only, never query scene objects here.
            float hz=Volatile.Read(ref frequency),amp=Volatile.Read(ref amplitude),air=Volatile.Read(ref wind);
            for(int i=0;i<data.Length;i+=channels)
            {
                phase+=hz/sampleRate;if(phase>=1)phase-=1;
                noise=noise*1664525+1013904223;
                float n=((noise>>9)/(float)8388608)*2-1;
                float wave=(float)(System.Math.Sin(phase*System.Math.PI*2)*.65+System.Math.Sin(phase*System.Math.PI*4)*.24+System.Math.Sin(phase*System.Math.PI*10)*.11);
                float value=wave*amp+n*(air+amp*.06f);
                for(int c=0;c<channels;c++)data[i+c]=value;
            }
        }
    }
}
