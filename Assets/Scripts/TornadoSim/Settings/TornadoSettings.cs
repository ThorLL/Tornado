using Unity.Mathematics;

namespace TornadoSim.Settings
{
    [System.Serializable]
    public class TornadoSettings : SmartSettings.Settings
    {
        public float2 position; 
        public float tornadoWindShear = 1f;
        public float tornadoWindShearJitter = 0.01f;

        public void MoveTornado(float2 jitterDir, float2 windDir)
        {
            position += tornadoWindShearJitter * jitterDir + tornadoWindShear * windDir;
            InvokeChange();
        }
    }
}