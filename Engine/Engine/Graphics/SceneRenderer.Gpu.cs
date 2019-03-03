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
			FORWARD				=	1 << 0,
			SHADOW				=	1 << 1,
			RIGID				=	1 << 4,
			SKINNED				=	1 << 5,
			ZPASS				=	1 << 6,
			ANISOTROPIC			=	1 << 7,
			GBUFFER				=	1 << 8,
			TRANSPARENT			=	1 << 9,
			IRRADIANCE_MAP		=	1 << 10,
			IRRADIANCE_VOLUME	=	1 << 11,
		}


		static float log2( float x ) {
			return (float)Math.Log( x, 2 );
		}


		[ShaderDefine]	public const int VTVirtualPageCount	=	VTConfig.VirtualPageCount;
		[ShaderDefine]	public const int VTPageSize			=	VTConfig.PageSize;
		[ShaderDefine]	public const int VTMaxMip			=	VTConfig.MaxMipLevel;
		[ShaderDefine]	public const int VTMipSelectorScale	=	(VTConfig.PageSize >> VTMaxMip) * VTConfig.VirtualPageCount;

		[ShaderDefine]	public const uint LightTypeOmni			=	1;
		[ShaderDefine]	public const uint LightTypeOmniShadow	=	2;
		[ShaderDefine]	public const uint LightTypeSpotShadow	=	3;
		[ShaderDefine]	public const uint LightTypeAmbient		=	4;
		[ShaderDefine]	public const uint LightSpotShapeSquare	=	0x00010000;
		[ShaderDefine]	public const uint LightSpotShapeRound	=	0x00020000;

		[ShaderDefine]	public const uint LightProbeSize			=	RenderSystem.LightProbeSize;
		[ShaderDefine]	public const uint LightProbeMaxSpecularMip	=	RenderSystem.LightProbeMaxSpecularMip;
		[ShaderDefine]	public const uint LightProbeDiffuseMip		=	RenderSystem.LightProbeDiffuseMip;

		[ShaderDefine]	public const uint InstanceGroupStatic		=	(int)InstanceGroup.Static;
		[ShaderDefine]	public const uint InstanceGroupDynamic		=	(int)InstanceGroup.Dynamic;
		[ShaderDefine]	public const uint InstanceGroupCharacter	=	(int)InstanceGroup.Character;
		[ShaderDefine]	public const uint InstanceGroupWeapon		=	(int)InstanceGroup.Weapon;


		[ShaderStructure]
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=1024)]
		struct STAGE {
			public Matrix	Projection				;
			public Matrix	ProjectionFPV			;
			public Matrix	View					;
			public Matrix	CascadeViewProjection0	;
			public Matrix	CascadeViewProjection1	;
			public Matrix	CascadeViewProjection2	;
			public Matrix	CascadeViewProjection3	;
			public Matrix	CascadeGradientMatrix0	;
			public Matrix	CascadeGradientMatrix1	;
			public Matrix	CascadeGradientMatrix2	;
			public Matrix	CascadeGradientMatrix3	;
			public Matrix	OcclusionGridMatrix		;
			public Vector4	CascadeScaleOffset0		;
			public Vector4	CascadeScaleOffset1		;
			public Vector4	CascadeScaleOffset2		;
			public Vector4	CascadeScaleOffset3		;
			public Vector4	ViewPos					;
			public Vector4	BiasSlopeFar			;
			public Color4	SkyAmbientLevel			;
			public Vector4	ViewBounds				;
			public Vector4	DirectLightDirection	;
			public Color4	DirectLightIntensity	;
			public Color4	FogColor				;
			public float	FogAttenuation			;	
			public float	DirectLightAngularSize	;
			public float	VTPageScaleRCP			;
			public float	VTGradientScaler		;
			public float	SsaoWeight				;
		}


		[ShaderStructure]
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=128)]
		struct INSTANCE {
			public Matrix	World	;
			public Color4	Color	;
			public Vector4	LMRegion;
			public int		Group	;
		}


		[ShaderStructure]
		struct SUBSET {
			public Vector4	Rectangle;
			public Color4	Color;
			public float	MaxMip;
			public float	Dummy1;
			public float	Dummy2;
			public float	Dummy3;
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
			public Vector4	Position;
			public uint		ImageIndex;
			public float	NormalizedWidth	;
			public float	NormalizedHeight;
			public float	NormalizedDepth	;

			public void FromLightProbe ( LightProbe light ) 
			{
				#region Update structure fields from LightProbe object
				MatrixInv			=	Matrix.Invert(light.ProbeMatrix);
				Position			=	new Vector4( light.ProbeMatrix.TranslationVector, 1 );
				ImageIndex			=	(uint)light.ImageIndex;
				NormalizedWidth		=	light.NormalizedWidth	;
				NormalizedHeight	=	light.NormalizedHeight	;
				NormalizedDepth		=	light.NormalizedDepth	;
				#endregion
			}
		}


		[ShaderStructure]
		[StructLayout(LayoutKind.Sequential)]
		public struct LIGHT {	
			//public Matrix	WorldMatrix;
			public Matrix	ViewProjection;
			public Vector4	PositionRadius;
			public Vector4	IntensityFar;
			public Vector4	ShadowScaleOffset;
			public uint		LightType;
			public float	SourceRadius;

			public void FromOmniLight ( OmniLight light ) 
			{
				#region Update structure fields from OmniLight object
				LightType		=	light.Ambient ? LightTypeAmbient : LightTypeOmni;
				PositionRadius	=	new Vector4( light.Position, light.RadiusOuter );
				IntensityFar	=	new Vector4( light.Intensity2.Red, light.Intensity2.Green, light.Intensity2.Blue, 0 );
				SourceRadius	=	light.RadiusInner;
				#endregion
			}

			public void FromSpotLight ( SpotLight light ) 
			{
				#region Update structure fields from SpotLight object

				LightType			=	LightTypeSpotShadow;
				PositionRadius		=	new Vector4( light.Position, light.RadiusOuter );
				IntensityFar		=	new Vector4( light.Intensity2.Red, light.Intensity2.Green, light.Intensity2.Blue, light.Projection.GetFarPlaneDistance() );
				ViewProjection		=	light.SpotView * light.Projection;
				ShadowScaleOffset	=	light.ShadowScaleOffset;
				SourceRadius		=	light.RadiusInner;
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
				AssignmentGroup		=	(int)decal.Group;

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
