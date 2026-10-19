using UnityEngine;

namespace MotoAcademy
{
    public enum SurfaceKind { Asphalt, Wet, Gravel, Grass }
    public sealed class SurfaceZone : MonoBehaviour
    {
        public SurfaceKind kind;
        public string label = "Asfalto seco";
        public float grip = 1f;
        public float speedLimit = 40f;
    }
}
