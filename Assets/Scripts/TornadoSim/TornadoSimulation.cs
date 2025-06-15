using System;
using Compute;
using MoreGizmos;
using Simulation;
using TornadoSim.Settings;
using Unity.Mathematics;
using UnityEngine;
using TMath = TornadoSim.Tornado.Math;

namespace TornadoSim
{
    public class TornadoSimulation : ParticleSimulation
    {
        static readonly int Positions = Shader.PropertyToID("Positions");
        static readonly int Velocities = Shader.PropertyToID("Velocities");
        static readonly int LiftSpans = Shader.PropertyToID("LiftSpans");
        static readonly int NumParticles = Shader.PropertyToID("numParticles");
        static readonly int Pressures = Shader.PropertyToID("Pressures");
        static readonly int AirDensities = Shader.PropertyToID("AirDensities");
        static readonly int MaxWindSpeeds = Shader.PropertyToID("MaxWindSpeeds");
        static readonly int MaxPressureDeficits = Shader.PropertyToID("MaxPressureDeficits");
        static readonly int TornadoRadii = Shader.PropertyToID("TornadoRadii");
        static readonly int GroundTemperatureKelvin = Shader.PropertyToID("GroundTemperatureKelvin");
        static readonly int DewPoint = Shader.PropertyToID("DewPoint");
        static readonly int EnvWind = Shader.PropertyToID("EnvWind");
        static readonly int TornadoHeight = Shader.PropertyToID("TornadoHeight");
        static readonly int StormTop = Shader.PropertyToID("StormTop");
        static readonly int Pcf = Shader.PropertyToID("pcf");
        static readonly int LiftCoefficient = Shader.PropertyToID("LiftCoefficient");
        static readonly int WindCoefficient = Shader.PropertyToID("WindCoefficient");
        static readonly int DragCoefficient = Shader.PropertyToID("DragCoefficient");
        static readonly int PressureGradientCoefficient = Shader.PropertyToID("PressureGradientCoefficient");
        static readonly int WindShearStrength = Shader.PropertyToID("WindShearStrength");
        static readonly int TornadoPos = Shader.PropertyToID("Tornado");
        static readonly int DespawnTime = Shader.PropertyToID("DespawnTime");
        static readonly int SpawnArea = Shader.PropertyToID("SpawnArea");
        static readonly int DeltaTime = Shader.PropertyToID("DeltaTime");
        static readonly int RunMode = Shader.PropertyToID("RunMode");

        public RunModes runMode;
        
        public EnvironmentSettings env = new();
        public CoefficientsSettings coefficients = new();
        public TornadoSettings tornado = new();
        public DespawnSettings despawnSettings = new();
        
        ComputeShader _simulation;
        ComputeBuffer _liftSpanBuffer;
        ComputeBuffer _pressuresBuffer;
        ComputeBuffer _airDensitiesBuffer;
        ComputeBuffer _maxWindSpeedsBuffer;
        ComputeBuffer _maxPressureDeficitsBuffer;
        ComputeBuffer _tornadoRadiiBuffer;
        
        void CreateEnvironmentCache()
        {
            _pressuresBuffer?.Release();
            _airDensitiesBuffer?.Release();
            _maxWindSpeedsBuffer?.Release();
            _maxPressureDeficitsBuffer?.Release();
            _tornadoRadiiBuffer?.Release();
            
            int height = (int)math.ceil(env.TornadoHeight);
            
            float[] pressures = new float[height];
            float[] airDensities = new float[height];
            float[] maxWindSpeeds = new float[height];
            float[] maxPressureDeficits = new float[height];
            float[] tornadoRadii = new float[height];
            
            for (int i = 0; i < height; i++)
            {
                float altitudinalTemperature = TMath.TemperatureAtAltitude(i, env.GroundTemperatureKelvin);
                float altitudinalDewPoint = TMath.DewPointAtAltitude(i, env.dewPoint);
                float pressure = TMath.AltitudeToPressure(i, env.GroundTemperatureKelvin);
                float airDensity = TMath.AirDensity(altitudinalTemperature, altitudinalDewPoint, pressure);
                float maxWindSpeed = TMath.MaxWindSpeed(pressure, airDensity, env.CoreFunnelPressure);
                float maxPressuresDeficit = TMath.MaxPressureDeficit(maxWindSpeed, airDensity);
                float radius = TMath.CoreRadius(pressure, env.CoreFunnelPressure, maxPressuresDeficit, maxWindSpeed, env.GroundTemperatureKelvin);
                
                pressures[i] = pressure;
                airDensities[i] = airDensity;
                maxWindSpeeds[i] = maxWindSpeed;
                maxPressureDeficits[i] = maxPressuresDeficit;
                tornadoRadii[i] = radius;
            }
            
            _pressuresBuffer = ComputeHelper.CreateStructuredBuffer(pressures);
            _airDensitiesBuffer = ComputeHelper.CreateStructuredBuffer(airDensities);
            _maxWindSpeedsBuffer = ComputeHelper.CreateStructuredBuffer(maxWindSpeeds);
            _maxPressureDeficitsBuffer = ComputeHelper.CreateStructuredBuffer(maxPressureDeficits);
            _tornadoRadiiBuffer = ComputeHelper.CreateStructuredBuffer(tornadoRadii);
            
            _simulation.SetBuffer(0, Pressures, _pressuresBuffer);
            _simulation.SetBuffer(0, AirDensities, _airDensitiesBuffer);
            _simulation.SetBuffer(0, MaxWindSpeeds, _maxWindSpeedsBuffer);
            _simulation.SetBuffer(0, MaxPressureDeficits, _maxPressureDeficitsBuffer);
            _simulation.SetBuffer(0, TornadoRadii, _tornadoRadiiBuffer);
        }

        void CreateLifespanBuffer()
        {
            int[] lifeSpans = new int[ParticleCount];
            for (int i = 0; i < ParticleCount; i++) lifeSpans[i] = -1;
            _liftSpanBuffer = ComputeHelper.CreateStructuredBuffer(lifeSpans);
        }

        void SetSimulationBuffers()
        {
            _simulation.SetBuffer(0, Positions, PositionBuffer);
            _simulation.SetBuffer(0, Velocities, VelocityBuffer);
            _simulation.SetBuffer(0, LiftSpans, _liftSpanBuffer);
            
            _simulation.SetInt(NumParticles, ParticleCount);
        }

        void UpdateEnvironmentSettings()
        {
            _simulation.SetFloat(GroundTemperatureKelvin, env.GroundTemperatureKelvin);
            _simulation.SetFloat(DewPoint,  env.dewPoint);
            _simulation.SetFloat3(EnvWind, env.wind);
            _simulation.SetFloat(TornadoHeight, env.TornadoHeight);
            _simulation.SetFloat(StormTop, env.StormTop);
            _simulation.SetFloat(Pcf, env.CoreFunnelPressure);
        }
        
        void UpdateCoefficientsSettings()
        {
            _simulation.SetFloat(LiftCoefficient, coefficients.liftCoefficient);
            _simulation.SetFloat(WindCoefficient, coefficients.windCoefficient);
            _simulation.SetFloat(DragCoefficient, coefficients.dragCoefficient);
            _simulation.SetFloat(PressureGradientCoefficient, coefficients.pressureGradientCoefficient);
            _simulation.SetFloat(WindShearStrength, coefficients.windShearStrength);
        }
        
        void UpdateTornadoSettings()
        {
            _simulation.SetFloat2(TornadoPos, tornado.position);
        }
        
        void UpdateDespawnSettings()
        {
            _simulation.SetInt(DespawnTime, despawnSettings.enableDespawning ? (int)despawnSettings.despawnTime : -1);
            _simulation.SetFloat(SpawnArea, Spawner.size);
        }

        protected override void SimulationSettingsLoad(Action<SmartSettings.Settings> settingsAdd)
        {
            settingsAdd(env);
            settingsAdd(coefficients);
            settingsAdd(tornado);
            settingsAdd(despawnSettings);
        }

        protected override void SimulationShaderLoad()
        {
            ComputeHelper.LoadShader(ref _simulation, "TornadoSim");
        }

        protected override void SimulationInitialise()
        {
            env.SubscribeToValue(CreateEnvironmentCache, "temperature", "dewPoint");
            
            env.OnChange += UpdateEnvironmentSettings;
            coefficients.OnChange += UpdateCoefficientsSettings;
            tornado.OnChange += UpdateTornadoSettings;
            despawnSettings.OnChange += UpdateDespawnSettings;
        }

        protected override void SimulationStart()
        {
            CreateEnvironmentCache();
            CreateLifespanBuffer();
            
            SetSimulationBuffers();
            
            UpdateEnvironmentSettings();
            UpdateCoefficientsSettings();
            UpdateTornadoSettings();
            UpdateDespawnSettings();
        }

        protected override void SimulationUpdate(float dt)
        {
            tornado.MoveTornado(Prng.NextFloat2Direction() * dt, math.normalizesafe(env.wind.xz) * dt);
            
            // Run time
            _simulation.SetFloat(DeltaTime, dt);
            
            // Caching
            _simulation.SetInt(RunMode, (int)runMode);
        }

        protected override void SimulationStep()
        {
            _simulation.Run(0, ParticleCount);
        }

        protected override void SimulationEnd()
        {
            _liftSpanBuffer?.Release();
            _pressuresBuffer?.Release();
            _airDensitiesBuffer?.Release();
            _maxWindSpeedsBuffer?.Release();
            _maxPressureDeficitsBuffer?.Release();
            _tornadoRadiiBuffer?.Release();
        }
        
        void OnDrawGizmos()
        {
            if (Application.isPlaying) return;
            
            // draw tornado
            for (int i = 0; i < env.TornadoHeight; i++)
            {
                float altitudinalTemperature = TMath.TemperatureAtAltitude(i, env.GroundTemperatureKelvin);
                float altitudinalDewPoint = TMath.DewPointAtAltitude(i, env.dewPoint);
                float pressure = TMath.AltitudeToPressure(i, env.GroundTemperatureKelvin);
                float airDensity = TMath.AirDensity(altitudinalTemperature, altitudinalDewPoint, pressure);
                float maxWindSpeed = TMath.MaxWindSpeed(pressure, airDensity, env.CoreFunnelPressure);
                float maxPressuresDeficit = TMath.MaxPressureDeficit(maxWindSpeed, airDensity);
                float radius = TMath.CoreRadius(pressure, env.CoreFunnelPressure, maxPressuresDeficit, maxWindSpeed, env.GroundTemperatureKelvin);
                float3 color = math.lerp(new float3(1, 0.5f, 0), new float3(0, 0, 1), i / env.TornadoHeight);
                Gizmos.color = new Color(color.x, color.y, color.z);

                GizmosExtra.DrawWireCircle(new Vector3(tornado.position.x, i, tornado.position.y), radius);
            }
        }
    }
}