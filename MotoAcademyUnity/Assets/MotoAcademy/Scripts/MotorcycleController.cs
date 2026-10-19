using MotoAcademy.Core;
using UnityEngine;

namespace MotoAcademy
{
    [RequireComponent(typeof(Rigidbody), typeof(BikeInput))]
    [DefaultExecutionOrder(10)]
    public sealed class MotorcycleController : MonoBehaviour
    {
        public BikeSpec spec = new BikeSpec();
        public Transform visual, frontWheel, rearWheel, handlebar;
        public Light headlight;
        public Renderer brakeLight;
        public Powertrain Engine { get; private set; }
        public BikeInput Input { get; private set; }
        public Rigidbody Body { get; private set; }
        public bool absEnabled = true;
        public bool AbsActive { get; private set; }
        public bool WheelLocked { get; private set; }
        public bool TractionLimited { get; private set; }
        public float Speed => Body == null ? 0 : Vector3.ProjectOnPlane(Body.linearVelocity, Vector3.up).magnitude * 3.6f;
        public float ForwardSpeed => Vector3.Dot(Body.linearVelocity, transform.forward);
        public float Lean { get; private set; }
        public float GripUse { get; private set; }
        public float FrontLoad { get; private set; } = .47f;
        public SurfaceZone Surface { get; private set; }
        public bool Grounded { get; private set; }
        public float Limit => Surface != null ? Surface.speedLimit : 40;
        public float Grip => (Surface != null ? Surface.grip : .95f) * (WorldBuilder.Raining && (Surface == null || Surface.kind == SurfaceKind.Asphalt) ? .65f : 1);
        public string SurfaceName => Surface != null ? Surface.label : "Asfalto seco";
        public System.Action<string, int> Incident;
        public readonly Vector3 Spawn = new Vector3(0, .84f, -55);
        private float yawRate, wheelAngle, previousSpeed, impactCooldown;
        private Vector3 groundNormal = Vector3.up;
        private MaterialPropertyBlock lightBlock;

        private void Awake()
        {
            Input = GetComponent<BikeInput>(); Body = GetComponent<Rigidbody>();
            Engine = new Powertrain(spec);
            Body.mass = spec.mass;
            Body.centerOfMass = new Vector3(0, -.28f, 0);
            Body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.maxLinearVelocity = 60;
            lightBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (Input.Kill) Engine.ToggleKill();
            if (Input.Starter) Engine.Start(Input.Clutch);
            if (Input.Up) Engine.Shift(1, Input.Clutch);
            if (Input.Down) Engine.Shift(-1, Input.Clutch);
            if (Input.Neutral) Engine.Neutral(Input.Clutch, Speed / 3.6f);
            if (Input.Abs) absEnabled = !absEnabled;
            if (Input.ResetBike) ResetToSpawn(true);
            if (headlight != null) headlight.enabled = Engine.Running;
            if (brakeLight != null)
            {
                lightBlock.SetColor("_EmissionColor", Color.red * (Input.FrontBrake + Input.RearBrake > .05f ? 4 : .3f));
                brakeLight.SetPropertyBlock(lightBlock);
            }
        }

        private bool Suspension(float z, out RaycastHit hit)
        {
            Vector3 point = transform.TransformPoint(new Vector3(0, .1f, z));
            // Ground is layer 8; excludes the motorcycle, gates and roadside props.
            if (!Physics.Raycast(point, Vector3.down, out hit, 1.25f, 1 << 8, QueryTriggerInteraction.Ignore)) return false;
            float compress = Mathf.Clamp(.94f - hit.distance, -.10f, .4f);
            float velocity = Vector3.Dot(Body.GetPointVelocity(point), hit.normal);
            float force = Mathf.Clamp(compress * 28000 - velocity * 3300, 0, spec.mass * 9.81f * 3);
            Body.AddForceAtPosition(hit.normal * force, point, ForceMode.Force);
            return hit.distance < 1.05f;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            bool fg = Suspension(.67f, out RaycastHit f), rg = Suspension(-.67f, out RaycastHit r);
            Grounded = fg || rg;
            groundNormal = fg && rg ? (f.normal + r.normal).normalized : fg ? f.normal : rg ? r.normal : Vector3.up;
            Surface = fg ? f.collider.GetComponent<SurfaceZone>() : rg ? r.collider.GetComponent<SurfaceZone>() : null;
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized;
            Vector3 right = Vector3.Cross(groundNormal, forward).normalized;
            float speed = Vector3.Dot(Body.linearVelocity, forward);
            Engine.Step(dt, speed, Input.Throttle, Input.Clutch);
            float accel = (speed - previousSpeed) / dt;
            previousSpeed = speed;
            FrontLoad = Mathf.Clamp(.47f - accel * .58f / (9.81f * 1.34f), .18f, .82f);
            AbsActive = WheelLocked = TractionLimited = false;
            if (Grounded)
            {
                float traction = spec.mass * 9.81f * Grip;
                float frontRequest = Input.FrontBrake * 1900f, rearRequest = Input.RearBrake * 850f;
                float frontCap = traction * FrontLoad, rearCap = traction * (1 - FrontLoad);
                bool frontLock = frontRequest > frontCap, rearLock = rearRequest > rearCap;
                AbsActive = absEnabled && Speed > 4 && (frontLock || rearLock);
                WheelLocked = !absEnabled && Speed > 4 && (frontLock || rearLock);
                float brake = Mathf.Min(frontRequest, frontCap) + Mathf.Min(rearRequest, rearCap);
                if (WheelLocked) brake *= .72f;
                float drive = Engine.WheelTorque / spec.wheelRadius;
                TractionLimited = Mathf.Abs(drive) > rearCap;
                drive = Mathf.Clamp(drive, -rearCap, rearCap);
                float resistance = speed * Mathf.Abs(speed) * .24f + (Mathf.Abs(speed) > .15f ? Mathf.Sign(speed) * 20f : 0);
                // Never let brakes reverse a stationary bike in a single step.
                float braking = Mathf.Sign(speed) * Mathf.Min(brake, Mathf.Abs(speed) * spec.mass / dt);
                float longitudinal = drive - braking - resistance;
                float steerAngle = Input.Steering * Mathf.Lerp(30f, 7f, Mathf.Clamp01(Speed / 90));
                float targetYaw = Mathf.Tan(steerAngle * Mathf.Deg2Rad) * speed / 1.34f;
                float lateralDemand = Mathf.Abs(targetYaw * speed) * spec.mass;
                GripUse = Mathf.Sqrt(longitudinal * longitudinal + lateralDemand * lateralDemand) / Mathf.Max(traction, 1);
                float availableLateral = Mathf.Sqrt(Mathf.Max(0, traction * traction - Mathf.Min(longitudinal * longitudinal, traction * traction)));
                float yawLimit = availableLateral / (spec.mass * Mathf.Max(Mathf.Abs(speed), 1));
                if (WheelLocked) yawLimit *= .32f;
                targetYaw = Mathf.Clamp(targetYaw, -yawLimit, yawLimit);
                yawRate = Mathf.Lerp(yawRate, targetYaw, 1 - Mathf.Exp(-dt * 5));
                Body.MoveRotation(Body.rotation * Quaternion.Euler(0, yawRate * Mathf.Rad2Deg * dt, 0));
                Body.AddForce(forward * longitudinal);
                float sideSpeed = Vector3.Dot(Body.linearVelocity, right);
                float lateral = Mathf.Clamp(-sideSpeed * spec.mass * 8, -availableLateral, availableLateral);
                Body.AddForce(right * lateral);
                // Static brake hold also works on the training ramp.
                if (Mathf.Abs(speed) < .15f && brake > 10)
                {
                    float slopeGravity = Vector3.Dot(Physics.gravity * spec.mass, forward);
                    Body.AddForce(forward * Mathf.Clamp(-slopeGravity, -brake, brake));
                }
            }
            else { GripUse = 0; yawRate = 0; }
            float targetLean = -Mathf.Atan(speed * yawRate / 9.81f) * Mathf.Rad2Deg;
            Lean = Mathf.Lerp(Lean, Mathf.Clamp(targetLean, -38, 38), dt * 5);
            if (visual != null)
            {
                float pitch = fg && rg ? -Mathf.Atan2(f.point.y - r.point.y, 1.34f) * Mathf.Rad2Deg : 0;
                visual.localRotation = Quaternion.Euler(pitch, 0, Lean);
            }
            wheelAngle += speed / spec.wheelRadius * Mathf.Rad2Deg * dt;
            if (frontWheel != null) frontWheel.localRotation = Quaternion.Euler(wheelAngle, Input.Steering * 20f, 0);
            if (rearWheel != null) rearWheel.localRotation = Quaternion.Euler(wheelAngle, 0, 0);
            if (handlebar != null) handlebar.localRotation = Quaternion.Euler(0, Input.Steering * 20, 0);
            impactCooldown -= dt;
            if (transform.position.y < -8 || transform.position.magnitude > 700) ResetToSpawn(true);
        }

        public void ResetToSpawn(bool penalize)
        {
            Body.position = Spawn; Body.rotation = Quaternion.identity;
            Body.linearVelocity = Vector3.zero; Body.angularVelocity = Vector3.zero;
            previousSpeed = yawRate = 0; Engine.Reset();
            if (penalize && Incident != null) Incident("Moto reposicionada", 5);
        }

        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.layer == 8 || impactCooldown > 0 || other.relativeVelocity.magnitude < 1.5f) return;
            impactCooldown = 2;
            Incident?.Invoke("Colisão com " + other.gameObject.name, 8);
        }
    }
}
