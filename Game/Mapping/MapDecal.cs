using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Fusion.Core.Mathematics;
using IronStar.Core;
using Fusion.Engine.Graphics;
using IronStar.SFX;
using Fusion.Development;
using System.Drawing.Design;
using Fusion.Core.Shell;

namespace IronStar.Mapping {
	public class MapDecal : MapNode {


		[AECategory("Decal Image")]
		[AEAtlasImage(@"decals/decals")]
		public string ImageName { get; set; } = "";

		/// <summary>
		/// 
		/// </summary>
		[AECategory("Decal Size")]
		[AEValueRange(0, 64, 1f, 1/16f)]
		public float Width { get; set;} = 4;

		/// <summary>
		/// 
		/// </summary>
		[AECategory("Decal Size")]
		[AEValueRange(0, 64, 1f, 1/16f)]
		public float Height { get; set;} = 4;

		/// <summary>
		/// 
		/// </summary>
		[AECategory("Decal Size")]
		[AEValueRange(0, 16, 1f, 1/16f)]
		public float Depth { get; set;} = 1;

		/// <summary>
		/// Decal emission intensity
		/// </summary>
		[AECategory("Decal Material")]
		public Color EmissionColor { get; set;} = Color.Black;

		/// <summary>
		/// Decal emission intensity
		/// </summary>
		[AECategory("Decal Material")]
		public float EmissionIntensity { get; set;} = 100;

		/// <summary>
		/// Decal base color
		/// </summary>
		[AECategory("Decal Material")]
		public Color BaseColor { get; set;} = new Color(128,128,128,255);

		/// <summary>
		/// Decal roughness
		/// </summary>
		[AECategory("Decal Material")]
		[AEValueRange(0, 1, 1/4f, 1/128f)]
		public float Roughness { get; set;}= 0.5f;

		/// <summary>
		/// Decal meatllic
		/// </summary>
		[AECategory("Decal Material")]
		[AEValueRange(0, 1, 1/4f, 1/128f)]
		public float Metallic { get; set;} = 0.5f;

		/// <summary>
		/// Color blend factor [0,1]
		/// </summary>
		[AECategory("Decal Material")]
		[AEValueRange(0, 1, 1/4f, 1/128f)]
		public float ColorFactor { get; set;} = 1.0f;

		/// <summary>
		/// Roughmess and specular blend factor [0,1]
		/// </summary>
		[AECategory("Decal Material")]
		[AEValueRange(0, 1, 1/4f, 1/128f)]
		public float SpecularFactor { get; set;} = 1.0f;

		/// <summary>
		/// Normalmap blend factor [-1,1]
		/// </summary>
		[AECategory("Decal Material")]
		[AEValueRange(0, 1, 1/4f, 1/128f)]
		public float NormalMapFactor { get; set;} = 1.0f;

		/// <summary>
		/// Falloff factor [-1,1]
		/// </summary>
		[AECategory("Decal Material")]
		[AEValueRange(0, 1, 1/4f, 1/128f)]
		public float FalloffFactor { get; set;} = 0.5f;
		

		Decal decal = null;


		/// <summary>
		/// 
		/// </summary>
		public MapDecal ()
		{
		}



		public override void SpawnNode( GameWorld world )
		{
			if ( string.IsNullOrWhiteSpace(ImageName)) {
				return;
			}

			var rw	=	world.Game.RenderSystem.RenderWorld;
			var ls	=	rw.LightSet;

			decal	=	new Decal();

			decal.DecalMatrix		=	Matrix.Scaling( Width/2, Height/2, Depth/2 ) * WorldMatrix;
			decal.DecalMatrixInverse=	Matrix.Invert( decal.DecalMatrix );
									
			decal.Emission			=	EmissionColor.ToColor4() * EmissionIntensity;
			decal.BaseColor			=	new Color4( BaseColor.R/255.0f, BaseColor.G/255.0f, BaseColor.B/255.0f, 1 );
			
			decal.Metallic			=	Metallic;
			decal.Roughness			=	Roughness;
			decal.ImageRectangle	=	ls.DecalAtlas.GetNormalizedRectangleByName( ImageName );
			decal.ImageSize			=	ls.DecalAtlas.GetAbsoluteRectangleByName( ImageName ).Size;

			//decal.ImageRectangle	=	ls.DecalAtlas.GetClipByName( ImageName ).FirstIndex

			decal.ColorFactor		=	ColorFactor;
			decal.SpecularFactor	=	SpecularFactor;
			decal.NormalMapFactor	=	NormalMapFactor;
			decal.FalloffFactor		=	FalloffFactor;

			decal.Group				=	InstanceGroup.Static;

			world.Game.RenderSystem.RenderWorld.LightSet.Decals.Add( decal );
		}



		public override void ActivateNode()
		{
		}



		public override void UseNode()
		{
		}



		public override void DrawNode( GameWorld world, DebugRender dr, Color color, bool selected )
		{
			var transform	=	WorldMatrix;

			var c	= transform.TranslationVector 
					+ transform.Left * Width * 0.40f
					+ transform.Up   * Height * 0.40f;

			float len = Math.Min( Width, Height ) / 6;

			var x  = transform.Right * len;
			var y  = transform.Down * len;
			var z  = transform.Backward * len;

			var p0 = Vector3.TransformCoordinate( new Vector3(  Width/2,  Height/2, 0 ), transform ); 
			var p1 = Vector3.TransformCoordinate( new Vector3( -Width/2,  Height/2, 0 ), transform ); 
			var p2 = Vector3.TransformCoordinate( new Vector3( -Width/2, -Height/2, 0 ), transform ); 
			var p3 = Vector3.TransformCoordinate( new Vector3(  Width/2, -Height/2, 0 ), transform ); 

			var p4 = Vector3.TransformCoordinate( new Vector3( 0, 0,  Depth ), transform ); 
			var p5 = Vector3.TransformCoordinate( new Vector3( 0, 0, -Depth ), transform ); 

			dr.DrawLine( p0, p1, color, color, 1, 1 );
			dr.DrawLine( p1, p2, color, color, 1, 1 );
			dr.DrawLine( p2, p3, color, color, 1, 1 );
			dr.DrawLine( p3, p0, color, color, 1, 1 );

			dr.DrawLine( c, c+x, Color.Red  , Color.Red  , 2, 2 );
			dr.DrawLine( c, c+y, Color.Lime , Color.Lime , 2, 2 );
			dr.DrawLine( c, c+z, Color.Blue , Color.Blue , 5, 1 );

			dr.DrawLine( p4, p5, color, color, 2, 2 );
		}



		public override void KillNode( GameWorld world )
		{
			world.Game.RenderSystem.RenderWorld.LightSet.Decals.Remove( decal );
		}


		public override MapNode DuplicateNode(GameWorld world)
		{
			var newNode = (MapDecal)MemberwiseClone();
			newNode.decal = null;
			newNode.NodeGuid = Guid.NewGuid();
			return newNode;
		}

		public override BoundingBox GetBoundingBox()
		{
			return new BoundingBox( Width, Height, 0.25f );
		}
	}
}
