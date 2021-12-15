using BEPUutilities.DataStructures;
using Fusion.Engine.Graphics.Scenes;
using BEPUphysics;
using Fusion.Core.Mathematics;
using Fusion.Core;
using Fusion.Engine.Graphics;

namespace BEPUrender.Lines
{
	/// <summary>
	/// Renders bounding boxes of entities.
	/// </summary>
	public class BoundingBoxDrawer
	{
		public void Draw(DebugRender debugRender, Space space)
		{
			if (space.Entities.Count > 0)
			{
				foreach (var e in space.Entities)
				{
					debugRender.DrawBasis( MathConverter.Convert( e.WorldTransform ), 0.5f, 2 );

					Vector3[] boundingBoxCorners = MathConverter.Convert(e.CollisionInformation.BoundingBox.GetCorners());
					var color = e.ActivityInformation.IsActive ? Color.DarkRed : new Color(150, 100, 100);

					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[0], color));
					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[1], color));

					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[0], color));
					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[3], color));

					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[0], color));
					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[4], color));

					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[1], color));
					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[2], color));

					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[1], color));
					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[5], color));

					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[2], color));
					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[3], color));

					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[2], color));
					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[6], color));

					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[3], color));
					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[7], color));

					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[4], color));
					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[5], color));

					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[4], color));
					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[7], color));

					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[5], color));
					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[6], color));

					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[6], color));
					debugRender.PushVertex(new DebugVertex(boundingBoxCorners[7], color));
				}
			}
		}
	}
}
