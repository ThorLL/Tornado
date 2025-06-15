using Unity.Mathematics;
using UnityEngine;

namespace MoreGizmos
{
    public static class GizmosExtra
    {
        public static void DrawWireCircle(Vector3 center, float radius, float segments = 32)
        {
            float degreePrSegment = math.PI2 / segments;

            for (int i = 0; i < segments; i++)
            {
                float degrees = degreePrSegment * i;
                Vector3 pointA = center + new Vector3(radius * Mathf.Cos(degrees), 0, radius * Mathf.Sin(degrees));
                degrees += degreePrSegment;
                Vector3 pointB = center + new Vector3(radius * Mathf.Cos(degrees), 0, radius * Mathf.Sin(degrees));
                Gizmos.DrawLine(pointA, pointB);
            }
        }
    }
}

