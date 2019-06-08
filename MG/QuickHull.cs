using System;
using System.Collections.Generic;
using System.Numerics;

namespace MG
{
    class QuickHull
    {
        private readonly float _epsilon = 0.0001f;
        private readonly float _epsilonSquared;

        private readonly List<Vector3> _vertexData;
        private readonly MeshBuilder _mesh;
        public QuickHull(List<Vector3> pointCloud)
        {
            _vertexData = pointCloud;
            var extremeValues = GetExtremeValues();
            var scale = GetScale(extremeValues);
            _epsilon = _epsilon * scale;
            _epsilonSquared = _epsilon * _epsilon;
            var initialPoints = GetFirstFourPointIndices(extremeValues);

            _mesh = GetInitialTetrahedron(initialPoints);
            CreateConvexHalfEdgeMesh();
        }

        private bool AddPointToFace(Face face, int pointIndex)
        {
            float distance = GetSignedDistanceToPlane(_vertexData[pointIndex], face.Plane);

            if (distance <= 0 || distance * distance <= _epsilonSquared * face.Plane.NormalLength)
                return false;

            if (face.PointsOnPositiveSide == null)
                face.PointsOnPositiveSide = new List<int>();

            face.PointsOnPositiveSide.Add(pointIndex);
            if (distance > face.MostDistantPointDist)
            {
                face.MostDistantPointDist = distance;
                face.MostDistantPoint = pointIndex;
            }
            return true;
        }

        static Vector3 GetTriangleNormal(Vector3 a, Vector3 b, Vector3 c) => Vector3.Cross(a - c, b - c);

        private MeshBuilder GetInitialTetrahedron(int[] pointIndices)
        {
            // Create a tetrahedron half edge mesh and compute planes defined by each triangle
            var mesh = new MeshBuilder(pointIndices[0], pointIndices[1], pointIndices[2], pointIndices[3]);
            foreach (var face in mesh.Faces)
            {
                var verticeIndices = mesh.GetVertexIndicesOfFace(face);
                var va = _vertexData[verticeIndices[0]];
                var vb = _vertexData[verticeIndices[1]];
                var vc = _vertexData[verticeIndices[2]];

                var normal = GetTriangleNormal(va, vb, vc);
                var plane = new Plane(normal, va);
                face.Plane = plane;
            }

            // Finally we assign a face for each vertex outside the tetrahedron (vertices inside the tetrahedron have no role anymore)
            for (int i = 0; i < _vertexData.Count; i++)
                foreach (var face in mesh.Faces)
                    if (AddPointToFace(face, i))
                        break;

            return mesh;
        }

        private int[] GetFirstFourPointIndices(int[] extremeValues)
        {
            GetFirstTwoPoints(extremeValues, out var point1, out var point2);
            var point3 = GetThirdPoint(point1, point2);

            var trianglePlane = new Plane(GetTriangleNormal(
                _vertexData[point1], _vertexData[point2], _vertexData[point3]), _vertexData[point1]);

            var point4 = GetFourthPoint(trianglePlane);

            if (trianglePlane.IsPointOnPositiveSide(_vertexData[point4]))
            {
                var tmp = point1;
                point1 = point2;
                point2 = tmp;
            }

            var pointIndices = new[] { point1, point2, point3, point4 };
            return pointIndices;
        }

        static float GetSignedDistanceToPlane(Vector3 point, Plane plane)
            => Vector3.Dot(plane.Normal, point) + plane.Distance;

        private int GetFourthPoint(Plane trianglePlane)
        {
            int maxI = 0;
            var maxDistance = _epsilon;

            for (int i = 0; i < _vertexData.Count; i++)
            {
                float d = Math.Abs(GetSignedDistanceToPlane(_vertexData[i], trianglePlane));
                if (d > maxDistance)
                {
                    maxDistance = d;
                    maxI = i;
                }
            }

            if (maxDistance == _epsilon)
                throw new Exception("Degenerate case, 2 dimensional object");

            return maxI;
        }

        private void GetFirstTwoPoints(int[] extremeValues, out int selectedPoint1, out int selectedPoint2)
        {
            float maxD = _epsilonSquared;
            selectedPoint1 = 0;
            selectedPoint2 = 0;

            for (int i = 0; i < 6; i++)
            {
                for (int j = i + 1; j < 6; j++)
                {
                    float d = (_vertexData[extremeValues[i]] - _vertexData[extremeValues[j]]).LengthSquared();
                    if (d > maxD)
                    {
                        maxD = d;
                        selectedPoint1 = extremeValues[i];
                        selectedPoint2 = extremeValues[j];
                    }
                }
            }

            if (maxD == _epsilonSquared || selectedPoint1 == selectedPoint2)
                throw new Exception("A degenerate case: the point cloud seems to consists of a single point");
        }

        private int GetThirdPoint(int selectedPoint1, int selectedPoint2)
        {
            var v1 = _vertexData[selectedPoint1];
            var v2 = _vertexData[selectedPoint2];

            var r = new Ray(v1, v2 - v1);

            var maxD = _epsilonSquared;

            var maxI = int.MaxValue;
            for (int i = 0; i < _vertexData.Count; i++)
            {
                var distToRay = GetSquaredDistanceBetweenPointAndRay(_vertexData[i], r);
                if (distToRay > maxD)
                {
                    maxD = distToRay;
                    maxI = i;
                }
            }

            if (maxD == _epsilonSquared || maxI == selectedPoint1 || maxI == selectedPoint2)
                throw new Exception("Degenerate case, 1 dimensional object");

            return maxI;
        }

        static float GetSquaredDistanceBetweenPointAndRay(Vector3 point, Ray ray)
        {
            Vector3 diff = point - ray.Start;
            float dot = Vector3.Dot(ray.Direction, diff);
            var dir = ray.Direction;
            return diff.LengthSquared() - dot * dot / dir.LengthSquared();
        }

        void CreateConvexHalfEdgeMesh()
        {
            var faceQueue = CreateInitialFaceQueue();

            // Process faces until the face list is empty.
            int faceAnalysisNumber = 0;
            while (faceQueue.Count > 0)
            {
                faceAnalysisNumber++;

                var topFaceIndex = faceQueue.Dequeue();

                var face = _mesh.Faces[topFaceIndex];
                face.InFaceStack = false;

                if (face.PointsOnPositiveSide == null || face.IsDisabled())
                    continue;

                //// Find out the faces that have our active point on their positive side (these are the "visible faces"). The face on top of the stack of course is one of them. At the same time, we create a list of horizon edges.
                var horizonEdges = new List<int>();
                var visibleFaces = GetVisibleFaces(topFaceIndex, faceAnalysisNumber, _vertexData[face.MostDistantPoint], horizonEdges);

                // Order horizon edges so that they form a loop. This may fail due to numerical instability in which case we give up trying to solve horizon edge for this point and accept a minor degeneration in the convex hull.
                if (!ReorderHorizonEdges(horizonEdges))
                    throw new Exception("Failed to solve horizon edge.");

                // Except for the horizon edges, all half edges of the visible faces can be marked as disabled. Their data slots will be reused.
                // The faces will be disabled as well, but we need to remember the points that were on the positive side of them - therefore
                // we save pointers to them.

                var newHalfEdgeIndices = new List<int>();
                var disabledFacePointVectors = new List<int>();
                DisableVisibleFaces(visibleFaces, horizonEdges, newHalfEdgeIndices, disabledFacePointVectors);

                var newFaceIndices = CreateNewFaces(horizonEdges, face.MostDistantPoint, _vertexData[face.MostDistantPoint], newHalfEdgeIndices);

                AddPointsToNewFaces(disabledFacePointVectors, face.MostDistantPoint, newFaceIndices);

                AddNewFacesToStack(faceQueue, newFaceIndices);
            }
        }

        private void DisableVisibleFaces(List<int> visibleFaces, List<int> horizonEdges, List<int> newHalfEdgeIndices,
            List<int> disabledFacePointVectors)
        {
            int disableCounter = 0;

            foreach (var faceIndex in visibleFaces)
            {
                var disabledFace = _mesh.Faces[faceIndex];
                var halfEdges = _mesh.GetHalfEdgeIndicesOfFace(disabledFace);

                for (int j = 0; j < 3; j++)
                {
                    if ((disabledFace.HorizonEdgesOnCurrentIteration & (1 << j)) != 0)
                        continue;

                    if (disableCounter < horizonEdges.Count * 2)
                    {
                        // Use on this iteration
                        newHalfEdgeIndices.Add(halfEdges[j]);
                        disableCounter++;
                    }
                    else
                        // Mark for reusal on later iteration step
                        _mesh.DisableHalfEdge(halfEdges[j]);
                }

                // Disable the face, but retain pointer to the points that were on the positive side of it. We need to assign those points
                // to the new faces we create shortly.
                var pointsToHandle = _mesh.DisableFace(faceIndex);
                if (pointsToHandle != null)
                    disabledFacePointVectors.AddRange(pointsToHandle);
            }

            if (disableCounter < horizonEdges.Count * 2)
            {
                int newHalfEdgesNeeded = horizonEdges.Count * 2 - disableCounter;
                for (int i = 0; i < newHalfEdgesNeeded; i++)
                    newHalfEdgeIndices.Add(_mesh.AddHalfEdge());
            }
        }

        private List<int> GetVisibleFaces(int topFaceIndex, int faceStackElementNumber, Vector3 activePoint, List<int> horizonEdges)
        {
            var visibleFaces = new List<int>();

            var possiblyVisibleFaces = new Stack<FaceData>();
            possiblyVisibleFaces.Push(new FaceData(topFaceIndex, int.MaxValue));

            while (possiblyVisibleFaces.Count > 0)
            {
                var faceData = possiblyVisibleFaces.Pop();

                var pvisibleFace = _mesh.Faces[faceData.FaceIndex];

                if (pvisibleFace.VisibilityCheckedOnIteration == faceStackElementNumber)
                {
                    if (pvisibleFace.IsVisibleFaceOnCurrentIteration)
                        continue;
                }
                else
                {
                    pvisibleFace.VisibilityCheckedOnIteration = faceStackElementNumber;

                    if (Vector3.Dot(pvisibleFace.Plane.Normal, activePoint) + pvisibleFace.Plane.Distance > 0)
                    {
                        pvisibleFace.IsVisibleFaceOnCurrentIteration = true;
                        pvisibleFace.HorizonEdgesOnCurrentIteration = 0;
                        visibleFaces.Add(faceData.FaceIndex);

                        var faceIndices = _mesh.GetHalfEdgeIndicesOfFace(pvisibleFace);

                        foreach (var heIndex in faceIndices)
                            if (_mesh.HalfEdges[heIndex].Opp != faceData.EnteredFromHalfEdge)
                                possiblyVisibleFaces.Push(
                                    new FaceData(_mesh.HalfEdges[_mesh.HalfEdges[heIndex].Opp].Face, heIndex));

                        continue;
                    }

                    if (faceData.FaceIndex == topFaceIndex)
                        throw new Exception();
                }

                UpdateHorizonEdges(horizonEdges, pvisibleFace, faceData);
            }

            return visibleFaces;
        }

        private void UpdateHorizonEdges(List<int> horizonEdges, Face pvisibleFace, FaceData faceData)
        {
            // The face is not visible. Therefore, the halfedge we came from is part of the horizon edge.
            pvisibleFace.IsVisibleFaceOnCurrentIteration = false;
            horizonEdges.Add(faceData.EnteredFromHalfEdge);
            // Store which half edge is the horizon edge.
            var halfEdges = _mesh.GetHalfEdgeIndicesOfFace(
                _mesh.Faces[_mesh.HalfEdges[faceData.EnteredFromHalfEdge].Face]);

            int ind;
            if (halfEdges[0] == faceData.EnteredFromHalfEdge)
                ind = 0;
            else if (halfEdges[1] == faceData.EnteredFromHalfEdge)
                ind = 1;
            else
                ind = 2;

            _mesh.Faces[_mesh.HalfEdges[faceData.EnteredFromHalfEdge].Face]
                    .HorizonEdgesOnCurrentIteration |= 1 << ind;
        }

        private Queue<int> CreateInitialFaceQueue()
        {
            // Init face stack with those faces that have points assigned to them
            var faceStack = new Queue<int>();

            for (int i = 0; i < 4; i++)
                if (_mesh.Faces[i].PointsOnPositiveSide != null && _mesh.Faces[i].PointsOnPositiveSide.Count > 0)
                {
                    faceStack.Enqueue(i);
                    _mesh.Faces[i].InFaceStack = true;
                }

            return faceStack;
        }

        private void AddPointsToNewFaces(List<int> disabledFacePointVectors, int activePointIndex, List<int> newFaceIndices)
        {
            // Assign points that were on the positive side of the disabled faces to the new faces.
            foreach (var point in disabledFacePointVectors)
            {
                if (point == activePointIndex)
                    continue;
                foreach (var newFaceIndex in newFaceIndices)
                    if (AddPointToFace(_mesh.Faces[newFaceIndex], point))
                        break;
            }
        }

        private void AddNewFacesToStack(Queue<int> faceQueue, List<int> newFaceIndices)
        {
            foreach (var newFaceIndex in newFaceIndices)
            {
                var newFace = _mesh.Faces[newFaceIndex];
                if (newFace.PointsOnPositiveSide == null || newFace.InFaceStack)
                    continue;

                faceQueue.Enqueue(newFaceIndex);
                newFace.InFaceStack = true;
            }
        }

        private List<int> CreateNewFaces(List<int> horizonEdges, int activePointIndex, Vector3 activePoint, List<int> newHalfEdgeIndices)
        {
            var newFaceIndices = new List<int>();

            for (int i = 0; i < horizonEdges.Count; i++)
            {
                int ab = horizonEdges[i];

                var horizonEdgeVertexIndices = _mesh.GetVertexIndicesOfHalfEdge(_mesh.HalfEdges[ab]);
                var a = horizonEdgeVertexIndices.Item1;
                var b = horizonEdgeVertexIndices.Item2;
                var c = activePointIndex;

                int ca = newHalfEdgeIndices[2 * i + 0];
                int bc = newHalfEdgeIndices[2 * i + 1];

                var halfEdges = _mesh.HalfEdges;

                var caOpp = newHalfEdgeIndices[i > 0 ? i * 2 - 1 : 2 * horizonEdges.Count - 1];
                var bcOpp = newHalfEdgeIndices[(i + 1) * 2 % (horizonEdges.Count * 2)];

                int newFaceIndex = _mesh.AddFace();
                newFaceIndices.Add(newFaceIndex);

                halfEdges[ab] = new HalfEdge(halfEdges[ab].EndVertex, halfEdges[ab].Opp, newFaceIndex, bc);
                halfEdges[bc] = new HalfEdge(c, bcOpp, newFaceIndex, ca);
                halfEdges[ca] = new HalfEdge(a, caOpp, newFaceIndex, ab);

                var newFace = _mesh.Faces[newFaceIndex];

                newFace.Plane = new Plane(GetTriangleNormal(_vertexData[a], _vertexData[b], activePoint), activePoint);

                newFace.HalfEdge = ab;
            }

            return newFaceIndices;
        }

        private bool ReorderHorizonEdges(List<int> horizonEdges)
        {
            for (int i = 0; i < horizonEdges.Count - 1; i++)
            {
                int endVertex = _mesh.HalfEdges[horizonEdges[i]].EndVertex;
                bool foundNext = false;
                for (int j = i + 1; j < horizonEdges.Count; j++)
                {
                    int beginVertex = _mesh.HalfEdges[_mesh.HalfEdges[horizonEdges[j]].Opp].EndVertex;
                    if (beginVertex == endVertex)
                    {
                        var tmp = horizonEdges[j];
                        horizonEdges[j] = horizonEdges[i + 1];
                        horizonEdges[i + 1] = tmp;
                        foundNext = true;
                        break;
                    }
                }

                if (!foundNext)
                    return false;
            }

            if (_mesh.HalfEdges[horizonEdges[horizonEdges.Count - 1]].EndVertex !=
                _mesh.HalfEdges[_mesh.HalfEdges[horizonEdges[0]].Opp].EndVertex)
                throw new Exception();

            return true;
        }

        private float GetScale(int[] extremeValues)
        {
            float s = 0;
            for (int i = 0; i < 6; i++)
            {
                var v = _vertexData[extremeValues[i]];
                if (Math.Abs(v.X) > s)
                    s = Math.Abs(v.X);
                if (Math.Abs(v.Y) > s)
                    s = Math.Abs(v.Y);
                if (Math.Abs(v.Z) > s)
                    s = Math.Abs(v.Z);
            }
            return s;
        }

        private int[] GetExtremeValues()
        {
            int[] outIndices = new int[6];
            float[] extremeVals =
            {
                _vertexData[0].X,
                _vertexData[0].X,
                _vertexData[0].Y,
                _vertexData[0].Y,
                _vertexData[0].Z,
                _vertexData[0].Z
            };

            for (int i = 1; i < _vertexData.Count; i++)
            {
                var pos = _vertexData[i];

                if (pos.X > extremeVals[0])
                {
                    extremeVals[0] = pos.X;
                    outIndices[0] = i;
                }
                else if (pos.X < extremeVals[1])
                {
                    extremeVals[1] = pos.X;
                    outIndices[1] = i;
                }
                if (pos.Y > extremeVals[2])
                {
                    extremeVals[2] = pos.Y;
                    outIndices[2] = i;
                }
                else if (pos.Y < extremeVals[3])
                {
                    extremeVals[3] = pos.Y;
                    outIndices[3] = i;
                }
                if (pos.Z > extremeVals[4])
                {
                    extremeVals[4] = pos.Z;
                    outIndices[4] = i;
                }
                else if (pos.Z < extremeVals[5])
                {
                    extremeVals[5] = pos.Z;
                    outIndices[5] = i;
                }
            }
            return outIndices;
        }

        public List<TriangleIndices> GetMeshIndices() => _mesh.GetMesh();

        public Triangle[] Mesh => MeshBuilder.CreateTriangles(_vertexData, _mesh.GetMesh());
    }
}
