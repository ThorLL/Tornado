using UnityEngine;
using Compute;
using Unity.Mathematics;

namespace Simulation
{
    public class TornadoSim : MonoBehaviour
    {
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
        }

        void OnDestroy()
        {
            PositionBuffer.Release();
            VelocityBuffer.Release();
        }
    }
}