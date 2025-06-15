using Unity.Mathematics;
using UnityEngine;

namespace TornadoSim.Settings
{
    [System.Serializable]
    public class EnvironmentSettings : SmartSettings.Settings
    {
        [Range(0f, 90f)] public float temperature = 21f;
        [Range(0f, 90f)] public float dewPoint = 19f;
        public float3 wind = new(-2, -1, -2);

        public float GroundTemperatureKelvin{get; private set; }
        public float CoreFunnelPressure{get; private set; }
        public float GroundAirDensity{get; private set; }
        public float MaxWindSpeed{get; private set; }
        public float MaxPressuresDeficit{get; private set; }
        public float TornadoHeight{get; private set; }
        public float StormTop{get; private set; }

        public override void Prepare() => CalculateEnvironmentProperties();

        protected override void PropertyChanged()
        {
            ValidateTemps();
            CalculateEnvironmentProperties();
        }

        void ValidateTemps()
        {
            temperature = Mathf.Clamp(temperature, dewPoint + 0.1f, 90f);
            dewPoint = Mathf.Clamp(dewPoint, 0f, temperature - 0.1f);
        }
        
        void CalculateEnvironmentProperties()
        {
            GroundTemperatureKelvin = Tornado.Math.CToKelvin(temperature);
            CoreFunnelPressure = Tornado.Math.CoreFunnelPressure(GroundTemperatureKelvin, dewPoint);
            GroundAirDensity = Tornado.Math.AirDensity(GroundTemperatureKelvin, dewPoint, Tornado.Math.AtmosphericPressure);
            MaxWindSpeed = Tornado.Math.MaxWindSpeed(Tornado.Math.AtmosphericPressure, GroundAirDensity, CoreFunnelPressure);
            MaxPressuresDeficit = Tornado.Math.MaxPressureDeficit(MaxWindSpeed, GroundAirDensity);
            
            TornadoHeight = Tornado.Math.PressureToAltitude(CoreFunnelPressure, GroundTemperatureKelvin);
            StormTop = Tornado.Math.PressureToAltitude(CoreFunnelPressure - MaxPressuresDeficit / 2, GroundTemperatureKelvin);
        }
    }
}