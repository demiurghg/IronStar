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
	[ShaderSharedStructure(typeof(GpuData.CAMERA))]
	[ShaderSharedStructure(typeof(ShadowSystem.CASCADE_SHADOW))]
	[ShaderSharedStructure(typeof(GpuData.DIRECT_LIGHT))]
	internal partial class SceneRenderer : RenderComponent {

		enum SurfaceFlags {
			FORWARD				=	1 << 0,
			SHADOW				=	1 << 1,
			RIGID				=	1 << 4,
			SKINNED				=	1 << 5,
			ZPASS				=	1 << 6,
			ANISOTROPIC			=	1 << 7,
			GBUFFER				=	1 << 8,
			RADIANCE			=	1 << 9,
			TRANSPARENT			=	1 << 10,
			IRRADIANCE_MAP		=	1 << 11,
			IRRADIANCE_VOLUME	=	1 << 12,
			CSM					=	1 << 13,
			SPOT				=	1 << 14,
		}


		static float log2( float x ) {
			return (float)Math.Log( x, 2 );
		}



		[ShaderStructure]
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=128)]
		public struct STAGE 
		{
			public Matrix	WorldToLightVolume		;
			
			public Vector4	ViewportSize			;
			
			public float	VTPageScaleRCP			;
			public float	VTGradientScaler		;
			public float	VTInvertedPhysicalSize	;
			public float	SsaoWeight				;
			public float	ShowLightComplexity		;

			public float	SlopeBias				;
			public float	DepthBias				;

			public float	DirectLightFactor		;
			public float	IndirectLightFactor		;

		}


		[ShaderStructure]
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=96)]
		public struct INSTANCE 
		{
			public INSTANCE( RenderInstance instance )
			{
				World		=	instance.World;
				LMRegion	=	instance.LightMapScaleOffset;
				Color		=	new Color3( instance.Color.Red, instance.Color.Green, instance.Color.Blue );
				Group		=	(int)instance.Group;
			}

			public Matrix	World	;
			public Vector4	LMRegion;
			public Color3	Color	;
			public int		Group	;
		}


		[ShaderStructure]
		public struct SUBSET {
			public Vector4	Rectangle;
			public Color3	Color;
			public float	MaxMip;
		}


		[ShaderStructure]
		[StructLayout(LayoutKind.Explicit)]
		public struct LIGHTINDEX {
			[FieldOffset( 0)]	public uint		Offset;		///	Light index buffer offset
			[FieldOffset( 4)]	public uint		Count;		/// [Spot count][Omni count]
															
			public void AddLightProbe () {
				Count += (1<<24);
			}
			public void AddDecal () {
				Count += (1<<12);
			}
			public void AddLight () {
				Count += (1<<0);
			}

			public ushort ProbeCount { get { return (ushort)( (Count & 0xFF000000) >> 24 ); } }
			public ushort DecalCount { get { return (ushort)( (Count & 0x00FFF000) >> 12 ); } }
			public ushort LightCount { get { return (ushort)( (Count & 0x00000FFF) >> 0  ); } }

			public ushort TotalCount { get { return (ushort)(DecalCount + LightCount + ProbeCount); } }
		}



		[ShaderStructure]
		[StructLayout(LayoutKind.Sequential)]
		public struct LIGHTPROBE {	
			public Matrix	MatrixInv;
			public Vector4	Position;	  //	negative W means using spherical reflection
			public uint		ImageIndex;
			public float	NormalizedWidth	;
			public float	NormalizedHeight;
			public float	NormalizedDepth	;

			public void FromLightProbe ( LightProbe light ) 
			{
				#region Update structure fields from LightProbe object
				MatrixInv			=	Matrix.Invert(light.ProbeMatrix);
				Position			=	new Vector4( light.ProbeMatrix.TranslationVector, light.Mode==LightProbeMode.CubeReflection ? 1 : -1 );
				ImageIndex			=	(uint)light.ImageIndex;
				NormalizedWidth		=	light.NormalizedWidth	;
				NormalizedHeight	=	light.NormalizedHeight	;
				NormalizedDepth		=	light.NormalizedDepth	;
				#endregion
			}
		}


		[ShaderStructure]
		[StructLayout(LayoutKind.Sequential)]
		#warning Reduce DECAL structure size to 128 byte
		public struct LIGHT 
		{	
			//public Matrix	WorldMatrix;
			public Matrix	ViewProjection;
			public Vector4	Position0LightRange;
			public Vector4	Position1TubeRadius;
			public Vector4	IntensityFar;
			public Vector4	ShadowScaleOffset;
			public uint		LightType;

			public void ClearLight()
			{
				LightType			=	RenderSystem.LightTypeNone;
				Position0LightRange	=	new Vector4( 0,0,0,0 );
				Position1TubeRadius	=	new Vector4( 0,0,0,0 );
				ViewProjection		=	Matrix.Identity;
				ShadowScaleOffset	=	Vector4.Zero;
			}

			public void FromOmniLight ( OmniLight light ) 
			{
				#region Update structure fields from OmniLight object
				LightType			=	light.Ambient ? RenderSystem.LightTypeAmbient : RenderSystem.LightTypeOmni;
				Position0LightRange	=	new Vector4( light.Position0, light.RadiusOuter );
				Position1TubeRadius	=	new Vector4( light.Position1, light.RadiusInner );
				IntensityFar		=	new Vector4( light.Intensity.Red, light.Intensity.Green, light.Intensity.Blue, 0 );
				#endregion
			}

			public void FromSpotLight ( SpotLight light ) 
			{
				#region Update structure fields from SpotLight object

				LightType			=	RenderSystem.LightTypeSpotShadow;
				Position0LightRange	=	new Vector4( light.Position0, light.RadiusOuter );
				Position1TubeRadius	=	new Vector4( light.Position1, light.RadiusInner );
				IntensityFar		=	new Vector4( light.Intensity.Red, light.Intensity.Green, light.Intensity.Blue, light.ProjectionMatrix.GetFarPlaneDistance() );
				ViewProjection		=	light.ViewMatrix * light.ProjectionMatrix;
				ShadowScaleOffset	=	light.RegionScaleOffset;
				#endregion
			}
		}



		[ShaderStructure]
		[StructLayout(LayoutKind.Sequential)]
		#warning Reduce DECAL structure size to 128 byte
		//	Size = 184
		//	Opt size 12
		//	Z-ordering
		public struct DECAL {
			//	float3 + sign
			public Matrix	DecalMatrixInv;
			//	uint + uint + uint
			public Vector4 	BasisX;
			public Vector4 	BasisY;
			public Vector4 	BasisZ;

			//	uint
			public Vector4 	EmissionRoughness;
			//	uint
			public Vector4	ImageScaleOffset;
			//	uint
			public Vector4	BaseColorMetallic;
			//	uint
			public float	ColorFactor;
			public float	SpecularFactor;
			public float	NormalMapFactor;
			public float	FalloffFactor;
			//	uint + glow scale 16 bit
			public uint		AssignmentGroup;
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
				FalloffFactor		=	MathUtil.Clamp( decal.FalloffFactor, 0, 0.99f );
				ImageScaleOffset	=	decal.GetScaleOffset();
				AssignmentGroup		=	(uint)decal.Group;

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
