﻿using System;
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
		[AEDisplayName("Tube Radius")]
		[AEValueRange(0, 50, 1, 0.125f)]
		public float TubeRadius { get; set; } = 0.125f;
		
		[AECategory("Spot Shape")]
		[AEDisplayName("Tube Length")]
		[AEValueRange(0, 50, 1, 0.125f)]
		public float TubeLength { get; set; } = 0.0f;
		
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



		[AECategory("Global Illumination")]
		[AEDisplayName("Enable GI")]
		public bool EnableGI { get; set; } = false;

		[AECategory("Spot Shadow")]
		[AEDisplayName("Spot Mask")]
		[AEAtlasImage("spots/spots")]
		public string SpotMaskName { get; set; } = "";
		
		[AECategory("Spot Shadow")]
		[AEDisplayName("Shadow LOD Bias")]
		[AEValueRange(0, 8, 1, 1)]
		public int LodBias { get; set; } = 0;
		
		[AECategory("Spot Shadow")]
		[AEDisplayName("Shadow Depth Bias")]
		[AEValueRange(0, 1/512f, 1/8192f, 1/16384f)]
		public float DepthBias { get; set; } = 1f / 1024f;

		[AECategory("Spot Shadow")]
		[AEDisplayName("Shadow Slope Bias")]
		[AEValueRange(0, 8, 1, 0.125f/4.0f)]
		public float SlopeBias { get; set; } = 2;


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



		public override void SpawnNodeECS( GameState gs )
		{
			ecsEntity = gs.Spawn();

			ecsEntity.AddComponent( new Transform( TranslateVector, RotateQuaternion, 1 ) );
			ecsEntity.AddComponent( CreateSpotLight() );
		}


		SFX2.SpotLight CreateSpotLight()
		{
			var light = new SFX2.SpotLight();

			light.TubeLength		=	TubeLength;
			light.TubeRadius		=	TubeRadius;
			light.OuterRadius		=	OuterRadius;
			light.LightColor		=	LightColor;
			light.LightIntensity	=	MathUtil.Log2( MathUtil.Clamp( LightIntensity, 1/64.0f, 1024 ) );

			light.EnableGI			=	EnableGI;
			light.SlopeBias			=	SlopeBias;
			light.DepthBias			=	DepthBias;
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

		//		var frustum = new BoundingFrustum( SpotView * SpotProjection );
				
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


		public override MapNode DuplicateNode()
		{
			var newNode = (MapSpotLight)MemberwiseClone();
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
