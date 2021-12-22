﻿using BEPUutilities;
using MathConverter = Fusion.Core.Mathematics.MathConverter;
using Color = Fusion.Core.Mathematics.Color;
using Fusion.Engine.Graphics.Scenes;
using System.Collections.Generic;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using VertexPositionNormalTexture = Fusion.Engine.Graphics.DebugVertex;
using BEPUutilities.DataStructures;

namespace BEPUrender.Models
{
	/// <summary>
	/// Superclass of display objects that follow entity collidables.
	/// </summary>
	public class DisplayEntityCollidable : ModelDisplayObject<EntityCollidable>
	{
		/// <summary>
		/// Constructs a new display entity.
		/// </summary>
		/// <param name="drawer">Drawer to use.</param>
		/// <param name="entityCollidable">EntityCollidable to draw.</param>
		public DisplayEntityCollidable(ModelDrawer drawer, EntityCollidable entityCollidable)
			: base(drawer, entityCollidable)
		{
		}

		public override void GetMeshData(RawList<VertexPositionNormalTexture> vertices, RawList<int> indices)
		{
			ModelDrawer.ShapeMeshGetters[DisplayedObject.GetType()](DisplayedObject, vertices, indices);
		}

		
		public override Color Color 
		{
			get 
			{ 
				float mass = DisplayedObject.Entity.Mass;

				return mass==0 ? new Color(255,0,0,128) : new Color(255,255,0,128);
			}
		}


		public override void Update()
		{
			if (DisplayedObject.Entity != null)
			{
				//The reason for this complexity is that we're drawing the shape's location directly and interpolated buffers might be active.
				//That means we can't rely solely on the collidable's world transform or the entity's world transform alone;
				//we must rebuild it from the entity's world transform and the collidable's local position.
				//TODO: This is awfully annoying.  Could use some built-in convenience methods to ease the usage.
				Vector3 translation = Matrix3x3.Transform(DisplayedObject.LocalPosition, DisplayedObject.Entity.BufferedStates.InterpolatedStates.OrientationMatrix);
				translation += DisplayedObject.Entity.BufferedStates.InterpolatedStates.Position;
				Matrix worldTransform = Matrix3x3.ToMatrix4X4(DisplayedObject.Entity.BufferedStates.InterpolatedStates.OrientationMatrix);
				worldTransform.Translation = translation;
				WorldTransform = MathConverter.Convert(worldTransform);
			}
			else
			{
				//Entityless EntityCollidables just go by what their current transform is.
				WorldTransform = MathConverter.Convert(DisplayedObject.WorldTransform.Matrix);
			}
		}
	}
}