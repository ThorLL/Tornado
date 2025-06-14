using UnityEngine;
using Compute;
using Unity.Mathematics;
using TMath = Tornado.Math;

namespace Simulation
{
    public class TornadoSim : MonoBehaviour
    {
        [Header("General")] 
        public uint seed = 1;
        public bool restart;

        [Header("Runtime")] 
        public RunModes runMode;
        public bool pause;
        public float maxTimestepFPS = 60;
        public int iterationsPerFrame = 3;
        public bool slowMode;
        public float slowTimeScale = 0.1f;
        public float normalTimeScale = 1f;
    
        float ActiveTimeScale => slowMode ? slowTimeScale : normalTimeScale;
        
        [Header("Environment")]
        [Range(0f, 90f)] public float temperature = 15;
        [Range(0f, 90f)] public float dewPoint = 13.5f;
        public float3 wind = new(-2, -1, -2);

        [Header("Coefficients")]
        public float liftCoefficient = 0.125f;
        public float windCoefficient = 5f;
        public float dragCoefficient = 0.5f;
        public float pressureGradientCoefficient = 64f;
        public float windShearStrength = 0.14f;
        
        [Header("Tornado")]
        public float2 tornado; float2 _initialTornado;
        [Range(0f, 1f)] public float tornadoWindShear = 1f;
        [Range(0f, 1f)] public float tornadoWindShearJitter = 0.01f;
        
        [Header("Despawn")]
        public int despawnTime = -1; // TODO: Add a check box to enable or disable instead of having negative for off

        [Header("References")]
        public Spawner spawner;
        ComputeShader _simulation;

        public ComputeBuffer PositionBuffer;
        public ComputeBuffer VelocityBuffer;
        public ComputeBuffer LiftSpanBuffer;
        
        // Caching buffers
        public ComputeBuffer PressuresBuffer;
        public ComputeBuffer AirDensitiesBuffer;
        public ComputeBuffer MaxWindSpeedsBuffer;
        public ComputeBuffer MaxPressureDeficitsBuffer;
        public ComputeBuffer TornadoRadiiBuffer;

        bool _cacheNeedsUpdating;
        float _lastTemp, _lastDewPoint;
        
        Unity.Mathematics.Random _prng;
        
        void Start()
        {
            _initialTornado = tornado;
            ComputeHelper.LoadShader(ref _simulation, "TornadoSim");
            Initialise();
        }

        void Initialise()
        {
            _prng = new (seed);
            if (!spawner) return;

            PositionBuffer?.Release();
            VelocityBuffer?.Release();
            LiftSpanBuffer?.Release();
            
            (float3[] points, float3[] velocities) = spawner.GetSpawnData();
            int[] lifeSpans = new int[points.Length];
            for (int i = 0; i < points.Length; i++) lifeSpans[i] = -1;
            
            PositionBuffer = ComputeHelper.CreateStructuredBuffer(points);
            VelocityBuffer = ComputeHelper.CreateStructuredBuffer(velocities);
            LiftSpanBuffer = ComputeHelper.CreateStructuredBuffer(lifeSpans);

            _simulation.SetBuffer(0, "Positions", PositionBuffer);
            _simulation.SetBuffer(0, "Velocities", VelocityBuffer);
            _simulation.SetBuffer(0, "LiftSpans", LiftSpanBuffer);
            
            _simulation.SetInt("numParticles", PositionBuffer.count);
            CreateEnvironmentCache();
            UpdateSettings(maxTimestepFPS > 0 ? 1 / maxTimestepFPS : math.EPSILON);
        }
        
        void CreateEnvironmentCache()
        {
            TMath.TornadoDetails(temperature, dewPoint, 
                out float groundTemperatureKelvin,
                out float coreFunnelPressure,
                out float tornadoHeight,
                out float _
            );
            int height = (int)math.ceil(tornadoHeight);
            
            float[] pressures = new float[height];
            float[] airDensities = new float[height];
            float[] maxWindSpeeds = new float[height];
            float[] maxPressureDeficits = new float[height];
            float[] tornadoRadii = new float[height];
            
            for (int i = 0; i < height; i++)
            {
                float altitudinalTemperature = TMath.TemperatureAtAltitude(i, groundTemperatureKelvin);
                float altitudinalDewPoint = TMath.DewPointAtAltitude(i, dewPoint);
                float pressure = TMath.AltitudeToPressure(i, groundTemperatureKelvin);
                float airDensity = TMath.AirDensity(altitudinalTemperature, altitudinalDewPoint, pressure);
                float maxWindSpeed = TMath.MaxWindSpeed(pressure, airDensity, coreFunnelPressure);
                float maxPressuresDeficit = TMath.MaxPressureDeficit(maxWindSpeed, airDensity);
                float radius = TMath.CoreRadius(pressure, coreFunnelPressure, maxPressuresDeficit, maxWindSpeed, groundTemperatureKelvin);
                
                pressures[i] = pressure;
                airDensities[i] = airDensity;
                maxWindSpeeds[i] = maxWindSpeed;
                maxPressureDeficits[i] = maxPressuresDeficit;
                tornadoRadii[i] = radius;
            }
            
            PressuresBuffer = ComputeHelper.CreateStructuredBuffer(pressures);
            AirDensitiesBuffer = ComputeHelper.CreateStructuredBuffer(airDensities);
            MaxWindSpeedsBuffer = ComputeHelper.CreateStructuredBuffer(maxWindSpeeds);
            MaxPressureDeficitsBuffer = ComputeHelper.CreateStructuredBuffer(maxPressureDeficits);
            TornadoRadiiBuffer = ComputeHelper.CreateStructuredBuffer(tornadoRadii);
            
            _simulation.SetBuffer(0, "Pressures", PressuresBuffer);
            _simulation.SetBuffer(0, "AirDensities", AirDensitiesBuffer);
            _simulation.SetBuffer(0, "MaxWindSpeeds", MaxWindSpeedsBuffer);
            _simulation.SetBuffer(0, "MaxPressureDeficits", MaxPressureDeficitsBuffer);
            _simulation.SetBuffer(0, "TornadoRadii", TornadoRadiiBuffer);
        }

        void UpdateCache()
        {
            if (!Mathf.Approximately(_lastTemp, temperature) || !Mathf.Approximately(_lastDewPoint, dewPoint))
            {
                _lastTemp = temperature;
                _lastDewPoint = dewPoint;
                _cacheNeedsUpdating = true;
            }

            if (_cacheNeedsUpdating && runMode is RunModes.Cached)
            {
                CreateEnvironmentCache();
                _cacheNeedsUpdating = false;
            }
        }
        
        void Update()
        {
            if (restart) // TODO: Change restart to a button
            {
                tornado = _initialTornado;
                Initialise();
                restart = false;
            }

            if (pause) return;

            float maxDeltaTime = maxTimestepFPS > 0 ? 1 / maxTimestepFPS : float.PositiveInfinity; // If framerate dips too low, run the simulation slower than real-time
            float dt = Mathf.Min(Time.deltaTime, maxDeltaTime);
            dt *= ActiveTimeScale;
            UpdateSettings(dt / iterationsPerFrame);
            RunSimulationFrame();
        }
        
        void UpdateSettings(float stepDeltaTime)
        {
            // Precompute static tornado values
            TMath.TornadoDetails(temperature, dewPoint, 
                out float groundTemperatureKelvin,
                out float coreFunnelPressure,
                out float tornadoHeight,
                out float stormTop
            );
            // Run time
            _simulation.SetFloat("DeltaTime", stepDeltaTime);
            
            // Caching
            _simulation.SetInt("RunMode", (int)runMode);
            UpdateCache();
            
            // Environment
            _simulation.SetFloat("GroundTemperatureKelvin", groundTemperatureKelvin);
            _simulation.SetFloat("DewPoint", dewPoint);
            _simulation.SetFloats("EnvWind", wind.x, wind.y, wind.z);

            // Coefficients
            _simulation.SetFloat("LiftCoefficient", liftCoefficient);
            _simulation.SetFloat("WindCoefficient", windCoefficient);
            _simulation.SetFloat("DragCoefficient", dragCoefficient);
            _simulation.SetFloat("PressureGradientCoefficient", pressureGradientCoefficient);
            _simulation.SetFloat("WindShearStrength", windShearStrength);

            // Tornado
            tornado += (tornadoWindShearJitter * _prng.NextFloat2Direction() + tornadoWindShear * math.normalizesafe(wind.xz)) * stepDeltaTime;
            _simulation.SetFloats("Tornado", tornado.x, tornado.y);
            
            // Precomputed tornado properties
            _simulation.SetFloat("TornadoHeight", tornadoHeight);
            _simulation.SetFloat("StormTop", stormTop);
            _simulation.SetFloat("pcf", coreFunnelPressure);
            
            // Despawn
            _simulation.SetInt("DespawnTime", despawnTime);
            _simulation.SetFloat("SpawnArea", spawner.size);
        }
        
        void RunSimulationFrame()
        {
            for (int i = 0; i < iterationsPerFrame; i++)
            {
                _simulation.Run(0, PositionBuffer.count);
            }
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

        void OnValidate()
        {
            if (seed == 0) seed = 1; // Unity Random doesn't work with seed == 0
            temperature = math.max(temperature, dewPoint);
            dewPoint = math.min(temperature, dewPoint);

            if (maxTimestepFPS < math.EPSILON) maxTimestepFPS = math.EPSILON;
            if (iterationsPerFrame < 1) iterationsPerFrame = 1;
            if (slowTimeScale < math.EPSILON) slowTimeScale = math.EPSILON;
            if (normalTimeScale < math.EPSILON) normalTimeScale = math.EPSILON;
        }

        void OnDestroy()
        {
            PositionBuffer.Release();
            VelocityBuffer.Release();
            LiftSpanBuffer.Release();
        }

        public enum RunModes
        {
            Dynamic = 0,
            Cached = 1
        }
    }
}