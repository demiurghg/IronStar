using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using IronStar.SFX;
using Fusion.Development;
using System.Drawing.Design;
using Fusion;
using Fusion.Core.Shell;
using IronStar.ECS;
using Fusion.Widgets.Advanced;

namespace IronStar.Mapping {

	public class MapSpotLight : MapNode, IEntityFactory {


		[AECategory("Light Color")]
		[AEDisplayName("Light Color")]
		public Color LightColor { get; set; } = Color.White;

		[AECategory("Light Color")]
		[AEDisplayName("Intensity")]
		[AESlider(0, 16, 1, 1f/16f)]
		public float LightIntensity { get; set; } = 5;
		


		[AECategory("Spot Shape")]
		[AEDisplayName("Outer Radius")]
		[AESlider(0, 100, 1, 0.125f)]
		public float OuterRadius { get; set; } = 5;
		
		[AECategory("Spot Shape")]
		[AEDisplayName("Tube Radius")]
		[AESlider(0, 50, 1, 0.125f)]
		public float TubeRadius { get; set; } = 0.125f;
		
		[AECategory("Spot Shape")]
		[AEDisplayName("Tube Length")]
		[AESlider(0, 50, 1, 0.125f)]
		public float TubeLength { get; set; } = 0.0f;
		
		[AECategory("Spot Shape")]
		[AESlider(0, 4, 1/4f, 1/64f)]
		public float NearPlane { get; set; } = 0.125f;
		
		[AECategory("Spot Shape")]
		[AESlider(0, 100, 1, 1/8f)]
		public float FarPlane { get; set; } = 5;
		
		[AECategory("Spot Shape")]
		[AESlider(0, 150, 15, 1)]
		public float FovVertical { get; set; } = 60;
		
		[AECategory("Spot Shape")]
		[AESlider(0, 150, 15, 1)]
		public float FovHorizontal { get; set; } = 60;

		[AECategory("Spot-light")]
		public LightStyle LightStyle { get; set; } = LightStyle.Default;



		[AECategory("Global Illumination")]
		[AEDisplayName("Enable GI")]
		public bool EnableGI { get; set; } = false;

		[AECategory("Spot Shadow")]
		[AEDisplayName("Spot Mask")]
		[AEAtlasImage("spots/spots")]
		public string SpotMaskName { get; set; } = "";
		
		[AECategory("Spot Shadow")]
		[AEDisplayName("Shadow LOD Bias")]
		[AESlider(0, 8, 1, 1)]
		public int LodBias { get; set; } = 0;
		

		Matrix SpotView {
			get {
				return	Matrix.Invert(Transform);
			}
		}

		Matrix SpotProjection {
			get {
				float n		=	NearPlane;
				float f		=	FarPlane;
				float w		=	(float)Math.Tan( MathUtil.DegreesToRadians( FovHorizontal/2 ) ) * NearPlane * 2;
				float h		=	(float)Math.Tan( MathUtil.DegreesToRadians( FovVertical/2	) ) * NearPlane * 2;
				return	Matrix.PerspectiveRH( w, h, n, f );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		public MapSpotLight ()
		{
		}



		public override void SpawnNodeECS( IGameState gs )
		{
			ecsEntity		=	gs.Spawn( this );
			ecsEntity.Tag	=	this;
		}


		public void Construct( Entity entity, IGameState gs )
		{
			entity.AddComponent( new Transform( Translation, Rotation, 1 ) );
			entity.AddComponent( CreateSpotLight() );
		}


		SFX2.SpotLight CreateSpotLight()
		{
			var light = new SFX2.SpotLight();

			light.TubeLength		=	TubeLength;
			light.TubeRadius		=	TubeRadius;
			light.OuterRadius		=	OuterRadius;
			light.LightColor		=	LightColor;
			light.LightIntensity	=	LightIntensity;

			light.EnableGI			=	EnableGI;
			light.SpotMaskName		=	SpotMaskName;
			light.LodBias			=	LodBias;

			light.FovHorizontal		=	FovHorizontal;
			light.FovVertical		=	FovVertical;
			light.FarPlane			=	FarPlane;
			light.NearPlane			=	NearPlane;

			return light;
		}


		//public override void DrawNode( GameWorld world, DebugRender dr, Color color, bool selected )
		//{
		//	var transform	=	WorldMatrix;
		//	var dispColor   =	LightColor;

		//	dr.DrawPoint( transform.TranslationVector, 1, color, 1 );

		//	var position	=	WorldMatrix.TranslationVector;
		//	var position0	=	WorldMatrix.TranslationVector + WorldMatrix.Right * TubeLength * 0.5f;
		//	var position1	=	WorldMatrix.TranslationVector + WorldMatrix.Left  * TubeLength * 0.5f;

		//	if (selected) 
		//	{
		//		dr.DrawSphere( position0, TubeRadius,  dispColor );
		//		dr.DrawSphere( position1, TubeRadius,  dispColor );
		//		dr.DrawSphere( position,  OuterRadius, dispColor );

		//		var frustum = new BoundingFrustum( ViewMatrix * SpotProjection );
				
		//		var points  = frustum.GetCorners();

		//		dr.DrawLine( points[0], points[1], dispColor );
		//		dr.DrawLine( points[1], points[2], dispColor );
		//		dr.DrawLine( points[2], points[3], dispColor );
		//		dr.DrawLine( points[3], points[0], dispColor );

		//		dr.DrawLine( points[4], points[5], dispColor );
		//		dr.DrawLine( points[5], points[6], dispColor );
		//		dr.DrawLine( points[6], points[7], dispColor );
		//		dr.DrawLine( points[7], points[4], dispColor );

		//		dr.DrawLine( points[0], points[4], dispColor );
		//		dr.DrawLine( points[1], points[5], dispColor );
		//		dr.DrawLine( points[2], points[6], dispColor );
		//		dr.DrawLine( points[3], points[7], dispColor );

		//	} 
		//	else 
		//	{
		//		dr.DrawSphere( position0, TubeRadius, dispColor );
		//		dr.DrawSphere( position1, TubeRadius, dispColor );
		//		dr.DrawLine( position0, position1, dispColor, dispColor, 3, 3 );
		//	}
		//}


		public override BoundingBox GetBoundingBox( IGameState gs )
		{
			var frustum = new BoundingFrustum( /*ViewMatrix **/ SpotProjection );
			var points  = frustum.GetCorners();
			return BoundingBox.FromPoints( points );
		}
	}
}
