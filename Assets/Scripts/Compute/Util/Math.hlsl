static const float Epsilon = 1.401298E-45f;
static const float MaxFloat = 3.40282347E+38f;
static const float MinFloat = -3.40282347E+38f;

static const float PI_HALF = 1.5707963268f;
static const float PI = 3.1415926536f;
static const float PI_ONE_HALF = 4.7123889804f;
static const float PI2 = 6.2831853072f;

static const float sqrt2 = 1.4142135624f;
static const float halfSqrt2 = 0.7071067812f;

// Modulo 289 without a division (only multiplications)
static float  mod289(float x)  { return x - floor(x * (1.0f / 289.0f)) * 289.0f; }
static float2 mod289(float2 x) { return x - floor(x * (1.0f / 289.0f)) * 289.0f; }
static float3 mod289(float3 x) { return x - floor(x * (1.0f / 289.0f)) * 289.0f; }
static float4 mod289(float4 x) { return x - floor(x * (1.0f / 289.0f)) * 289.0f; }

// Modulo 7 without a division
static float  mod7(float x)  { return x - floor(x * (1.0f / 7.0f)) * 7.0f; }
static float2 mod7(float2 x) { return x - floor(x * (1.0f / 7.0f)) * 7.0f; }
static float3 mod7(float3 x) { return x - floor(x * (1.0f / 7.0f)) * 7.0f; }
static float4 mod7(float4 x) { return x - floor(x * (1.0f / 7.0f)) * 7.0f; }

// Permutation polynomial: (34x^2 + x) math.mod 289
static float  permute(float x)  { return mod289((34.0f * x + 1.0f) * x); }
static float2 permute(float2 x) { return mod289((34.0f * x + 1.0f) * x); }
static float3 permute(float3 x) { return mod289((34.0f * x + 1.0f) * x); }
static float4 permute(float4 x) { return mod289((34.0f * x + 1.0f) * x); }

static float  taylorInvSqrt(float r)  { return 1.79284291400159f - 0.85373472095314f * r; }
static float2 taylorInvSqrt(float2 r) { return 1.79284291400159f - 0.85373472095314f * r; }
static float3 taylorInvSqrt(float3 r) { return 1.79284291400159f - 0.85373472095314f * r; }
static float4 taylorInvSqrt(float4 r) { return 1.79284291400159f - 0.85373472095314f * r; }

static uint NextState(inout uint state)
{
    state ^= state << 13;
    state ^= state >> 17;
    state ^= state << 5;
    return state;
}

static float NextFloat(inout uint state)
{
    return asfloat(0x3f800000 | (NextState(state) >> 9)) - 1.0f;
}

static float2 NextFloat2Direction(inout uint state)
{
    float angle = NextFloat(state) * PI * 2.0f;
    float s, c;
    sincos(angle, s, c);
    return float2(c, s);
}