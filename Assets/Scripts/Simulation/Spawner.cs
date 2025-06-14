using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Simulation
{
    public class Spawner : MonoBehaviour
    {
        public int particleSpawnDensity = 600;
        public float3 initialVel;
        public float jitterStrength;
        public bool showSpawnBounds;
        
        public Vector3 centre;
        public float size;
        public Color debugDisplayCol;

        int CalculateParticleCountPerAxis()
        {
            int targetParticleCount = (int)(spawnVolume * particleSpawnDensity);
            int particlesPerAxis = (int)Math.Cbrt(targetParticleCount);
            return particlesPerAxis;
        }
        
        [Header("Debug Info")] 
        public int debugNumParticles;
        public float spawnVolume;
        
        public (float3[] Points, float3[] Velocities) GetSpawnData()
        {
            List<float3> allPoints = new();
            List<float3> allVelocities = new();
            
            int particlesPerAxis = CalculateParticleCountPerAxis();
            (float3[] points, float3[] velocities) = SpawnCube(particlesPerAxis);
            allPoints.AddRange(points);
            allVelocities.AddRange(velocities);

            return (allPoints.ToArray(), allVelocities.ToArray());
        }

        (float3[] p, float3[] v) SpawnCube(int numPerAxis)
        {
            int numPoints = numPerAxis * numPerAxis * numPerAxis;
            float3[] points = new float3[numPoints];
            float3[] velocities = new float3[numPoints];

            int i = 0;

            for (int x = 0; x < numPerAxis; x++)
            {
                for (int y = 0; y < numPerAxis; y++)
                {
                    for (int z = 0; z < numPerAxis; z++)
                    {
                        float tx = x / (numPerAxis - 1f);
                        float ty = y / (numPerAxis - 1f);
                        float tz = z / (numPerAxis - 1f);

                        float px = (tx - 0.5f) * size + centre.x;
                        float py = (ty - 0.5f) * size + centre.y;
                        float pz = (tz - 0.5f) * size + centre.z;
                        float3 jitter = UnityEngine.Random.insideUnitSphere * jitterStrength;
                        points[i] = new float3(px, py, pz) + jitter;
                        velocities[i] = initialVel;
                        i++;
                    }
                }
            }

            return (points, velocities);
        }
        void OnValidate()
        {
            spawnVolume = size * size * size;
            int numPerAxis = CalculateParticleCountPerAxis();
            debugNumParticles = numPerAxis * numPerAxis * numPerAxis;
        }

        void OnDrawGizmos()
        {
            if (!showSpawnBounds || Application.isPlaying) return;
            
            Gizmos.color = debugDisplayCol;
            Gizmos.DrawWireCube(centre, Vector3.one * size);
        }
    }
}