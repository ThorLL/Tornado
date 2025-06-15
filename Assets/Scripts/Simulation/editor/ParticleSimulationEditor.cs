using UnityEditor;
using UnityEngine;

namespace Simulation.editor
{
    [CustomEditor(typeof(ParticleSimulation), true)]
    public class ParticleSimulationEditor : Editor
    {
        ParticleSimulation _sim;

        void OnEnable()
        {
            _sim = (ParticleSimulation)target;
        }

        public override void OnInspectorGUI()
        {
            if(Application.IsPlaying(_sim) && GUILayout.Button("Reset")) _sim.Restart();
            base.OnInspectorGUI();
        }
    }
}