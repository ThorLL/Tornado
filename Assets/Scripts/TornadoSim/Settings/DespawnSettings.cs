namespace TornadoSim.Settings
{
    [System.Serializable]
    public class DespawnSettings : SmartSettings.Settings
    {
        public bool enableDespawning;
        public uint despawnTime = 3000;
    }
}