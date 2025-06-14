// ReSharper disable once CheckNamespace
namespace UnityEngine
{
    public static class StructuredBufferExtensions
    {
        public static T[] ReadValues<T>(this ComputeBuffer buffer, int index = 0, int count = -1)
        {
            if (count <= 0) count = buffer.count;
            T[] data = new T[buffer.count];
            buffer.GetData(data, 0, index, count);
            return data;
        }

        public static T ReadValue<T>(this ComputeBuffer buffer, int index) => buffer.ReadValues<T>(index, 1)[0];
    }
}