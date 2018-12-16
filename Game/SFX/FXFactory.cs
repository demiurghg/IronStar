using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Utils;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using System.IO;
using IronStar.Core;
using BEPUphysics;
using BEPUphysics.Character;
using Fusion.Core.IniParser.Model;
using System.ComponentModel;
using System.Xml.Serialization;
using Fusion.Engine.Graphics;
using System.Drawing.Design;
using Fusion.Core.Content;
using Fusion.Development;
using Fusion.Core.Shell;

namespace IronStar.SFX {

	public partial class FXFactory : JsonObject, IPrecachable {

		[AECategory("General")]
		public float Period { get; set; } = 1;

		[AECategory( "Misc Stages" )]
		[AEExpandable]
		public FXSoundStage SoundStage { get; set; } = new FXSoundStage();

		[AECategory( "Misc Stages" )]
		[AEExpandable]
		public FXLightStage LightStage { get; set; } = new FXLightStage();

		[AECategory( "Misc Stages" )]
		[AEExpandable]
		public FXCameraShake CameraShake { get; set; } = new FXCameraShake();

		[AECategory( "Particle Stages" )]
		[AEExpandable]
		public FXParticleStage ParticleStage1 { get; set; } = new FXParticleStage();

		[AECategory( "Particle Stages" )]
		[AEExpandable]
		public FXParticleStage ParticleStage2 { get; set; } = new FXParticleStage();

		[AECategory( "Particle Stages" )]
		[AEExpandable]
		public FXParticleStage ParticleStage3 { get; set; } = new FXParticleStage();

		[AECategory( "Particle Stages" )]
		[AEExpandable]
		public FXParticleStage ParticleStage4 { get; set; } = new FXParticleStage();

		[AECategory( "Particle Stages" )]
		[AEExpandable]
		public FXParticleStage ParticleStage5 { get; set; } = new FXParticleStage();


		public FXInstance CreateFXInstance( FXPlayback fxPlayback, FXEvent fxEvent, bool looped )
		{
			return new FXInstance( fxPlayback, fxEvent, this, looped );
		}
	}



	[ContentLoader( typeof( FXFactory ) )]
	public sealed class FXFactoryLoader : ContentLoader {

		public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			return content.Game.GetService<JsonFactory>().ImportJson( stream );
		}
	}


	public enum FXDistribution {
		Uniform,
		Gauss,
	}


	public enum FXDistribution3D {
		Box,
		Sphere,
		Cylinder,
		Tube,
		Ring,
		Gauss,
	}


	//public enum FXVelocityDi


	public enum FXVelocityBias {
		None,
		LocalUp,
		//Centripital,
	}

	public enum FXDirection {
		None,
		LocalUp,
		LocalDown,
		LocalRight,
		LocalLeft,
		LocalForward,
		LocalBackward,
	}


	public class FXTiming {
		[XmlAttribute]
		[Description( "Relative delay of particle emission [0..1]" )]
		public float Delay { get; set; } = 0;
		
		[XmlAttribute]
		[Description( "Relative period of particle emission [0..1]" )]
		public float Bunch { get; set; } = 1;
		
		[XmlAttribute]
		[Description( "Relative fade-in period [0..1]" )]
		public float FadeIn { get; set; } = 0.1f;
		
		[XmlAttribute]
		[Description( "Relative fade-out period [0..1]" )]
		public float FadeOut { get; set; } = 0.1f;

		public override string ToString()
		{
			return string.Format("d:{0:0.##} b:{1:0.##} fade[{2:0.##}, {3:0.##}]", Delay, Bunch, FadeIn, FadeOut);
		}
	}


	public enum FXSoundAttenuation {
		Local,
		Normal,
		Loud,
		Distant,
	}


	public enum FXLightType {
		Omni,
		SpotShadow,
	}


	public enum FXLightStyle {
		Const,
		Saw,
		InverseSaw,
		Random,
		Strobe,
	}


	public class FXLifetime {
		[XmlAttribute]
		[Description( "Lifetime distribution" )]
		public FXDistribution Distribution { get; set; } = FXDistribution.Uniform;

		[XmlAttribute]
		[Description( "Minimum particle lifetime" )]
		public float MinLifetime { get; set; } = 1;

		[XmlAttribute]
		[Description( "Maximum particle lifetime" )]
		public float MaxLifetime { get; set; } = 1;

		public override string ToString()
		{
			return string.Format( "{0}: [{1:0.##}, {2:0.##}]", Distribution, MinLifetime, MaxLifetime );
		}

		public float GetLifetime ( Random rand )
		{
			var lifeTime = FXFactory.GetLinearDistribution( rand, Distribution, MinLifetime, MaxLifetime );
			return MathUtil.Clamp( lifeTime, 0.00390625f, 64f );
		}
	}


	public class FXAcceleration {
		[XmlAttribute]
		[Description( "Gravity factor [-1..1]. Zero value means no gravity. Negative values means buoyant particles" )]
		public float GravityFactor { get; set; } = 0;

		[XmlAttribute]
		[Description( "Velocity damping factor" )]
		public float Damping { get; set; } = 0;

		[XmlAttribute]
		[Description( "Constant acceleration opposite to initial velocity vector" )]
		public float DragForce { get; set; } = 0;

		[XmlAttribute]
		[Description( "Constant normally distributed acceleration" )]
		public float Turbulence { get; set; } = 0;

		public override string ToString()
		{
			return string.Format( "G:{0:0.##} D:{1:0.##} Drag:{2:0.##}, Turb:{3:0.##}", GravityFactor, Damping, DragForce, Turbulence );
		}
	}


	public class FXVelocity {

		[XmlAttribute]
		public FXDirection Direction { get; set; } = FXDirection.None;
		
		[XmlAttribute]
		public FXDistribution LinearDistribution { get; set; } = FXDistribution.Uniform;

		[Description( "Minimum linear velocity" )]
		public float LinearVelocityMin { get; set; } = 0;

		[Description( "Maximum linear velocity" )]
		public float LinearVelocityMax { get; set; } = 1;

		[Description( "Radial velocity distribution" )]
		public FXDistribution3D RadialDistribution { get; set; } = FXDistribution3D.Gauss;
		
		[Description( "Minimum radial velocity" )]
		public float RadialVelocity { get; set; } = 1;

		[Description( "Source velocity addition factor" )]
		public float Advection { get; set; } = 0;

		public Vector3 GetVelocity( FXEvent fxEvent, Random rand )
		{
			var rv				=	RadialVelocity;
			var velocityValue   =   FXFactory.GetLinearDistribution( rand, LinearDistribution, LinearVelocityMin, LinearVelocityMax );
			var velocity		=   FXFactory.GetDirection( Direction, velocityValue, fxEvent );
			var addition		=	FXFactory.GetVolumeDistribution( rand, RadialDistribution, rv,rv,rv,rv );
			var advection		=	fxEvent.Velocity * Advection;

			return velocity + addition + advection;
		}
	}


	public class FXPosition {
		[Description( "Offset direction" )]
		public FXDirection OffsetDirection { get; set; } = FXDirection.None;

		[Description( "Offset along offset direction" )]
		public float OffsetFactor { get; set; } = 0;

		[Description( "Average size of spawn area" )]
		public FXDistribution3D Distribution { get; set; } = FXDistribution3D.Box;

		[AEDisplayName("Width (X)")]
		public float Width { get; set; } = 0;

		[AEDisplayName("Height (Y)")]
		public float Height { get; set; } = 0;

		[AEDisplayName("Depth (Z)")]
		public float Depth { get; set; } = 0;

		[AEDisplayName("Radius (R)")]
		public float Radius { get; set; } = 0;

		public Vector3 GetPosition ( FXEvent fxEvent, Random rand, float scale )
		{
			var position = FXFactory.GetPosition( OffsetDirection, OffsetFactor * scale, fxEvent);
			var radial	= FXFactory.GetVolumeDistribution( rand, Distribution, Width, Height, Depth, Radius );
			return position + radial;
		}
	}


	public class FXShape {
		[XmlAttribute]
		public float Size0 { get; set; } = 1;

		[XmlAttribute]
		public float Size1 { get; set; } = 1;

		[XmlAttribute]
		public bool EnableRotation { get; set; } = false;

		[XmlAttribute]
		public float InitialAngle { get; set; } = 0;

		[XmlAttribute]
		public float MinAngularVelocity { get; set; } = 0;

		[XmlAttribute]
		public float MaxAngularVelocity { get; set; } = 0;

		public override string ToString()
		{
			return string.Format( "{0} {1} {2:0.##} [{3:0.##} {4:0.##}]", Size0/2+Size1/2, EnableRotation?"Enabled":"Disabled", InitialAngle, MinAngularVelocity, MaxAngularVelocity );
		}

		public void GetAngles ( Random rand, float lifetime, out float a, out float b )
		{
			a = b = 0;

			if (!EnableRotation) {
				return;
			}

			var sign = (rand.NextFloat(-1,1) > 0) ? 1 : -1;

			a = MathUtil.DegreesToRadians( rand.NextFloat( -InitialAngle, InitialAngle ) );

			b = a + MathUtil.DegreesToRadians( rand.NextFloat( MinAngularVelocity, MaxAngularVelocity ) * lifetime * sign );
		}
	}


	public class FXParticleStage {

		public override string ToString()
		{
			if (Enabled) {
				return string.Format("{0} [{1}] [{2}]", Effect, Count, Sprite); 
			} else {
				return string.Format( "Disabled" );
			}
		}

		[Description( "Enables and disables current particle stage" )]
		public bool Enabled { get; set; } = false;

		[Description( "Particle sprite name" )]
		[Editor( typeof( SpriteFileLocationEditor ), typeof( UITypeEditor ) )]
		[AEAtlasImage("sprites\\particles")]
		public string Sprite { get; set; } = "";

		[Description( "Particle sprite name" )]
		public bool UseRandomImages { get; set; } = false;

		[Description( "Particle visual effect" )]
		public ParticleFX Effect { get; set; } = ParticleFX.Hard;

		[Description( "Total number of emitted particles per active period" )]
		public int Count { get; set; } = 10;

		[Description( "Particle stage active period" )]
		public float Period { get; set; } = 1;

		[Description( "Defines temporal properties of particle stage" )]
		[AEExpandable]
		public FXTiming Timing { get; set; } = new FXTiming();

		[Description( "Particle base color" )]
		public Color Color { get; set; } = Color.White;

		[Description( "Particle alpha factor or alpha-kill threshold" )]
		public float Alpha { get; set; } = 1;

		[Description( "Roughness for hard particles" )]
		public float Roughness { get; set; } = 0.5f;

		[Description( "Metallic for hard particles" )]
		public float Metallic { get; set; } = 0.0f;

		[Description( "Particle emission intensity" )]
		public float Intensity { get; set; } = 100;

		[Description( "Particle approximate subsurface scattering" )]
		public float Scattering { get; set; } = 0;

		[Description( "Particle extending along velocity vector" )]
		public float BeamFactor { get; set; } = 0;

		[Description( "Defines life-time properties of particle stage" )]
		[AEExpandable]
		public FXLifetime Lifetime { get; set; } = new FXLifetime();

		[Description( "Defines shape of particles" )]
		[AEExpandable]
		public FXShape Shape { get; set; } = new FXShape();

		[Description( "Defines particle spawn area" )]
		[AEExpandable]
		public FXPosition Position { get; set; } = new FXPosition();

		[Description( "Defines particle velocity" )]
		[AEExpandable]
		public FXVelocity Velocity { get; set; } = new FXVelocity();

		[Description( "Defines particle damping and acceleration" )]
		[AEExpandable]
		public FXAcceleration Acceleration { get; set; } = new FXAcceleration();
	}



	public class FXSoundStage {

		public override string ToString()
		{
			if ( Enabled ) {
				return string.Format( "{0}", Sound );
			} else {
				return string.Format( "Disabled" );
			}
		}

		[Description( "Enables and disables sound stage" )]
		public bool Enabled { get; set; } = false;

		[Editor( typeof( SoundFileLocationEditor ), typeof( UITypeEditor ) )]
		public string Sound { get; set; } = "";

		[AEValueRange(0,1, 0.125f, 0.125f/16)]
		public float Reverb { get; set; } = 1;
	}



	public class FXLightStage {

		public override string ToString()
		{
			if ( Enabled ) {
				return string.Format( "R:{0} I:[{1}{2}{3}]", OuterRadius, Color.R, Color.G, Color.B );
			} else {
				return string.Format( "Disabled" );
			}
		}

		[XmlAttribute]
		[Description( "Enables and disables light stage" )]
		public bool Enabled { get; set; } = false;

		[XmlAttribute]
		[Description( "Enables and disables light stage" )]
		public float Period { get; set; } = 1;

		[Description( "Light intensity" )]
		public float Intensity { get; set; } = 100;

		[Description( "Light intensity" )]
		public Color Color { get; set; } = Color.White;

		[XmlAttribute]
		[Description( "Light radius" )]
		public float InnerRadius { get; set; } = 0;

		[XmlAttribute]
		[Description( "Light radius" )]
		public float OuterRadius { get; set; } = 5;

		[XmlAttribute]
		[Description( "Pulse string: 'a' - means zero intensity, 'z' - means double intensity" )]
		public string PulseString { get; set; } = "m";

		[XmlAttribute]
		[Description( "Light style" )]
		public FXLightStyle LightStyle { get; set; } = FXLightStyle.Const;

		[XmlAttribute]
		[Description( "Offset direction" )]
		public FXDirection OffsetDirection { get; set; } = FXDirection.None;

		[XmlAttribute]
		[Description( "Offset along offset direction" )]
		public float OffsetFactor { get; set; } = 0;
	}



	public class FXDecal {

		[XmlAttribute]
		[Description( "Size of the decal" )]
		public float Size { get; set; } = 0.5f;

		[XmlAttribute]
		[Description( "Depth of the decal" )]
		public float Depth { get; set; } = 0.25f;

		[XmlAttribute]
		[Description( "Decal lifetime" )]
		public float LifetimeNormal { get; set; } = 2.0f;

		[XmlAttribute]
		[Description( "Decal lifetime" )]
		public float LifetimeGlow { get; set; } = 2.0f;

		[Description( "Color decal texture name" )]
		[Editor( typeof( SpriteFileLocationEditor ), typeof( UITypeEditor ) )]
		public string ColorDecal { get; set; } = "";

		[Description( "Normal map decal texture name" )]
		[Editor( typeof( SpriteFileLocationEditor ), typeof( UITypeEditor ) )]
		public string NormalDecal { get; set; } = "";

		[Description( "Roughness decal texture name" )]
		[Editor( typeof( SpriteFileLocationEditor ), typeof( UITypeEditor ) )]
		public string RoughnessDecal { get; set; } = "";

		[Description( "Metallic decal texture name" )]
		[Editor( typeof( SpriteFileLocationEditor ), typeof( UITypeEditor ) )]
		public string MetalDecal { get; set; } = "";

		[Description( "Emission decal texture name" )]
		[Editor( typeof( SpriteFileLocationEditor ), typeof( UITypeEditor ) )]
		public string EmissionDecal { get; set; } = "";

		public override string ToString()
		{
			if ( Enabled ) {
				return string.Format( "Enabled" );
			} else {
				return string.Format( "Disabled" );
			}
		}

		[XmlAttribute]
		[Description( "Enables and disables decals" )]
		public bool Enabled { get; set; } = false;
	}



	public class FXCameraShake {

		public override string ToString()
		{
			if ( Enabled ) {
				return string.Format( "Enabled" );
			} else {
				return string.Format( "Disabled" );
			}
		}

		[XmlAttribute]
		[Description( "Enables and disables camera shake stage" )]
		public bool Enabled { get; set; } = false;

	}
}
