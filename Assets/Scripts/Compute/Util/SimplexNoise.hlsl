﻿#include "Assets/Scripts/Compute/Util/Math.hlsl"

// based on https://github.com/keijiro/NoiseShader/blob/master/Assets/GLSL/SimplexNoise2D.glsl
// which itself is modification of https://github.com/ashima/webgl-noise/blob/master/src/noise3D.glsl
//
// License : Copyright (C) 2011 Ashima Arts. All rights reserved.
//           Distributed under the MIT License. See LICENSE file.
//           https://github.com/keijiro/NoiseShader/blob/master/LICENSE
//           https://github.com/ashima/webgl-noise
//           https://github.com/stegu/webgl-noise

// output noise is in range [-1, 1]
float sNoise(float2 v) {
    const float4 c = float4(0.211324865405187f,  // (3.0-sqrt(3.0))/6.0
                             0.366025403784439f,   // 0.5*(sqrt(3.0)-1.0)
                             -0.577350269189626f,  // -1.0 + 2.0 * C.x
                             0.024390243902439f);  // 1.0 / 41.0

    // First corner
    float2 i  = floor(v + dot(v, c.yy));
    float2 x0 = v -   i + dot(i, c.xx);

    // Other corners
    float2 i1;
    i1.x = step(x0.y, x0.x);
    i1.y = 1.0 - i1.x;

    // x1 = x0 - i1  + 1.0 * C.xx;
    // x2 = x0 - 1.0 + 2.0 * C.xx;
    float2 x1 = x0 + c.xx - i1;
    float2 x2 = x0 + c.zz;

    // Permutations
    i = mod289(i); // Avoid truncation effects in permutation
    float3 p =
      permute(permute(i.y + float3(0.0f, i1.y, 1.0f))
                    + i.x + float3(0.0f, i1.x, 1.0f));

    float3 m = max(0.5f - float3(dot(x0, x0), dot(x1, x1), dot(x2, x2)), 0.0f);
    m = m * m;
    m = m * m;

    // Gradients: 41 points uniformly over a line, mapped onto a diamond.
    // The ring size 17*17 = 289 is close to a multiple of 41 (41*7 = 287)
    float3 x = 2.0f * frac(p * c.www) - 1.0f;
    float3 h = abs(x) - 0.5f;
    float3 ox = floor(x + 0.5f);
    float3 a0 = x - ox;

    // Normalise gradients implicitly by scaling m
    m *= taylorInvSqrt(a0 * a0 + h * h);

    // Compute final noise value at P
    float3 g = float3(
        a0.x * x0.x + h.x * x0.y,
        a0.y * x1.x + h.y * x1.y,
        g.z = a0.z * x2.x + h.z * x2.y
    );
    return 130.0f * dot(m, g);
}

float sNoise01(float2 v) {
    return sNoise(v) * 0.5f + 0.5f;
}

float sNoise(inout uint state)
{
    return sNoise(float2(NextFloat(state), NextFloat(state)));
}

float sNoise01(inout uint state)
{
    return sNoise01(float2(NextFloat(state), NextFloat(state)));
}