using Unity.Mathematics;

// ReSharper disable once CheckNamespace
namespace UnityEngine
{
    public static class ComputeShaderExtensions
    {
        public static void Run(this ComputeShader cs, int kernelIndex, int numIterationsX, int numIterationsY = 1, int numIterationsZ = 1)
        {
            cs.GetKernelThreadGroupSizes(kernelIndex, out uint x, out uint y, out uint z);
            int numGroupsX = Mathf.CeilToInt(numIterationsX / (float)x);
            int numGroupsY = Mathf.CeilToInt(numIterationsY / (float)y);
            int numGroupsZ = Mathf.CeilToInt(numIterationsZ / (float)z);
            cs.Dispatch(kernelIndex, numGroupsX, numGroupsY, numGroupsZ);
        }

        public static void SetFloat2(this ComputeShader cs, string name, float2 value) => cs.SetFloats(name, value.x, value.y);
        public static void SetFloat3(this ComputeShader cs, string name, float3 value) => cs.SetFloats(name, value.x, value.y, value.z);
        public static void SetFloat4(this ComputeShader cs, string name, float4 value) => cs.SetFloats(name, value.x, value.y, value.z, value.w);
        public static void SetInt2(this ComputeShader cs, string name, int2 value) => cs.SetInts(name, value.x, value.y);
        public static void SetInt3(this ComputeShader cs, string name, int3 value) => cs.SetInts(name, value.x, value.y, value.z);
        public static void SetInt4(this ComputeShader cs, string name, int4 value) => cs.SetInts(name, value.x, value.y, value.z, value.w);
        public static void SetFloat2(this ComputeShader cs, int nameId, float2 value) => cs.SetFloats(nameId, value.x, value.y);
        public static void SetFloat3(this ComputeShader cs, int nameId, float3 value) => cs.SetFloats(nameId, value.x, value.y, value.z);
        public static void SetFloat4(this ComputeShader cs, int nameId, float4 value) => cs.SetFloats(nameId, value.x, value.y, value.z, value.w);
        public static void SetInt2(this ComputeShader cs, int nameId, int2 value) => cs.SetInts(nameId, value.x, value.y);
        public static void SetInt3(this ComputeShader cs, int nameId, int3 value) => cs.SetInts(nameId, value.x, value.y, value.z);
        public static void SetInt4(this ComputeShader cs, int nameId, int4 value) => cs.SetInts(nameId, value.x, value.y, value.z, value.w);
    }
}