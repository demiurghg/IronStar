using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Drivers.Graphics;
using System.Runtime.InteropServices;
using Fusion.Development;
using Fusion.Engine.Graphics.Ubershaders;

namespace Fusion.Engine.Graphics {

	[RequireShader("surface", true)]
	internal partial class SceneRenderer : RenderComponent {

		enum SurfaceFlags {
			FORWARD	=	1 << 0,
			SHADOW	=	1 << 1,
			RIGID	=	1 << 4,
			SKINNED	=	1 << 5,
			ZPASS	=	1 << 6,
		}


		static float log2( float x ) {
			return (float)Math.Log( x, 2 );
		}


		[ShaderDefine]	public const int VTVirtualPageCount	=	VTConfig.VirtualPageCount;
		[ShaderDefine]	public const int VTPageSize			=	VTConfig.PageSize;
		[ShaderDefine]	public const int VTMaxMip				=	VTConfig.MaxMipLevel;

		[ShaderDefine]	public const int LightTypeOmni			=	1;
		[ShaderDefine]	public const int LightTypeOmniShadow	=	2;
		[ShaderDefine]	public const int LightTypeSpotShadow	=	3;


		[ShaderStructure]
		[StructLayout(LayoutKind.Explicit, Size=320)]
		struct BATCH {
			[FieldOffset(  0)] public Matrix	Projection;
			[FieldOffset( 64)] public Matrix	View;
			[FieldOffset(128)] public Matrix	World;
			[FieldOffset(192)] public Vector4	ViewPos			;
			[FieldOffset(208)] public Vector4	BiasSlopeFar	;
			[FieldOffset(224)] public Color4	Color;
			[FieldOffset(240)] public Vector4	ViewBounds;
			[FieldOffset(256)] public float		VTPageScaleRCP;
			[FieldOffset(260)] public int		AssignmentGroup;
		}


		[ShaderStructure]
		struct SUBSET {
			public Vector4 Rectangle;
		}


		[ShaderStructure]
		[StructLayout(LayoutKind.Explicit, Size=656)]
		struct LIGHTDATA {
			[FieldOffset(  0)] public Matrix	View;
			[FieldOffset( 64)] public Matrix	Projection;
			//[FieldOffset(128)] public Matrix	InverseViewProjection;
			//[FieldOffset(192)] public Vector4	FrustumVectorTR;
			//[FieldOffset(208)] public Vector4	FrustumVectorBR;
			//[FieldOffset(224)] public Vector4	FrustumVectorBL;
			//[FieldOffset(240)] public Vector4	FrustumVectorTL;
			//[FieldOffset(256)] public Matrix	CSMViewProjection0;
			//[FieldOffset(320)] public Matrix	CSMViewProjection1;
			//[FieldOffset(384)] public Matrix	CSMViewProjection2;
			//[FieldOffset(448)] public Matrix	CSMViewProjection3;
			[FieldOffset(512)] public Vector4	ViewPosition;
			//[FieldOffset(528)] public Vector4	DirectLightDirection;
			//[FieldOffset(544)] public Vector4	DirectLightIntensity;
			//[FieldOffset(560)] public Vector4	ViewportSize;
			//[FieldOffset(576)] public Vector4	CSMFilterRadius;
			//[FieldOffset(592)] public Color4	AmbientColor;
			//[FieldOffset(608)] public Vector4	Viewport;
			//[FieldOffset(624)] public float		ShowCSLoadOmni;
			//[FieldOffset(628)] public float		ShowCSLoadEnv;
			//[FieldOffset(632)] public float		ShowCSLoadSpot;
			//[FieldOffset(636)] public int		CascadeCount;
			//[FieldOffset(640)] public float		CascadeScale;
			//[FieldOffset(644)] public float		FogDensity;
		}


		[ShaderStructure]
		[StructLayout(LayoutKind.Explicit)]
		public struct LIGHTINDEX {
			[FieldOffset( 0)]	public uint		Offset;		///	Light index buffer offset
			[FieldOffset( 4)]	public uint		Count;		/// [Spot count][Omni count]
															
			public void AddDecal () {
				Count += (1<<12);
			}
			public void AddLight () {
				Count += (1<<0);
			}

			public ushort DecalCount { get { return (ushort)( (Count & 0xFFF000) >> 12 ); } }
			public ushort LightCount { get { return (ushort)( (Count & 0x000FFF) >> 0  ); } }

			public ushort TotalCount { get { return (ushort)(DecalCount + LightCount); } }
		}



		[ShaderStructure]
		[StructLayout(LayoutKind.Sequential)]
		public struct LIGHT {	
			//public Matrix	WorldMatrix;
			public Matrix	ViewProjection;
			public Vector4	PositionRadius;
			public Vector4	IntensityFar;
			public Vector4	ShadowScaleOffset;
			public int		LightType;

			public void FromOmniLight ( OmniLight light ) 
			{
				#region Update structure fields from OmniLight object
				LightType		=	SceneRenderer.LightTypeOmni;
				PositionRadius	=	new Vector4( light.Position, light.RadiusOuter );
				IntensityFar	=	new Vector4( light.Intensity.Red, light.Intensity.Green, light.Intensity.Blue, 0 );
				#endregion
			}

			public void FromSpotLight ( SpotLight light ) 
			{
				#region Update structure fields from SpotLight object
				LightType			=	SceneRenderer.LightTypeSpotShadow;
				PositionRadius		=	new Vector4( light.Position, light.RadiusOuter );
				IntensityFar		=	new Vector4( light.Intensity.Red, light.Intensity.Green, light.Intensity.Blue, light.Projection.GetFarPlaneDistance() );
				ViewProjection		=	light.SpotView * light.Projection;
				ShadowScaleOffset	=	light.ShadowScaleOffset;
				#endregion
			}
		}



		[ShaderStructure]
		[StructLayout(LayoutKind.Sequential)]
		#warning Reduce DECAL structure size to 128 byte
		public struct DECAL {
			public Matrix	DecalMatrixInv;
			public Vector4 	BasisX;
			public Vector4 	BasisY;
			public Vector4 	BasisZ;
			public Vector4 	EmissionRoughness;
			public Vector4	ImageScaleOffset;
			public Vector4	BaseColorMetallic;
			public float	ColorFactor;
			public float	SpecularFactor;
			public float	NormalMapFactor;
			public float	FalloffFactor;
			public int		AssignmentGroup;
			public float	MipBias;

			public void FromDecal ( Decal decal, float projM22, ref Rectangle screenSize )
			{
				#region Update structure fields from Decal object
				DecalMatrixInv		=	decal.DecalMatrixInverse;
				BasisX				=	new Vector4(decal.DecalMatrix.Right.Normalized(),	0);
				BasisY				=	new Vector4(decal.DecalMatrix.Up.Normalized(),		0);
				BasisZ				=	new Vector4(decal.DecalMatrix.Backward.Normalized(),0);
				BaseColorMetallic	=	new Vector4( decal.BaseColor.Red, decal.BaseColor.Green, decal.BaseColor.Blue, decal.Metallic );
				EmissionRoughness	=	new Vector4( decal.Emission.Red, decal.Emission.Green, decal.Emission.Blue, decal.Roughness );
				ColorFactor			=	decal.ColorFactor;
				SpecularFactor		=	decal.SpecularFactor;
				NormalMapFactor		=	decal.NormalMapFactor;
				FalloffFactor		=	decal.FalloffFactor;
				ImageScaleOffset	=	decal.GetScaleOffset();
				AssignmentGroup		=	0;

				var widthScaling	=	decal.DecalMatrix.Right.Length() + float.Epsilon;
				var heightScaling	=	decal.DecalMatrix.Up.Length() + float.Epsilon;

				var minRelativeSize	=	Math.Min( decal.ImageSize.Width / widthScaling, decal.ImageSize.Height / heightScaling );
				var projScaling		=	1 / projM22;

				MipBias				=	log2( minRelativeSize / screenSize.Height * projScaling );

				BaseColorMetallic.X	=	(float)Math.Pow( BaseColorMetallic.X, 2.2f );
				BaseColorMetallic.Y	=	(float)Math.Pow( BaseColorMetallic.Y, 2.2f );
				BaseColorMetallic.Z	=	(float)Math.Pow( BaseColorMetallic.Z, 2.2f );
				#endregion
			}
		}
	}
}
