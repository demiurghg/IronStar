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

namespace Fusion.Engine.Graphics 
{
	public enum InstanceGroup : uint 
	{
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
	public sealed class RenderInstance : DisposableBase 
	{
		private readonly RenderSystem rs;
		private readonly RenderWorld rw;

		public int InstanceRef
		{
			get 
			{	
				unchecked
				{
					return (Mesh.InstanceRef << 8) | ((int)(Group) & 0xFF);
				}
			}
		}


		public bool Visible 
		{ 
			get { return visible; }
			set
			{
				if (visible != value)
				{
					visible = value;
					isMoved = true;
				}
			}
		}

		bool visible = true;

		public InstanceGroup Group {
			get; set;
		}

		/// <summary>
		/// Instance world matrix. Default value is Matrix.Identity.
		/// </summary>
		public Matrix World 
		{
			get { return world; }
			set
			{
				if (world!=value)
				{
					worldBBoxDirty = true;
					isMoved = true;
					world = value;
				}
			}
		}

		bool isMoved = true;

		public bool IsShadowDirty
		{
			get 
			{
				return (isMoved || IsSkinned) && !NoShadow;
			}
		}

		public void ClearShadowDirty()
		{
			isMoved = false;
		}

		/// <summary>
		/// Instance color. Default value 0,0,0,0
		/// </summary>
		public Color4 Color 
		{
			get { return color; }
			set 
			{
				if ( color!=value )
				{
					color	=	value;
				}
			}
		}

		Matrix world;
		Color4 color;

		/// <summary>
		/// Gets and sets mesh.
		/// </summary>
		public Mesh Mesh { get; private set; }
		public Scene Scene { get; private set; }

		public BoundingBox LocalBoundingBox {
			get; private set;
		}

		bool worldBBoxDirty = true;
		BoundingBox worldBBox = new BoundingBox();

		/// <summary>
		/// Gets and sets surface effect.
		/// </summary>
		public InstanceFX InstanceFX {
			get; set;
		}

		public bool NoShadow { get; set; }

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
		public string LightMapRegionName {
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

		/// <summary>
		/// Creates instance from mesh in scene.
		/// </summary>
		/// <param name="rs"></param>
		/// <param name="mesh"></param>
		public RenderInstance ( RenderSystem rs, Scene scene, Mesh mesh )
		{
			this.rs		=	rs;
			this.rw		=	rs.RenderWorld;
			this.Scene	=	scene;

			Group		=	InstanceGroup.Static;
			World		=	Matrix.Identity;
			Color		=	Color4.Zero;

			vb			=	mesh.VertexBuffer;
			ib			=	mesh.IndexBuffer;
			
			vertexCount	=	mesh.VertexCount;
			indexCount	=	mesh.IndexCount;

			IsSkinned	=	mesh.IsSkinned;

			this.Mesh	=	mesh;

			if (IsSkinned && scene.Nodes.Count > RenderSystem.MaxBones) {
				throw new ArgumentOutOfRangeException( string.Format("Scene contains more than {0} bones and cannot be used for skinning.", RenderSystem.MaxBones ) );
			}

			if (IsSkinned) {
				BoneTransforms	=	Enumerable
					.Range(0, RenderSystem.MaxBones)
					.Select( i => Matrix.Identity )
					.ToArray();
			}

			LocalBoundingBox = mesh.ComputeBoundingBox();
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
			}

			base.Dispose( disposing );
		}


		public int GetSubsetCount()
		{
			return Mesh.Subsets.Count;
		}


		internal void GetSubsetData( int subsetIndex, ref SceneRenderer.SUBSET subsetData, out bool isTransparent, out int startPrimitive, out int primitiveCount )
		{
			var subset		=	Mesh.Subsets[ subsetIndex ];
			var material	=	Scene.Materials[ subset.MaterialIndex ];
			var name		=	material.Name;

			var segment		=	rw.VirtualTexture.GetTextureSegmentInfo( name );
			var region		=	segment.Region;

			subsetData.Color		=	segment.AverageColor.ToColor3();
			subsetData.MaxMip		=	segment.MaxMipLevel;
			subsetData.Rectangle	=	new Vector4( region.X, region.Y, region.Width, region.Height );

			startPrimitive	=	subset.StartPrimitive;
			primitiveCount	=	subset.PrimitiveCount;
			isTransparent	=	material.Transparent;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public BoundingBox ComputeWorldBoundingBox()
		{
			if (worldBBoxDirty)
			{
				worldBBox = BoundingBox.FromPoints( LocalBoundingBox.GetCorners().Select( p => Vector3.TransformCoordinate( p, World ) ) );
				worldBBoxDirty = false;
			}
			return worldBBox;
		}


		[Obsolete("Should not be used", true)]
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
