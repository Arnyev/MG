using System;
using System.Collections.Generic;
using System.Numerics;

namespace MG
{
    public class MeshBuilder
    {
        public readonly List<Face> Faces = new List<Face>();
        public readonly List<HalfEdge> HalfEdges = new List<HalfEdge>();

        // When the mesh is modified and faces and half edges are removed from it, we do not actually remove them from the container vectors.
        // Insted, they are marked as disabled which means that the indices can be reused when we need to add new faces and half edges to the mesh.
        // We store the free indices in the following vectors.
        private readonly List<int> _disabledFaces = new List<int>();
        private readonly List<int> _disabledHalfEdges = new List<int>();

        public int AddFace()
        {
            if (_disabledFaces.Count > 0)
            {
                int index = _disabledFaces[_disabledFaces.Count - 1];
                var face = Faces[index];

                if (!face.IsDisabled() || face.PointsOnPositiveSide != null)
                    throw new Exception();

                face.MostDistantPointDist = 0;
                _disabledFaces.RemoveAt(_disabledFaces.Count - 1);

                return index;
            }

            Faces.Add(new Face());
            return Faces.Count - 1;
        }

        public int AddHalfEdge()
        {
            if (_disabledHalfEdges.Count > 0)
            {
                int index = _disabledHalfEdges[_disabledHalfEdges.Count - 1];
                _disabledHalfEdges.RemoveAt(_disabledHalfEdges.Count - 1);
                return index;
            }

            HalfEdges.Add(new HalfEdge());
            return HalfEdges.Count - 1;
        }

        // Mark a face as disabled and return a pointer to the points that were on the positive of it.
        public List<int> DisableFace(int faceIndex)
        {
            var face = Faces[faceIndex];
            face.Disable();
            _disabledFaces.Add(faceIndex);
            var points = face.PointsOnPositiveSide;
            face.PointsOnPositiveSide = null;
            return points;
        }

        public void DisableHalfEdge(int heIndex)
        {
            HalfEdges[heIndex] = new HalfEdge(-1, 0, 0, 0);
            _disabledHalfEdges.Add(heIndex);
        }

        public MeshBuilder(int a, int b, int c, int d)
        {
            HalfEdges.Add(new HalfEdge(b, 6, 0, 1));
            HalfEdges.Add(new HalfEdge(c, 9, 0, 2));
            HalfEdges.Add(new HalfEdge(a, 3, 0, 0));
            HalfEdges.Add(new HalfEdge(c, 2, 1, 4));
            HalfEdges.Add(new HalfEdge(d, 11, 1, 5));
            HalfEdges.Add(new HalfEdge(a, 7, 1, 3));
            HalfEdges.Add(new HalfEdge(a, 0, 2, 7));
            HalfEdges.Add(new HalfEdge(d, 5, 2, 8));

            HalfEdges.Add(new HalfEdge(b, 10, 2, 6));
            HalfEdges.Add(new HalfEdge(b, 1, 3, 10));
            HalfEdges.Add(new HalfEdge(d, 8, 3, 11));
            HalfEdges.Add(new HalfEdge(c, 4, 3, 9));

            var ABC = new Face { HalfEdge = 0 };
            Faces.Add(ABC);

            var ACD = new Face { HalfEdge = 3 };
            Faces.Add(ACD);

            var BAD = new Face { HalfEdge = 6 };
            Faces.Add(BAD);

            var CBD = new Face { HalfEdge = 9 };

            Faces.Add(CBD);
        }

        public int[] GetVertexIndicesOfFace(Face f)
        {
            var he1 = HalfEdges[f.HalfEdge];
            var he2 = HalfEdges[he1.Next];
            var he3 = HalfEdges[he2.Next];

            return new[] { he1.EndVertex, he2.EndVertex, he3.EndVertex };
        }

        public Tuple<int, int> GetVertexIndicesOfHalfEdge(HalfEdge he) =>
            Tuple.Create(HalfEdges[he.Opp].EndVertex, he.EndVertex);


        public int[] GetHalfEdgeIndicesOfFace(Face f) =>
            new[] { f.HalfEdge, HalfEdges[f.HalfEdge].Next, HalfEdges[HalfEdges[f.HalfEdge].Next].Next };


        public List<TriangleIndices> GetMesh()
        {
            var indices = new List<TriangleIndices>();
            indices.Capacity = Faces.Count - _disabledFaces.Count;

            bool[] faceProcessed = new bool[Faces.Count];
            var faceStack = new Stack<int>();

            // Map vertex indices from original point cloud to the new mesh vertex indices
            for (int i = 0; i < Faces.Count; i++)
                if (!Faces[i].IsDisabled())
                {
                    faceStack.Push(i);
                    break;
                }

            if (faceStack.Count == 0)
                return null;

            while (faceStack.Count > 0)
            {
                var top = faceStack.Pop();
                if (faceProcessed[top])
                    continue;

                faceProcessed[top] = true;
                var halfEdges = GetHalfEdgeIndicesOfFace(Faces[top]);
                var adjacent = new[]
                {
                    HalfEdges[HalfEdges[halfEdges[0]].Opp].Face,
                    HalfEdges[HalfEdges[halfEdges[1]].Opp].Face,
                    HalfEdges[HalfEdges[halfEdges[2]].Opp].Face
                };

                foreach (var a in adjacent)
                    if (!faceProcessed[a] && !Faces[a].IsDisabled())
                        faceStack.Push(a);

                var vertices = GetVertexIndicesOfFace(Faces[top]);

                indices.Add(new TriangleIndices(vertices[0], vertices[1], vertices[2], 0));
            }

            return indices;
        }

        public static Triangle[] CreateTriangles(List<Vector3> pointCloud, List<TriangleIndices> indices)
        {
            var triangles = new List<Triangle>();
            for (int i = 0; i < indices.Count; i++)
            {
                var triangleIndices = indices[i];
                triangles.Add(new Triangle(
                    pointCloud[triangleIndices.A],
                    pointCloud[triangleIndices.B],
                    pointCloud[triangleIndices.C]));
            }
            
            return triangles.ToArray();
        }
    }
}