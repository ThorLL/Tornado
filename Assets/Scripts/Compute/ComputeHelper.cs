using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Compute
{
    public static class ComputeHelper
    {
        static readonly uint[] ArgsBufferArray = new uint[5];
        
        public static RenderTexture CreateRenderTexture(int width, int height, FilterMode filterMode, GraphicsFormat format, string name, int depth = 0, bool useMipMaps = false)
        {
            RenderTexture texture = new(width, height, depth)
            {
                graphicsFormat = format,
                enableRandomWrite = true,
                autoGenerateMips = false,
                useMipMap = useMipMaps
            };
            texture.Create();

            texture.name = name;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = filterMode;
            return texture;
        }

        public static ComputeBuffer CreateStructuredBuffer<T>(int capacity) => new(capacity, GetStride<T>());

        public static ComputeBuffer CreateStructuredBuffer<T>(IEnumerable<T> data)
        {
            ComputeBuffer buffer = new(data.Count(), GetStride<T>());
            buffer.SetData(data.ToArray());
            return buffer;
        }

        public static void CreateArgsBuffer(ref ComputeBuffer argsBuffer, Mesh mesh, int numInstances)
        {
            const int stride = sizeof(uint);
            const int numArgs = 5;
            const int subMeshIndex = 0;

            bool createNewBuffer = argsBuffer == null || !argsBuffer.IsValid() || argsBuffer.count != ArgsBufferArray.Length || argsBuffer.stride != stride;
            if (createNewBuffer)
            {
                argsBuffer?.Release();
                argsBuffer = new ComputeBuffer(numArgs, stride, ComputeBufferType.IndirectArguments);
            }

            lock (ArgsBufferArray)
            {
                ArgsBufferArray[0] = mesh.GetIndexCount(subMeshIndex);
                ArgsBufferArray[1] = (uint)numInstances;
                ArgsBufferArray[2] = mesh.GetIndexStart(subMeshIndex);
                ArgsBufferArray[3] = mesh.GetBaseVertex(subMeshIndex);
                ArgsBufferArray[4] = 0; // offset
				
                argsBuffer.SetData(ArgsBufferArray);
            }
        }
        
        static int GetStride<T>() => System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));

        public static void LoadShader(ref ComputeShader computeShader, string resourcePath)
        {
            if (computeShader == null) computeShader = (ComputeShader)Resources.Load(resourcePath);
        }
    }
}
