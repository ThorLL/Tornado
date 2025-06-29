﻿using System.Collections.Generic;
using UnityEngine;

namespace Simulation.Renderers.MeshGenerators
{
    public static class SphereGenerator
    {
        // Indices of the vertex pairs that make up each of the initial 12 edges
        static readonly int[] VertexPairs = { 0, 1, 0, 2, 0, 3, 0, 4, 1, 2, 2, 3, 3, 4, 4, 1, 5, 1, 5, 2, 5, 3, 5, 4 };
        // Indices of the edge triplets that make up the initial 8 faces
        static readonly int[] EdgeTriplets = { 0, 1, 4, 1, 2, 5, 2, 3, 6, 3, 0, 7, 8, 9, 4, 9, 10, 5, 10, 11, 6, 11, 8, 7 };
        // The six initial vertices
        static readonly Vector3[] BaseVertices = { Vector3.up, Vector3.left, Vector3.back, Vector3.right, Vector3.forward, Vector3.down };


        public static Mesh GenerateSphereMesh(int resolution)
        {
            Mesh mesh = new();
            int numDivisions = Mathf.Max(0, resolution);
            int numVertsPerFace = ((numDivisions + 3) * (numDivisions + 3) - (numDivisions + 3)) / 2;
            int numVerts = numVertsPerFace * 8 - (numDivisions + 2) * 12 + 6;
            int numTrisPerFace = (numDivisions + 1) * (numDivisions + 1);

            FixedSizeList<Vector3> vertices = new(numVerts);
            FixedSizeList<int> triangles = new(numTrisPerFace * 8 * 3);

            vertices.AddRange(BaseVertices);

            // Create 12 edges, with n vertices added along them (n = numDivisions)
            Edge[] edges = new Edge[12];
            for (int i = 0; i < VertexPairs.Length; i += 2)
            {
                Vector3 startVertex = vertices.Items[VertexPairs[i]];
                Vector3 endVertex = vertices.Items[VertexPairs[i + 1]];

                int[] edgeVertexIndices = new int[numDivisions + 2];
                edgeVertexIndices[0] = VertexPairs[i];

                // Add vertices along edge
                for (int divisionIndex = 0; divisionIndex < numDivisions; divisionIndex++)
                {
                    float t = (divisionIndex + 1f) / (numDivisions + 1f);
                    edgeVertexIndices[divisionIndex + 1] = vertices.NextIndex;
                    vertices.Add(Vector3.Slerp(startVertex, endVertex, t));
                }
                edgeVertexIndices[numDivisions + 1] = VertexPairs[i + 1];
                int edgeIndex = i / 2;
                edges[edgeIndex] = new Edge(edgeVertexIndices);
            }

            // Create faces
            for (int i = 0; i < EdgeTriplets.Length; i += 3)
            {
                int faceIndex = i / 3;
                bool reverse = faceIndex >= 4;
                CreateFace(edges[EdgeTriplets[i]], edges[EdgeTriplets[i + 1]], edges[EdgeTriplets[i + 2]], reverse);
            }

            mesh.SetVertices(vertices.Items);
            mesh.SetTriangles(triangles.Items, 0, true);
            mesh.RecalculateNormals();
            return mesh;

            void CreateFace(Edge sideA, Edge sideB, Edge bottom, bool reverse)
            {
                int numPointsInEdge = sideA.VertexIndices.Length;
                FixedSizeList<int> vertexMap = new(numVertsPerFace);
                vertexMap.Add(sideA.VertexIndices[0]); // top of triangle

                for (int i = 1; i < numPointsInEdge - 1; i++)
                {
                    // Side A vertex
                    vertexMap.Add(sideA.VertexIndices[i]);

                    // Add vertices between sideA and sideB
                    Vector3 sideAVertex = vertices.Items[sideA.VertexIndices[i]];
                    Vector3 sideBVertex = vertices.Items[sideB.VertexIndices[i]];
                    int numInnerPoints = i - 1;
                    for (int j = 0; j < numInnerPoints; j++)
                    {
                        float t = (j + 1f) / (numInnerPoints + 1f);
                        vertexMap.Add(vertices.NextIndex);
                        vertices.Add(Vector3.Slerp(sideAVertex, sideBVertex, t));
                    }

                    // Side B vertex
                    vertexMap.Add(sideB.VertexIndices[i]);
                }

                // Add bottom edge vertices
                for (int i = 0; i < numPointsInEdge; i++) vertexMap.Add(bottom.VertexIndices[i]);

                // Triangulate
                int numRows = numDivisions + 1;
                for (int row = 0; row < numRows; row++)
                {
                    // vertices down left edge follow quadratic sequence: 0, 1, 3, 6, 10, 15...
                    // the nth term can be calculated with: (n^2 - n)/2
                    int topVertex = ((row + 1) * (row + 1) - row - 1) / 2;
                    int bottomVertex = ((row + 2) * (row + 2) - row - 2) / 2;

                    int numTrianglesInRow = 1 + 2 * row;
                    for (int column = 0; column < numTrianglesInRow; column++)
                    {
                        int v0 = topVertex;
                        int v1, v2;

                        if (column % 2 == 0)
                        {
                            v1 = bottomVertex + 1;
                            v2 = bottomVertex;
                            topVertex++;
                            bottomVertex++;
                        }
                        else
                        {
                            v1 = bottomVertex;
                            v2 = topVertex - 1;
                        }

                        triangles.Add(vertexMap.Items[v0]);
                        triangles.Add(vertexMap.Items[reverse ? v2 : v1]);
                        triangles.Add(vertexMap.Items[reverse ? v1 : v2]);
                    }
                }

            }
        }

        // Convenience classes:
        class Edge
        {
            public readonly int[] VertexIndices;

            public Edge(int[] vertexIndices) => VertexIndices = vertexIndices;
        }

        class FixedSizeList<T>
        {
            public readonly T[] Items;
            public int NextIndex;

            public FixedSizeList(int size) => Items = new T[size];

            public void Add(T item)
            {
                Items[NextIndex] = item;
                NextIndex++;
            }

            public void AddRange(IEnumerable<T> items)
            {
                foreach (T item in items) Add(item);
            }
        }

    }
}