using UnityEngine;
using Compute;
using Unity.Mathematics;
using TMath = Tornado.Math;

namespace Simulation
{
    public class TornadoSim : MonoBehaviour
    {
        [Header("Runtime")]
        public float maxTimestepFPS = 60;
        
        [Header("Environment")]
        [Range(0f, 90f)] public float temperature = 15;
        [Range(0f, 90f)] public float dewPoint = 13.5f;

        [Header("Coefficients")]
        public float liftCoefficient = 0.125f;
        public float windCoefficient = 5f;
        public float dragCoefficient = 0.5f;
        public float pressureGradientCoefficient = 64f;
        
        [Header("Tornado")]
        public float2 tornado;
        
        [Header("References")]
        public Spawner spawner;
        ComputeShader _simulation;

        public ComputeBuffer PositionBuffer;
        public ComputeBuffer VelocityBuffer;
        
        void Start()
        {
            ComputeHelper.LoadShader(ref _simulation, "TornadoSim");
            Initialise();
        }

        void Initialise()
        {
            if (!spawner) return;

            PositionBuffer?.Release();
            VelocityBuffer?.Release();
            
            (float3[] points, float3[] velocities) = spawner.GetSpawnData();
            PositionBuffer = ComputeHelper.CreateStructuredBuffer(points);
            VelocityBuffer = ComputeHelper.CreateStructuredBuffer(velocities);

            _simulation.SetBuffer(0, "Positions", PositionBuffer);
            _simulation.SetBuffer(0, "Velocities", VelocityBuffer);
            
            _simulation.SetInt("numParticles", PositionBuffer.count);
            
            UpdateSettings(maxTimestepFPS > 0 ? 1 / maxTimestepFPS : math.EPSILON);
        }
        
        void Update()
        {
            float maxDeltaTime = maxTimestepFPS > 0 ? 1 / maxTimestepFPS : float.PositiveInfinity; // If framerate dips too low, run the simulation slower than real-time
            float dt = Mathf.Min(Time.deltaTime, maxDeltaTime);
            UpdateSettings(dt);
            RunSimulationFrame();
        }
        
        void UpdateSettings(float stepDeltaTime)
        {
            // Precompute static tornado values
            float groundTemperatureKelvin = TMath.CToKelvin(temperature);
            float coreFunnelPressure = TMath.CoreFunnelPressure(groundTemperatureKelvin, dewPoint);
            float groundAirDensity = TMath.AirDensity(groundTemperatureKelvin, dewPoint, TMath.AtmosphericPressure);
            float maxWindSpeed = TMath.MaxWindSpeed(TMath.AtmosphericPressure, groundAirDensity, coreFunnelPressure);
            float maxPressuresDeficit = TMath.MaxPressureDeficit(maxWindSpeed, groundAirDensity);
            
            float tornadoHeight = TMath.PressureToAltitude(coreFunnelPressure, groundTemperatureKelvin);
            float stormTop = TMath.PressureToAltitude(coreFunnelPressure - maxPressuresDeficit / 2, groundTemperatureKelvin);

            // Run time
            _simulation.SetFloat("DeltaTime", stepDeltaTime);
            
            // Environment
            _simulation.SetFloat("GroundTemperatureKelvin", groundTemperatureKelvin);
            _simulation.SetFloat("DewPoint", dewPoint);
            
            // Coefficients
            _simulation.SetFloat("LiftCoefficient", liftCoefficient);
            _simulation.SetFloat("WindCoefficient", windCoefficient);
            _simulation.SetFloat("DragCoefficient", dragCoefficient);
            _simulation.SetFloat("PressureGradientCoefficient", pressureGradientCoefficient);

            // Tornado
            _simulation.SetFloats("Tornado", tornado.x, tornado.y);
            
            // Precomputed tornado properties
            _simulation.SetFloat("TornadoHeight", tornadoHeight);
            _simulation.SetFloat("StormTop", stormTop);
            _simulation.SetFloat("pcf", coreFunnelPressure);
        }
        
        void RunSimulationFrame()
        {
            _simulation.Run(0, PositionBuffer.count);
        }

        void OnDrawGizmos()
        {
            if (Application.isPlaying) return;

            float groundTemperatureKelvin = TMath.CToKelvin(temperature);
            float coreFunnelPressure = TMath.CoreFunnelPressure(groundTemperatureKelvin, dewPoint);
            float tornadoHeight = TMath.PressureToAltitude(coreFunnelPressure, groundTemperatureKelvin);

            // draw tornado
            for (int i = 0; i < tornadoHeight; i++)
            {
                float altitudinalTemperature = TMath.TemperatureAtAltitude(i, groundTemperatureKelvin);
                float altitudinalDewPoint = TMath.DewPointAtAltitude(i, dewPoint);
                float pressure = TMath.AltitudeToPressure(i, groundTemperatureKelvin);
                float airDensity = TMath.AirDensity(altitudinalTemperature, altitudinalDewPoint, pressure);
                float maxWindSpeed = TMath.MaxWindSpeed(pressure, airDensity, coreFunnelPressure);
                float maxPressuresDeficit = TMath.MaxPressureDeficit(maxWindSpeed, airDensity);
                float radius = TMath.CoreRadius(pressure, coreFunnelPressure, maxPressuresDeficit, maxWindSpeed, groundTemperatureKelvin);
                var color = math.lerp(new float3(1, 0.5f, 0), new float3(0, 0, 1), i / tornadoHeight);
                Gizmos.color = new Color(color.x, color.y, color.z);

                GizmosExtra.DrawWireCircle(new Vector3(tornado.x, i, tornado.y), radius);
            }
        }
        
        void OnDestroy()
        {
            PositionBuffer.Release();
            VelocityBuffer.Release();
        }
    }
}