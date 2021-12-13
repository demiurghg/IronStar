using System;
using System.Collections.Generic;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.DataStructures;
using BEPUphysics.Entities;
using BEPUphysics.UpdateableSystems;
using Matrix = BEPUutilities.Matrix;
using Color = Fusion.Core.Mathematics;
using Fusion.Engine.Graphics.Scenes;
using Fusion.Engine.Graphics;
using VertexPositionNormalTexture = Fusion.Engine.Graphics.DebugVertex;
using BEPUutilities.DataStructures;

namespace BEPUrender.Models
{
	/// <summary>
	/// Manages and draws models.
	/// </summary>
	public class ModelDrawer
	{
		public delegate void ShapeMeshGetter(EntityCollidable collidable, RawList<VertexPositionNormalTexture> vertices, RawList<int> indices);

		private readonly Dictionary<object, ModelDisplayObject> displayObjects = new Dictionary<object, ModelDisplayObject>();

		private readonly List<SelfDrawingModelDisplayObject> selfDrawingDisplayObjects = new List<SelfDrawingModelDisplayObject>();

		private static readonly Dictionary<Type, Type> displayTypes = new Dictionary<Type, Type>();
		private static readonly Dictionary<Type, ShapeMeshGetter> shapeMeshGetters = new Dictionary<Type, ShapeMeshGetter>();

		RawList<DebugVertex>	vertexData	=	new RawList<DebugVertex>(64);
		RawList<int>			indexData	=	new RawList<int>		(64);

		/// <summary>
		/// Gets the map from object types to display object types.
		/// </summary>
		public static Dictionary<Type, Type> DisplayTypes
		{
			get { return displayTypes; }
		}

		/// <summary>
		/// Gets the map from shape object types to methods which can be used to construct the data.
		/// </summary>
		public static Dictionary<Type, ShapeMeshGetter> ShapeMeshGetters
		{
			get { return shapeMeshGetters; }
		}

		static ModelDrawer()
		{
			//Display types are sometimes requested from contexts lacking a convenient reference to a ModelDrawer instance.
			//Having them static simplifies things.
			displayTypes.Add(typeof(FluidVolume), typeof(DisplayFluid));
			displayTypes.Add(typeof(Terrain), typeof(DisplayTerrain));
			displayTypes.Add(typeof(TriangleMesh), typeof(DisplayTriangleMesh));
			displayTypes.Add(typeof(StaticMesh), typeof(DisplayStaticMesh));
			displayTypes.Add(typeof(InstancedMesh), typeof(DisplayInstancedMesh));

			//Entity types are handled through a special case that uses an Entity's Shape to look up one of the ShapeMeshGetters.
			shapeMeshGetters.Add(typeof(ConvexCollidable<BoxShape>), DisplayBox.GetShapeMeshData);
			shapeMeshGetters.Add(typeof(ConvexCollidable<SphereShape>), DisplaySphere.GetShapeMeshData);
			shapeMeshGetters.Add(typeof(ConvexCollidable<CapsuleShape>), DisplayCapsule.GetShapeMeshData);
			shapeMeshGetters.Add(typeof(ConvexCollidable<CylinderShape>), DisplayCylinder.GetShapeMeshData);
			shapeMeshGetters.Add(typeof(ConvexCollidable<ConeShape>), DisplayCone.GetShapeMeshData);
			shapeMeshGetters.Add(typeof(ConvexCollidable<TriangleShape>), DisplayTriangle.GetShapeMeshData);
			shapeMeshGetters.Add(typeof(ConvexCollidable<ConvexHullShape>), DisplayConvexHull.GetShapeMeshData);
			shapeMeshGetters.Add(typeof(ConvexCollidable<MinkowskiSumShape>), DisplayConvex.GetShapeMeshData);
			shapeMeshGetters.Add(typeof(ConvexCollidable<WrappedShape>), DisplayConvex.GetShapeMeshData);
			shapeMeshGetters.Add(typeof(ConvexCollidable<TransformableShape>), DisplayConvex.GetShapeMeshData);
			shapeMeshGetters.Add(typeof(CompoundCollidable), DisplayCompoundBody.GetShapeMeshData);
			shapeMeshGetters.Add(typeof(MobileMeshCollidable), DisplayMobileMesh.GetShapeMeshData);
		}


		readonly DebugRender debugRender;


		public ModelDrawer(DebugRender debugRender)
		{
			this.debugRender	=	debugRender;
		}


		/// <summary>
		/// Constructs a new display object for an object.
		/// </summary>
		/// <param name="objectToDisplay">Object to create a display object for.</param>
		/// <returns>Display object for an object.</returns>
		public ModelDisplayObject GetDisplayObject(object objectToDisplay)
		{
			Type displayType;

			if (!displayObjects.ContainsKey(objectToDisplay))
			{
				if (displayTypes.TryGetValue(objectToDisplay.GetType(), out displayType))
				{
					return (ModelDisplayObject)Activator.CreateInstance(displayType, new[] { this, objectToDisplay });
				}

				Entity e;
				if ((e = objectToDisplay as Entity) != null)
				{
					return new DisplayEntityCollidable(this, e.CollisionInformation);
				}

				EntityCollidable entityCollidable;
				if ((entityCollidable = objectToDisplay as EntityCollidable) != null)
				{
					return new DisplayEntityCollidable(this, entityCollidable);
				}

			}
			return null;
		}


		/// <summary>
		/// Attempts to add an object to the ModelDrawer.
		/// </summary>
		/// <param name="objectToDisplay">Object to be added to the model drawer.</param>
		/// <returns>ModelDisplayObject created for the object.  Null if it couldn't be added.</returns>
		public ModelDisplayObject Add(object objectToDisplay)
		{
			ModelDisplayObject displayObject = GetDisplayObject(objectToDisplay);

			if (displayObject != null)
			{
				Add(displayObject);
				displayObjects.Add(objectToDisplay, displayObject);
				return displayObject;
			}
			
			return null; //Couldn't add it.
		}

		/// <summary>
		/// Adds the display object to the drawer.
		/// </summary>
		/// <param name="displayObject">Display object to add.</param>
		/// <returns>Whether or not the display object was added.</returns>
		public bool Add(SelfDrawingModelDisplayObject displayObject)
		{
			if (!selfDrawingDisplayObjects.Contains(displayObject))
			{
				selfDrawingDisplayObjects.Add(displayObject);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Adds a display object directly to the drawer without being linked to a source.
		/// </summary>
		/// <param name="displayObject">Display object to add.</param>
		public virtual void Add(ModelDisplayObject displayObject)
		{
			vertexData.Clear();
			indexData.Clear();

			displayObject.GetMeshData( vertexData, indexData );

			displayObject.Model = new DebugModel( debugRender, vertexData.Elements, indexData.Elements );
			displayObject.Model.Color = new Color.Color( 128,64,32,255 );

			debugRender.AddModel( displayObject.Model );
		}

		/// <summary>
		/// Removes an object from the drawer.
		/// </summary>
		/// <param name="objectToRemove">Object to remove.</param>
		/// <returns>Whether or not the object was present.</returns>
		public bool Remove(object objectToRemove)
		{
			ModelDisplayObject displayObject;
			if (displayObjects.TryGetValue(objectToRemove, out displayObject))
			{
				Remove(displayObject);
				displayObjects.Remove(objectToRemove);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Removes an object from the drawer.
		/// </summary>
		/// <param name="displayObject">Display object to remove.</param>
		/// <returns>Whether or not the object was present.</returns>
		public bool Remove(SelfDrawingModelDisplayObject displayObject)
		{
			return selfDrawingDisplayObjects.Remove(displayObject);
		}


		/// <summary>
		/// Removes a display object from the drawer.  Only use this if display object was added directly.
		/// </summary>
		/// <param name="displayObject">Object to remove.</param>
		public virtual void Remove(ModelDisplayObject displayObject)
		{
			debugRender.RemoveModel( displayObject?.Model );
		}

		/// <summary>
		/// Cleans out the model drawer of any existing display objects.
		/// </summary>
		public void Clear()
		{
			displayObjects.Clear();
			selfDrawingDisplayObjects.Clear();
			ClearManagedModels();
		}

		/// <summary>
		/// Cleans out any data contained by derived drawers.
		/// </summary>
		protected virtual void ClearManagedModels()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Determines if the object has an associated display object in this drawer.
		/// </summary>
		/// <param name="displayedObject">Object to check for in the drawer.</param>
		/// <returns>Whether or not the object has an associated display object in this drawer.</returns>
		public bool Contains(object displayedObject)
		{
			return displayObjects.ContainsKey(displayedObject);
		}

		/// <summary>
		/// Updates the drawer and its components.
		/// </summary>
		public void Update()
		{
			foreach (SelfDrawingModelDisplayObject displayObject in selfDrawingDisplayObjects)
			{
				displayObject.Update();
			}

			UpdateManagedModels();
		}

		/// <summary>
		/// Updates the drawer's technique.
		/// </summary>
		protected virtual void UpdateManagedModels()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Draws the drawer's models.
		/// </summary>
		/// <param name="viewMatrix">View matrix to use to draw the objects.</param>
		/// <param name="projectionMatrix">Projection matrix to use to draw the objects.</param>
		public void Draw(DebugRender debugRender)
		{
			foreach (var pair in displayObjects)
			{
				var modelDisplayObject			=	pair.Value;
				modelDisplayObject.Update();

				((DebugRenderAsync)debugRender).SetTransform( modelDisplayObject.Model, modelDisplayObject.WorldTransform );
			}

			foreach (SelfDrawingModelDisplayObject displayObject in selfDrawingDisplayObjects)
			{
				displayObject.Draw(Matrix.Identity, Matrix.Identity);
			}
		}

		/// <summary>
		/// Draws the models managed by the drawer using the appropriate technique.
		/// </summary>
		/// <param name="viewMatrix">View matrix to use to draw the objects.</param>
		/// <param name="projectionMatrix">Projection matrix to use to draw the objects.</param>
		protected virtual void DrawManagedModels(Matrix viewMatrix, Matrix projectionMatrix)
		{
			throw new NotImplementedException();
		}


	}
}