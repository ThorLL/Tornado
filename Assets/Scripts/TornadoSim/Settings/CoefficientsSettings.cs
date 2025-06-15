namespace TornadoSim.Settings
{
    [System.Serializable]
    public class CoefficientsSettings : SmartSettings.Settings
    {
        public float liftCoefficient = 0.125f;
        public float windCoefficient = 5f;
        public float dragCoefficient = 0.5f;
        public float pressureGradientCoefficient = 64f;
        public float windShearStrength = 0.14f;
    }
}