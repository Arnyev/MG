using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MG
{
    class IntersectionCalculator
    {
        public static List<List<IntersectionRange>> GroupRanges(List<IntersectionRange> input)
        {
            var graph = new bool[input.Count, input.Count];

            for (int i = 0; i < input.Count; i++)
                for (int j = i + 1; j < input.Count; j++)
                    graph[i, j] = graph[j, i] = input[i].IsAdjacent(input[j]);

            var ccs = GetConnectedComponents(input, graph);

            return ccs.Select(x => x.Select(y => input[y]).ToList()).ToList();
        }

        private static List<List<int>> GetConnectedComponents(List<IntersectionRange> input, bool[,] graph)
        {
            var components = new List<List<int>>();

            var visited = new bool[input.Count];
            var queue = new Queue<int>();
            for (int i = 0; i < input.Count; i++)
            {
                if (visited[i])
                    continue;

                var component = new List<int>();
                component.Add(i);
                queue.Enqueue(i);
                visited[i] = true;
                while (queue.Count != 0)
                {
                    var v = queue.Dequeue();
                    for (int u = 0; u < input.Count; u++)
                        if (!visited[u] && graph[v, u])
                        {
                            visited[u] = true;
                            component.Add(u);
                            queue.Enqueue(u);
                        }
                }

                components.Add(component);
            }

            return components;
        }

        public static bool CheckIntersection(Triangle[] trianglesA, Triangle[] trianglesB)
        {
            return CheckIntersectionEdgesAFacesB(trianglesA, trianglesB) ||
                   CheckIntersectionEdgesAFacesB(trianglesB, trianglesA);
        }

        private static bool CheckIntersectionEdgesAFacesB(Triangle[] trianglesA, Triangle[] trianglesB)
        {
            var minXsA = trianglesA.Select(x => x.MinX).ToArray();
            var maxXsA = trianglesA.Select(x => x.MaxX).ToArray();
            var minYsA = trianglesA.Select(x => x.MinY).ToArray();
            var maxYsA = trianglesA.Select(x => x.MaxY).ToArray();
            var minZsA = trianglesA.Select(x => x.MinZ).ToArray();
            var maxZsA = trianglesA.Select(x => x.MaxZ).ToArray();

            var minXsB = trianglesB.Select(x => x.MinX).ToArray();
            var maxXsB = trianglesB.Select(x => x.MaxX).ToArray();
            var minYsB = trianglesB.Select(x => x.MinY).ToArray();
            var maxYsB = trianglesB.Select(x => x.MaxY).ToArray();
            var minZsB = trianglesB.Select(x => x.MinZ).ToArray();
            var maxZsB = trianglesB.Select(x => x.MaxZ).ToArray();

            for (var i = 0; i < trianglesA.Length; i++)
            {
                var triangleA = trianglesA[i];
                var edges = new[]
                {
                    new Edge(triangleA.A, triangleA.B),
                    new Edge(triangleA.B, triangleA.C),
                    new Edge(triangleA.C, triangleA.A)
                };

                for (var j = 0; j < trianglesB.Length; j++)
                {
                    if (maxXsA[i] < minXsB[j] || minXsA[i] > maxXsB[j] ||
                        maxYsA[i] < minYsB[j] || minYsA[i] > maxYsB[j] ||
                        maxZsA[i] < minZsB[j] || minZsA[i] > maxZsB[j])
                        continue;

                    var triangleB = trianglesB[j];

                    foreach (var edge in edges)
                        if (ChechIntersection(triangleB.A, triangleB.B, triangleB.C, edge.Start, edge.Direction))
                            return true;
                }
            }

            return false;
        }

        public static bool ChechIntersection(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 edgeStart, Vector3 edgeDirection)
        {
            var normal = Vector3.Cross(p1 - p2, p3 - p2);

            var d = Vector3.Dot(p1 - edgeStart, normal) / Vector3.Dot(edgeDirection, normal);
            if (float.IsNaN(d) || d < 0 || d > 1)
                return false;

            var newPoint = d * edgeDirection + edgeStart;

            var v0 = p2 - p1;
            var v1 = p3 - p1;
            var v2 = newPoint - p1;
            float d00 = Vector3.Dot(v0, v0);
            float d01 = Vector3.Dot(v0, v1);
            float d11 = Vector3.Dot(v1, v1);
            float d20 = Vector3.Dot(v2, v0);
            float d21 = Vector3.Dot(v2, v1);
            float denom = d00 * d11 - d01 * d01;
            var v = (d11 * d20 - d01 * d21) / denom;
            var w = (d00 * d21 - d01 * d20) / denom;
            var u = 1.0f - v - w;

            if (v >= 0 && v <= 1 && u >= 0 && u <= 1 && w >= 0 && w <= 1)
                return true;

            return false;
        }
    }
}
