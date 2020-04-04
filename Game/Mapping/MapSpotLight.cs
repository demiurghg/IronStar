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
using Fusion;
using Fusion.Core.Shell;

namespace IronStar.Mapping {

	public class MapSpotLight : MapNode {


		[AECategory("Light Color")]
		[AEDisplayName("Light Color")]
		public Color LightColor { get; set; } = Color.White;

		[AECategory("Light Color")]
		[AEDisplayName("Intensity")]
		[AEValueRange(0, 5000, 10, 1)]
		public float LightIntensity { get; set; } = 100;
		


		[AECategory("Spot Shape")]
		[AEDisplayName("Outer Radius")]
		[AEValueRange(0, 100, 1, 0.125f)]
		public float OuterRadius { get; set; } = 5;
		
		[AECategory("Spot Shape")]
		[AEDisplayName("Inner Radius")]
		[AEValueRange(0, 50, 1, 0.125f)]
		public float InnerRadius { get; set; } = 0.1f;
		
		[AECategory("Spot Shape")]
		[AEValueRange(0, 4, 1/4f, 1/64f)]
		public float NearPlane { get; set; } = 0.125f;
		
		[AECategory("Spot Shape")]
		[AEValueRange(0, 100, 1, 1/8f)]
		public float FarPlane { get; set; } = 5;
		
		[AECategory("Spot Shape")]
		[AEValueRange(0, 150, 15, 1)]
		public float FovVertical { get; set; } = 60;
		
		[AECategory("Spot Shape")]
		[AEValueRange(0, 150, 15, 1)]
		public float FovHorizontal { get; set; } = 60;

		[AECategory("Spot-light")]
		public LightStyle LightStyle { get; set; } = LightStyle.Default;



		[AECategory("Spot Shadow")]
		[AEDisplayName("Spot Mask")]
		[AEAtlasImage("spots/spots")]
		public string SpotMaskName { get; set; } = "";
		
		[AECategory("Spot Shadow")]
		[AEDisplayName("Shadow LOD Bias")]
		public int LodBias { get; set; } = 0;
		
		[AECategory("Spot Shadow")]
		[AEDisplayName("Shadow Depth Bias")]
		[AEValueRange(0, 1/512f, 1/8192f, 1/16384f)]
		public float DepthBias { get; set; } = 1f / 1024f;

		[AECategory("Spot Shadow")]
		[AEDisplayName("Shadow Slope Bias")]
		[AEValueRange(0, 8, 1, 0.125f/4.0f)]
		public float SlopeBias { get; set; } = 2;


		SpotLight	light;


		Matrix SpotView {
			get {
				return	Matrix.Invert(WorldMatrix);
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



		public override void SpawnNode( GameWorld world )
		{
			var lightSet	=	world.Game.RenderSystem.RenderWorld.LightSet;

			light		=	new SpotLight();

			light.Intensity		=	LightColor.ToColor4() * LightIntensity;
			light.SpotView		=	SpotView;
			light.Position		=	TranslateVector;
			light.Projection	=	SpotProjection;
			light.RadiusOuter	=	OuterRadius;
			light.RadiusInner	=	InnerRadius;

			light.SpotMaskName	=	SpotMaskName;

			light.LodBias		=	LodBias;

			light.DepthBias		=	DepthBias;
			light.SlopeBias		=	SlopeBias;

			light.LightStyle	=	LightStyle;

			world.Game.RenderSystem.RenderWorld.LightSet.SpotLights.Add( light );
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
			var dispColor   =	LightColor;

			dr.DrawPoint( transform.TranslationVector, 1, color, 2 );
			dr.DrawSphere( transform.TranslationVector, InnerRadius, dispColor );

			if (selected) {
				
				var frustum = new BoundingFrustum( SpotView * SpotProjection );
				
				var points  = frustum.GetCorners();

				dr.DrawLine( points[0], points[1], color );
				dr.DrawLine( points[1], points[2], color );
				dr.DrawLine( points[2], points[3], color );
				dr.DrawLine( points[3], points[0], color );

				dr.DrawLine( points[4], points[5], color );
				dr.DrawLine( points[5], points[6], color );
				dr.DrawLine( points[6], points[7], color );
				dr.DrawLine( points[7], points[4], color );

				dr.DrawLine( points[0], points[4], color );
				dr.DrawLine( points[1], points[5], color );
				dr.DrawLine( points[2], points[6], color );
				dr.DrawLine( points[3], points[7], color );

			} else {
			}
		}



		public override void KillNode( GameWorld world )
		{
			world.Game.RenderSystem.RenderWorld.LightSet.SpotLights.Remove( light );
		}


		public override MapNode DuplicateNode( GameWorld world )
		{
			var newNode = (MapSpotLight)MemberwiseClone();
			newNode.light = null;
			newNode.NodeGuid = Guid.NewGuid();
			return newNode;
		}


		public override BoundingBox GetBoundingBox()
		{
			var frustum = new BoundingFrustum( SpotView * SpotProjection );
			var points  = frustum.GetCorners();
			return BoundingBox.FromPoints( points );
		}
	}
}
