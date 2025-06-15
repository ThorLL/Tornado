using UnityEngine;

namespace Simulation.Renderers
{
    [RequireComponent(typeof(ParticleSimulation))]
    public abstract class ParticleRenderer : MonoBehaviour
    {
        protected ParticleSimulation Sim;

        void OnEnable()
        {
            Sim = FindFirstObjectByType<ParticleSimulation>();
            Enabled();
        }
        
        protected virtual void Enabled() { }
    }
}