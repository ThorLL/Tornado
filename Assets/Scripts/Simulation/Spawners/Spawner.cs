using Unity.Mathematics;
using UnityEngine;

namespace Simulation.Spawners
{
    public abstract class Spawner : MonoBehaviour
    {
        public abstract (float3[] Points, float3[] Velocities) GetSpawnData();
        public float size;
    }
}