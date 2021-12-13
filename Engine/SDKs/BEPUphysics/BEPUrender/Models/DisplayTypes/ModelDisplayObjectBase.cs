
using System;
using System.Collections.Generic;
using Fusion.Core.Mathematics;
using Fusion.Core;
using Fusion.Engine.Graphics.Scenes;
using VertexPositionNormalTexture = Fusion.Engine.Graphics.DebugVertex;
using BEPUutilities.DataStructures;
using Fusion.Engine.Graphics;

namespace BEPUrender.Models
{
	/// <summary>
	/// Base class of ModelDisplayObjects.
	/// </summary>
	public abstract class ModelDisplayObject
	{
		protected static Random Random = new Random();


		protected ModelDisplayObject(ModelDrawer drawer)
		{
			Drawer = drawer;
			//BatchInformation = new BatchInformation();
			//TextureIndex = Random.Next(8);
		}

		public DebugModel Model { get; set; }

		/// <summary>
		/// Gets or sets the world transform of the display object.
		/// </summary>
		public Matrix WorldTransform { get; set; }

		public abstract Color Color { get; }

		/// <summary>
		/// Gets the drawer that this display object belongs to.
		/// </summary>
		public ModelDrawer Drawer { get; private set; }

		/// <summary>
		/// Collects the local space vertex data of the model.
		/// </summary>
		/// <param name="vertices">List of vertices to be filled with the model vertices.</param>
		/// <param name="indices">List of indices to be filled with the model indices.</param>
		public abstract void GetMeshData(RawList<VertexPositionNormalTexture> vertices, RawList<int> indices);

		/// <summary>
		/// Updates the display object and reports the world transform.
		/// </summary>
		public abstract void Update();
	}
}