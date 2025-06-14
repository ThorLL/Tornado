// universal constants
static const float GasConstant = 8.31436f; // (m³•Pa•K⁻¹•mol⁻¹)

// earthly constants
static const float Gravity = 9.80665f; // (m/s²)
static const float AtmosphericPressure = 101325.0f; // (Pa)

// gas constants
static const float DryAirMolarMass = 0.0289644f; // (kg/mol)
static const float WaterVaporMolarMass = 0.01802f; // (kg/mol)

static const float TemperatureLapseRate = 0.0065f; // (C°/m)
static const float TemperatureDewLapseRate = 0.0018f; // (C°/m)

static const float AirGasConstant = GasConstant / DryAirMolarMass; // (J•kg⁻¹•K⁻¹)
static const float VaporGasConstant = GasConstant / WaterVaporMolarMass; // (J•kg⁻¹•K⁻¹)

static const float DryAirHeatCapacity = 1003.5f; // (J/(kg•K))
static const float DryAirHeatCapacityRatio = DryAirHeatCapacity / AirGasConstant; // (dimensionless)

// inferred gas constants
static const float PressureExponent = Gravity * DryAirMolarMass / (GasConstant * TemperatureLapseRate); // (dimensionless)
static const float InvPressureExponent = 1.0f / PressureExponent; // (dimensionless)

// https://doi.org/10.1175/1520-0450(1967)006%3C0203:OTCOSV%3E2.0.CO;2
static const float ReferenceVaporPressure = 610.78f; // (Pa)
static const float SaturationVaporPressureGrowthFactor = 21.875f; // (dimensionless)
static const float VaporNonLinearityOffset = 265.5f; // (C°)