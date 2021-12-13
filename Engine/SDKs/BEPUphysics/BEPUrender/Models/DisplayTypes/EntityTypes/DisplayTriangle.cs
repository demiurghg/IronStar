
using System.Collections.Generic;
using Fusion.Core.Mathematics;
using Fusion.Core;
using BEPUphysics.CollisionShapes.ConvexShapes;
using System;
using BEPUphysics.CollisionShapes;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using Fusion.Engine.Graphics.Scenes;
using VertexPositionNormalTexture = Fusion.Engine.Graphics.DebugVertex;
using BEPUutilities.DataStructures;

namespace BEPUrender.Models
{
	/// <summary>
	/// Helper class that can create shape mesh data.
	/// </summary>
	public static class DisplayTriangle
	{


		public static void GetShapeMeshData(EntityCollidable collidable, RawList<VertexPositionNormalTexture> vertices, RawList<int> indices)
		{
			var triangleShape = collidable.Shape as TriangleShape;
			if(triangleShape == null)
				throw new ArgumentException("Wrong shape type.");
			Vector3 normal = MathConverter.Convert(triangleShape.GetLocalNormal());
			vertices.Add(new VertexPositionNormalTexture(MathConverter.Convert(triangleShape.VertexA), -normal, new Vector2(0, 0)));
			vertices.Add(new VertexPositionNormalTexture(MathConverter.Convert(triangleShape.VertexB), -normal, new Vector2(0, 1)));
			vertices.Add(new VertexPositionNormalTexture(MathConverter.Convert(triangleShape.VertexC), -normal, new Vector2(1, 0)));

			vertices.Add(new VertexPositionNormalTexture(MathConverter.Convert(triangleShape.VertexA), normal, new Vector2(0, 0)));
			vertices.Add(new VertexPositionNormalTexture(MathConverter.Convert(triangleShape.VertexB), normal, new Vector2(0, 1)));
			vertices.Add(new VertexPositionNormalTexture(MathConverter.Convert(triangleShape.VertexC), normal, new Vector2(1, 0)));

			indices.Add(0);
			indices.Add(1);
			indices.Add(2);

			indices.Add(3);
			indices.Add(5);
			indices.Add(4);
		}
	}
}