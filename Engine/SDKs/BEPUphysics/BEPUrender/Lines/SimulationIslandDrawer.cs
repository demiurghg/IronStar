using Fusion.Core;
using System.Collections.Generic;
using BEPUutilities.DataStructures;
using Fusion.Engine.Graphics.Scenes;
using BEPUphysics;
using Fusion.Core.Mathematics;
using Fusion.Core;
using BEPUphysics.DeactivationManagement;
using MathConverter = Fusion.Core.Mathematics.MathConverter;
using Fusion.Engine.Graphics;

namespace BEPUrender.Lines
{
	/// <summary>
	/// Renders bounding boxes of simulation islands.
	/// </summary>
	public class SimulationIslandDrawer
	{
		Dictionary<SimulationIsland, BoundingBox> islandBoundingBoxes = new Dictionary<SimulationIsland, BoundingBox>();

		public void Draw(DebugRender debugRender, Space space)
		{
			if (space.Entities.Count > 0)
			{
				BoundingBox box;
				foreach (var entity in space.Entities)
				{
					var island = entity.ActivityInformation.SimulationIsland;
					if (island != null)
					{
						if (islandBoundingBoxes.TryGetValue(island, out box))
						{
							box = BoundingBox.CreateMerged(MathConverter.Convert(entity.CollisionInformation.BoundingBox), box);
							islandBoundingBoxes[island] = box;
						}
						else
						{
							islandBoundingBoxes.Add(island, MathConverter.Convert(entity.CollisionInformation.BoundingBox));
						}
					}
				}
				
				foreach (var islandBoundingBox in islandBoundingBoxes)
				{
					Color colorToUse = islandBoundingBox.Key.IsActive ? Color.DarkGoldenrod : Color.DarkGray;
					Vector3[] boundingBoxCorners = islandBoundingBox.Value.GetCorners();

					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[0], colorToUse));
					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[1], colorToUse));

					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[0], colorToUse));
					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[3], colorToUse));

					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[0], colorToUse));
					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[4], colorToUse));

					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[1], colorToUse));
					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[2], colorToUse));

					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[1], colorToUse));
					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[5], colorToUse));

					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[2], colorToUse));
					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[3], colorToUse));

					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[2], colorToUse));
					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[6], colorToUse));

					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[3], colorToUse));
					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[7], colorToUse));

					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[4], colorToUse));
					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[5], colorToUse));

					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[4], colorToUse));
					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[7], colorToUse));

					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[5], colorToUse));
					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[6], colorToUse));

					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[6], colorToUse));
					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[7], colorToUse));

				}

				islandBoundingBoxes.Clear();
			}
		}
	}
}
