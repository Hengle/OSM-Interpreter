﻿namespace Cuku.Geo
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using System.IO;
    using System;
    using System.Linq;
    using MessagePack;
    using UnityEngine.ProBuilder;
    using UnityEngine.ProBuilder.MeshOperations;
    using UnityEditor.ProBuilder;

    public static class CreateFeatures
    {
        static float borderHeight = 500.0f;

        [MenuItem("Cuku/Create Boundary Geometry")]
        static void CreateBoundaryGeometry()
        {
            var bytes = File.ReadAllBytes("Assets/StreamingAssets/Data/border.cuk");
            var boundaryData = MessagePackSerializer.Deserialize<Feature>(bytes);

            var members = boundaryData.Relations[0].Members;

            var boundaryPoints = new List<Vector3>();
            for (int m = 0; m < members.Length; m++)
            {
                var line = boundaryData.Lines.FirstOrDefault(l => l.Id == members[m].Id);
                var points = line.Points.GetPositions();

                // Reverse line points to match previous line's direction
                if (boundaryPoints.Count != 0 && boundaryPoints.Last() != points[0])
                {
                    points = points.Reverse().ToArray();
                }

                boundaryPoints.AddRange(points);
            }

            var firstLine = boundaryData.Lines.FirstOrDefault(l => l.Id == members[0].Id);
            var startPoint = new Point[] { firstLine.Points[0] }.GetPositions()[0];
            boundaryPoints.Add(startPoint);
            // boundaryPoints.ToArray().Reverse().ToArray().CreateBoundaryWall("Boundary");
            boundaryPoints.ToArray().Reverse().ToArray().CreateWall("Boundary");
        }


        static void CreateBoundaryWall(this Vector3[] basePoints, string name, Transform parent = null)
        {
            var tilePoints = basePoints.GroupBy(bp => bp.GetHitTerrain());

            foreach(var group in tilePoints) {
                group.ToArray().CreateWall(group.Key);
            }
        }

        static void CreateWall(this Vector3[] basePoints, string name, Transform parent = null)
        {
            // Create vertices
            var wallVertices = new List<Vector3>();

            for (int p = 0; p < basePoints.Length - 1; p++)
            {
                var point0 = basePoints[p];
                var point1 = basePoints[p + 1];

                wallVertices.Add(point0);
                wallVertices.Add(point1);
                wallVertices.Add(new Vector3(point0.x, borderHeight, point0.z));
                wallVertices.Add(new Vector3(point1.x, borderHeight, point1.z));
            }

            var sharedVertices = new List<SharedVertex>();

            // Create faces
            var faces = new List<Face>();
            for (int f = 0; f < wallVertices.Count - 3; f += 4)
            {
                var faceVertices = new int[] { f, f + 1, f + 2, f + 1, f + 3, f + 2 };
                faces.Add(new Face(faceVertices));
            }

            var wall = ProBuilderMesh.Create(wallVertices, faces);

            Normals.CalculateNormals(wall);
            Normals.CalculateTangents(wall);
            Smoothing.ApplySmoothingGroups(wall, faces, 30);
            wall.ToMesh();
            wall.Refresh();
            EditorMeshUtility.Optimize(wall);

            wall.SetMaterial(faces, Resources.Load<Material>("Boundary"));

            wall.gameObject.name = wall.name = name;
            wall.transform.SetParent(parent, true);
        }

        static string GetHitTerrain(this Vector3 position)
        {
            var origin = position;
            origin.y = borderHeight;

            RaycastHit hit;
            Ray ray = new Ray(origin, Vector3.down);
            if (Physics.Raycast (ray, out hit))
            {
                return hit.transform.name;
            }

            return null;
        }

        static Vector3[] GetPositions(this Point[] points)
        {
            var positions = new Vector3[points.Length];
            for (int p = 0; p < points.Length; p++)
            {
                positions[p] = new Vector3((float)points[p].X, 0, (float)points[p].Y);
            }
            return positions;
        }
    }
}
