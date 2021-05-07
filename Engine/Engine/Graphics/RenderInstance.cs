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

		public bool Visible = true;


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
					world = value;
					instanceDataDirty = true;
				}
			}
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
					instanceDataDirty = true;
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

		bool				instanceDataDirty	=	true;
		bool				subsetDataDirty		=	true;
		bool[]				transparency;
		ConstantBuffer		instanceCData;
		ConstantBuffer[]	subsetCData;


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

			instanceCData	=	new ConstantBuffer( rs.Device, typeof(SceneRenderer.INSTANCE) );
			subsetCData		=	mesh.Subsets.Select( subset => new ConstantBuffer( rs.Device, typeof(SceneRenderer.SUBSET) ) ).ToArray();
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				SafeDispose( ref instanceCData );
				SafeDispose( ref subsetCData );
			}

			base.Dispose( disposing );
		}


		public void MakeGpuDataDirty()
		{
			instanceDataDirty	=	true;
			subsetDataDirty		=	true;
		}


		public ConstantBuffer GetInstanceData()
		{
			var data = new SceneRenderer.INSTANCE();
			
			if (instanceDataDirty)
			{
				data.World		=	World;
				data.Color		=	Color;
				data.LMRegion	=	LightMapScaleOffset;
				data.Group		=	(int)Group;

				instanceCData.SetData( ref data );
				instanceDataDirty = false;
			}

			return instanceCData;
		}


		public int GetSubsetCount()
		{
			return subsetCData.Length;
		}


		public ConstantBuffer GetSubsetData(int subsetIndex, out bool isTransparent, out int startPrimitive, out int primitiveCount)
		{
			var data = new SceneRenderer.SUBSET();
			
			if (subsetDataDirty)
			{
				transparency	=	new bool[subsetCData.Length];

				for (int i=0; i<subsetCData.Length; i++)
				{
					var subset		=	Mesh.Subsets[ i ];
					var material	=	Scene.Materials[ subset.MaterialIndex ];
					var name		=	material.Name;

					var segment		=	rw.VirtualTexture.GetTextureSegmentInfo( name );
					var region		=	segment.Region;

					transparency[i]	=	segment.Transparent;

					data.Color		=	segment.AverageColor;
					data.MaxMip		=	segment.MaxMipLevel;
					data.Rectangle	=	new Vector4( region.X, region.Y, region.Width, region.Height );
					data.Dummy1		=	0;
					data.Dummy2		=	0;
					data.Dummy3		=	0;

					subsetCData[i].SetData( ref data );
				}

				subsetDataDirty = false;
			}

			startPrimitive	=	Mesh.Subsets[ subsetIndex ].StartPrimitive;
			primitiveCount	=	Mesh.Subsets[ subsetIndex ].PrimitiveCount;
			isTransparent	=	transparency[ subsetIndex ];

			return subsetCData[ subsetIndex ];
		}


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public BoundingBox ComputeWorldBoundingBox()
		{
			return BoundingBox.FromPoints( LocalBoundingBox.GetCorners().Select( p => Vector3.TransformCoordinate( p, World ) ) );
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
