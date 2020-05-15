using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Common;
using Fusion.Core.Content;
using Native.Embree;
using Fusion.Engine.Graphics.Scenes;

namespace Fusion.Engine.Graphics {

	public enum InstanceGroup : uint {
		Static		=	0x00000001,
		Kinematic	=	0x00000002,
		Dynamic		=	0x00000004,
		Character	=	0x00000008,
		Weapon		=	0x00000010,
		NotWeapon	=	~Weapon,
		All			=	0xFFFFFFFF,
	}


	
	/// <summary>
	/// Represnets mesh instance
	/// </summary>
	public class RenderInstance {

		public InstanceGroup Group {
			get; set;
		}

		/// <summary>
		/// Instance world matrix. Default value is Matrix.Identity.
		/// </summary>
		public Matrix World {
			get; set;
		}

		/// <summary>
		/// Instance color. Default value 0,0,0,0
		/// </summary>
		public Color4 Color {
			get; set;
		}

		/// <summary>
		/// Gets and sets mesh.
		/// </summary>
		public Mesh Mesh {
			get; private set;
		}

		public BoundingBox LocalBoundingBox {
			get; private set;
		}

		/// <summary>
		/// Gets and sets surface effect.
		/// </summary>
		public InstanceFX InstanceFX {
			get; set;
		}

		/// <summary>
		/// Gets whether mesh is skinned.
		/// </summary>
		public bool IsSkinned {
			get; private set;
		}

		/// <summary>
		/// Gets array of bone transforms.
		/// If skinning is not applied to mesh, this array is Null.
		/// </summary>
		public Matrix[] BoneTransforms {
			get; private set;
		}

		/// <summary>
		/// Gets array of bone colors.
		/// If skinning is not applied to mesh, this array is Null.
		/// </summary>
		public Color4[] BoneColors {
			get; private set;
		}

		/// <summary>
		/// Sets and gets lightmaps region size
		/// </summary>
		public Size2 LightMapSize {
			get; set;
		}

		/// <summary>
		/// Tag
		/// </summary>
		public Guid LightMapGuid {
			get; set;
		}

		public Vector4 LightMapScaleOffset {
			get; set;
		}

		public Rectangle BakingLMRegion;

		/// <summary>
		/// Tag
		/// </summary>
		public object Tag {
			get; set;
		}


		readonly internal VertexBuffer	vb;
		readonly internal IndexBuffer	ib;

		readonly internal int indexCount;
		readonly internal int vertexCount;


		public struct Subset {
			public int StartPrimitive;
			public int PrimitiveCount;
			public string Name;
		}


		internal readonly Subset[] Subsets;


		/// <summary>
		/// Creates instance from mesh in scene.
		/// </summary>
		/// <param name="rs"></param>
		/// <param name="mesh"></param>
		public RenderInstance ( RenderSystem rs, Scene scene, Mesh mesh )
		{
			Group		=	InstanceGroup.Static;
			World		=	Matrix.Identity;
			Color		=	Color4.Zero;

			vb			=	mesh.VertexBuffer;
			ib			=	mesh.IndexBuffer;
			
			vertexCount	=	mesh.VertexCount;
			indexCount	=	mesh.IndexCount;

			IsSkinned	=	mesh.IsSkinned;

			this.Mesh	=	mesh;

			if (IsSkinned && scene.Nodes.Count > SceneRenderer.MaxBones) {
				throw new ArgumentOutOfRangeException( string.Format("Scene contains more than {0} bones and cannot be used for skinning.", SceneRenderer.MaxBones ) );
			}

			if (IsSkinned) {
				BoneTransforms	=	Enumerable
					.Range(0, SceneRenderer.MaxBones)
					.Select( i => Matrix.Identity )
					.ToArray();
			}

			LocalBoundingBox = mesh.ComputeBoundingBox();

			Subsets	=	mesh.Subsets.Select( subset => new Subset() { 
					Name			= scene.Materials[subset.MaterialIndex].Name, 
					PrimitiveCount	= subset.PrimitiveCount,
					StartPrimitive	= subset.StartPrimitive 
				}).ToArray();
		}



		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public BoundingBox ComputeWorldBoundingBox()
		{
			return BoundingBox.FromPoints( LocalBoundingBox.GetCorners().Select( p => Vector3.TransformCoordinate( p, World ) ) );
		}


		public static RenderInstance FromScene ( RenderSystem rs, ContentManager content, string pathNode )
		{
			if (string.IsNullOrWhiteSpace(pathNode)) {
				return null;
			}

			var pair	=	pathNode.Split(new[] {'|'}, 2);

			var path	=	pair[0];
			var name	=	(pair.Length==2) ? pair[1] : null;
			
			var scene 	=	content.Load<Scene>(path);

			if (!scene.Meshes.Any()) {
				Log.Warning("Scene does not contain meshes.");
				return null;
			}


			var node	=	scene.Nodes.FirstOrDefault( n => n.Name == name );

			Mesh mesh;

			if (node==null) {
				Log.Warning("Node name is not provided or node does not exist. First mesh is used.");
				mesh	=	scene.Meshes.FirstOrDefault();
			} else {
				mesh	=	scene.Meshes[ node.MeshIndex ];
			}

			return new RenderInstance( rs, scene, mesh );
		}
	}
}
