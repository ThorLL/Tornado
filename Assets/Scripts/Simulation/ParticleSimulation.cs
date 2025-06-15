using System;
using System.Collections.Generic;
using Compute;
using Simulation.Spawners;
using SmartSettings;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = Unity.Mathematics.Random;

namespace Simulation
{
    #if UNITY_EDITOR

    [InitializeOnLoad]
    public static class DomainReloadHandler
    {
        static DomainReloadHandler()
        {
            ParticleSimulation sim = Object.FindFirstObjectByType<ParticleSimulation>();
            sim?.DomainLoad();
        }
    }
    #endif
    
    [RequireComponent(typeof(Spawner))]
    public abstract class ParticleSimulation : MonoBehaviour
    {
        public uint seed = 1;
        
        public bool pause;
        public float maxTimestepFPS = 60;
        public int iterationsPerFrame = 3;
        public bool slowMode;
        public float slowTimeScale = 0.1f;
        public float normalTimeScale = 1f;
    
        float ActiveTimeScale => slowMode ? slowTimeScale : normalTimeScale;

        public int ParticleCount { get; private set; }
        public ComputeBuffer PositionBuffer {get; private set; }
        public ComputeBuffer VelocityBuffer {get; private set; }

        protected Spawner Spawner;
        protected Random Prng;

        readonly List<Settings> _settings = new();

        public void DomainLoad() => First();

        void First()
        {
            Prng = new Random(seed);
            Spawner = FindFirstObjectByType<Spawner>();
            
            _settings.Clear();
            SimulationSettingsLoad(_settings.Add);
            foreach (Settings setting in _settings) setting.Init();

            SimulationShaderLoad();
            SimulationInitialise();
        }
        
        void Start()
        {
            First();
            Init();
            SimulationStart();
        }

        void Init()
        {
            PositionBuffer?.Release();
            VelocityBuffer?.Release();
            
            (float3[] points, float3[] velocities) = Spawner.GetSpawnData();
            
            PositionBuffer = ComputeHelper.CreateStructuredBuffer(points);
            VelocityBuffer = ComputeHelper.CreateStructuredBuffer(velocities);

            ParticleCount = points.Length;
        }
        
        public void Restart()
        {
            foreach (Settings settings in _settings) settings.Reset();
            Init();
            SimulationStart();
        }

        void Update()
        {
            if (pause) return;

            float maxDeltaTime = maxTimestepFPS > 0 ? 1 / maxTimestepFPS : float.PositiveInfinity; // If framerate dips too low, run the simulation slower than real-time
            float dt = Mathf.Min(Time.deltaTime, maxDeltaTime) * ActiveTimeScale;
            
            SimulationUpdate(dt);
            
            for (int i = 0; i < iterationsPerFrame; i++) SimulationStep();
        }

        void OnApplicationQuit()
        {
            PositionBuffer?.Release();
            VelocityBuffer?.Release();
            foreach (Settings settings in _settings) settings.Drop();
            SimulationEnd();
        }

        protected abstract void SimulationSettingsLoad(Action<Settings> settingsAdd);
        protected abstract void SimulationShaderLoad();
        protected abstract void SimulationInitialise();
        protected abstract void SimulationStart();
        protected abstract void SimulationUpdate(float dt);
        protected abstract void SimulationStep();
        protected abstract void SimulationEnd();
    }
}