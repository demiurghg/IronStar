
using System;
using System.Collections.Generic;
using BEPUphysics;
using BEPUutilities;
using BEPUutilities.DataStructures;
using Fusion.Drivers.Graphics;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.CollisionShapes;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using Fusion.Engine.Graphics.Scenes;
using MathConverter = Fusion.Core.Mathematics.MathConverter;
using MathHelper = Fusion.Core.Mathematics.MathUtil;
using Vector2 = Fusion.Core.Mathematics.Vector2;
using Vector3 = Fusion.Core.Mathematics.Vector3;
using VertexPositionNormalTexture = Fusion.Engine.Graphics.DebugVertex;
using BEPUutilities.DataStructures;

namespace BEPUrender.Models
{
    /// <summary>
    /// Helper class that can create shape mesh data.
    /// </summary>
    public static class DisplayConvex
    {
        private static BEPUutilities.Vector3[] SampleDirections;
        static DisplayConvex()
        {
            int[] sampleTriangleIndices;
            InertiaHelper.GenerateSphere(2, out SampleDirections, out sampleTriangleIndices);
        }

        public static void GetShapeMeshData(EntityCollidable collidable, RawList<VertexPositionNormalTexture> vertices, RawList<int> indices)
        {
            var shape = collidable.Shape as ConvexShape;
            if (shape == null)
                throw new ArgumentException("Wrong shape type for this helper.");
            var vertexPositions = new BEPUutilities.Vector3[SampleDirections.Length];

            for (int i = 0; i < SampleDirections.Length; ++i)
            {
                shape.GetLocalExtremePoint(SampleDirections[i], out vertexPositions[i]);
            }

            var hullIndices = new RawList<int>();
            ConvexHullHelper.GetConvexHull(vertexPositions, hullIndices);


            var hullTriangleVertices = new RawList<BEPUutilities.Vector3>();
            foreach (int i in hullIndices)
            {
                hullTriangleVertices.Add(vertexPositions[i]);
            }

            for (ushort i = 0; i < hullTriangleVertices.Count; i += 3)
            {
                Vector3 normal = MathConverter.Convert(BEPUutilities.Vector3.Normalize(BEPUutilities.Vector3.Cross(hullTriangleVertices[i + 2] - hullTriangleVertices[i], hullTriangleVertices[i + 1] - hullTriangleVertices[i])));
                vertices.Add(new VertexPositionNormalTexture(MathConverter.Convert(hullTriangleVertices[i]), normal, new Vector2(0, 0)));
                vertices.Add(new VertexPositionNormalTexture(MathConverter.Convert(hullTriangleVertices[i + 1]), normal, new Vector2(1, 0)));
                vertices.Add(new VertexPositionNormalTexture(MathConverter.Convert(hullTriangleVertices[i + 2]), normal, new Vector2(0, 1)));
                indices.Add(i);
                indices.Add((ushort)(i + 1));
                indices.Add((ushort)(i + 2));
            }
        }
    }
}