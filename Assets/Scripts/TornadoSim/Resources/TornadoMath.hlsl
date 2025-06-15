#include "Assets/Scripts/TornadoSim/Resources/TornadoConstants.hlsl"

float CToKelvin(float c) { return c + 273.13f; }

float TemperatureAtAltitude(float altitude, float groudTemperature)
{
    return groudTemperature - TemperatureLapseRate * altitude;
}

float DewPointAtAltitude(float altitude, float groundTempDewPoint)
{
    return groundTempDewPoint - TemperatureDewLapseRate * altitude;
}
        
float AltitudeToPressure(float altitude, float groundTemperature)
{
    float f = 1.0f - TemperatureLapseRate * altitude/groundTemperature;
    return AtmosphericPressure * pow(abs(f), PressureExponent);
}

// Tetens' equation
float VaporPressure(float altitudinalDewPoint)
{
    return ReferenceVaporPressure * exp(
        SaturationVaporPressureGrowthFactor * altitudinalDewPoint /
        (altitudinalDewPoint + VaporNonLinearityOffset)
    );
}
float AirDensity(float altitudinalTemperature, float altitudinalDewPoint, float absolutePressure)
{
    float vaporPressure = VaporPressure(altitudinalDewPoint);
    float dryAirPressure = absolutePressure - vaporPressure;

    float vaporDensity = vaporPressure / (VaporGasConstant * altitudinalTemperature);
    float dryAirDensity = dryAirPressure / (AirGasConstant * altitudinalTemperature);
    return dryAirDensity + vaporDensity;
}

float CoreFunnelPressure(float groundTemp, float groundDewPoint)
{
    float airDensity = AirDensity(groundTemp, groundDewPoint, AtmosphericPressure);
    return AtmosphericPressure * pow(1.0f - airDensity * (groundTemp - CToKelvin(groundDewPoint)) / groundTemp, DryAirHeatCapacityRatio);
}

float PressureToAltitude(float pressure, float groundTemperature)
{
    return (1.0f - pow(abs(pressure) / AtmosphericPressure, InvPressureExponent)) * groundTemperature / TemperatureLapseRate;
}

float MaxWindSpeed(float altitudinalPressure, float altitudinalAirDensity, float coreFunnelPressure)
{
    return sqrt((altitudinalPressure - coreFunnelPressure) / altitudinalAirDensity);
}

float WindSpeed(float maxWindSpeed, float radio) { return maxWindSpeed * radio; }

float MaxPressureDeficit(float maxWindSpeed, float altitudinalAirDensity)
{
    return altitudinalAirDensity * maxWindSpeed * maxWindSpeed;
}

float PressureDeficit(float maxPressureDeficit, float ratio, bool insideCore)
{
    ratio *= ratio;
    ratio *= 0.5f;
    if (insideCore) ratio = 1.0f - ratio;
    return maxPressureDeficit * ratio;
}

float CoreRadius(float altitudinalPressure, float pcf, float maxAltitudinalPressureDeficit, float maxAltitudinalWindSpeed, float groundTemperature)
{
    return sqrt(2 * (pcf + maxAltitudinalPressureDeficit) / altitudinalPressure) /
        maxAltitudinalWindSpeed * PressureToAltitude(pcf - 0.5f * maxAltitudinalPressureDeficit, groundTemperature);
}
