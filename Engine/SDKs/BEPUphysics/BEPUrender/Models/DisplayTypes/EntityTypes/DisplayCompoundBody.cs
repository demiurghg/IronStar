using System.Collections.Generic;
using Fusion.Drivers.Graphics;
using System;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using Fusion.Engine.Graphics.Scenes;
using MathConverter = Fusion.Core.Mathematics.MathConverter;
using VertexPositionNormalTexture = Fusion.Engine.Graphics.DebugVertex;
using BEPUutilities.DataStructures;

namespace BEPUrender.Models
{
	/// <summary>
	/// Helper class that can create shape mesh data.
	/// </summary>
	public static class DisplayCompoundBody
	{

		public static void GetShapeMeshData(EntityCollidable collidable, RawList<VertexPositionNormalTexture> vertices, RawList<int> indices)
		{
			var compoundCollidable = collidable as CompoundCollidable;
			if (compoundCollidable == null)
				throw new ArgumentException("Wrong shape type.");
			var tempIndices = new RawList<int>();
			var tempVertices = new RawList<VertexPositionNormalTexture>();
			for (int i = 0; i < compoundCollidable.Children.Count; i++)
			{
				var child = compoundCollidable.Children[i];
				ModelDrawer.ShapeMeshGetter shapeMeshGetter;
				if (ModelDrawer.ShapeMeshGetters.TryGetValue(child.CollisionInformation.GetType(), out shapeMeshGetter))
				{
					shapeMeshGetter(child.CollisionInformation, tempVertices, tempIndices);

					for (int j = 0; j < tempIndices.Count; j++)
					{
						indices.Add((ushort)(tempIndices[j] + vertices.Count));
					}
					var localTransform = child.Entry.LocalTransform;
					var localPosition = MathConverter.Convert(child.CollisionInformation.LocalPosition);
					var orientation = MathConverter.Convert(localTransform.Orientation);
					var position = MathConverter.Convert(localTransform.Position);
					for (int j = 0; j < tempVertices.Count; j++)
					{
						VertexPositionNormalTexture vertex = tempVertices[j];
						Fusion.Core.Mathematics.Vector3.Add(ref vertex.Position, ref localPosition, out vertex.Position);
						Fusion.Core.Mathematics.Vector3.Transform(ref vertex.Position, ref orientation, out vertex.Position);
						Fusion.Core.Mathematics.Vector3.Add(ref vertex.Position, ref position, out vertex.Position);
						Fusion.Core.Mathematics.Vector3.Transform(ref vertex.Normal, ref orientation, out vertex.Normal);
						vertices.Add(vertex);
					}

					tempVertices.Clear();
					tempIndices.Clear();
				}
			}
		}
	}
}