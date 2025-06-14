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
    }
}