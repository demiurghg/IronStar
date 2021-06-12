using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Fusion.Engine.Graphics {

	/// <summary>
	/// Defines particle behavior.
	/// </summary>
	[Flags]
	public enum ParticleFX : byte 
	{
		/// <summary>
		/// Hard (alpha-kill) emissive particle 
		/// like electric sparks or smoldering ash. 
		/// </summary>
		Hard,

		/// <summary>
		/// Hard (alpha kill) lit by lights particle 
		/// like blood drops and debris
		/// </summary>
		HardLit,

		/// <summary>
		/// Hard (alpha kill), casting shadow, lit by lights particle 
		/// like blood drops and debris
		/// </summary>
		HardLitShadow,

		/// <summary>
		/// Soft (alpha blend) emissive particles
		/// like fire or plasma balls
		/// </summary>
		Soft,

		/// <summary>
		/// Soft (alpha blend) light-mapped particles
		/// like smoke, steam or dust clouds
		/// </summary>
		SoftLit,	

		/// <summary>
		/// Soft (alpha blend), casting shadow, light-mapped particles
		/// like smoke, steam or dust clouds
		/// </summary>
		SoftLitShadow,	

		/// <summary>
		/// Distortive particles
		/// </summary>
		Distortive,
	}



	public enum ParticleStreamType {
		Hard, Soft, Distortive,
	}



	/// <summary>
	/// Particle structure
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct Particle {
		
		/// <summary>
		/// Initial position of the particle
		/// </summary>
		public Vector3	Position;              

		/// <summary>
		/// Initial velocity of the particle
		/// </summary>
		public Vector3	Velocity;           

		/// <summary>
		/// Color of particle, tint for glowing particles.
		/// </summary>
		public Color	Color { set { ColorPacked = (uint)value.ToBgra(); }	}
		public uint		ColorPacked;

		public float	Exposure { set { BitUtils.UIntSetUNorm8( ref MaterialERMS, value, 0 ); } }

		public float	Roughness { set { BitUtils.UIntSetUNorm8( ref MaterialERMS, value, 1 ); } }

		public float	Metallic { set { BitUtils.UIntSetUNorm8( ref MaterialERMS, value, 2 ); } }

		public float	Scattering { set { BitUtils.UIntSetUNorm8( ref MaterialERMS, value, 3 ); } }

		public uint		MaterialERMS;

		public float	Intensity { set { IntensityBeamFactor.X = value; } }
		public float	BeamFactor { set { IntensityBeamFactor.Y = value; } }

		public Half2	IntensityBeamFactor;

		/// <summary>
		/// Gravity influence.
		/// Zero means no gravity influence.
		/// Values between 0 and 1 means reduced gravity, like snowflakes or dust.
		/// Negative values means particle that has positive buoyancy.
		/// </summary>
		public float Gravity 
		{ 
			set { GravityDamping.X = value; } 
			get { return GravityDamping.X; } 
		}

		/// <summary>
		/// Counter velocity deceleration
		/// </summary>
		public float Damping 
		{ 
			set { GravityDamping.Y = value; } 
			get { return GravityDamping.Y; } 
		}

		public Half2		GravityDamping;

		/// <summary>
		/// Initial size of the particle
		/// </summary>
		public float		Size0 { set { Size01.X = value; } }

		/// <summary>
		/// Terminal size of the particle
		/// </summary>
		public float		Size1 { set { Size01.Y = value; } }

		public Half2		Size01;

		/// <summary>
		/// Initial rotation of the particle
		/// </summary>
		public float Rotation0 
		{ 
			set { Rotation01.X = value; } 
			get { return Rotation01.X; } 
		}

		/// <summary>
		/// Terminal rotation of the particle
		/// </summary>
		public float Rotation1 
		{ 
			set { Rotation01.Y = value; } 
			get { return Rotation01.Y; } 
		}

		public Half2		Rotation01;

		/// <summary>
		/// Total particle life-time
		/// </summary>
		public float		LifeTime;          

		/// <summary>
		/// Lag between desired particle injection time and actual 
		/// particle injection time caused by discrete updating.
		/// Internally this field used as particle life-time counter.
		/// </summary>
		public float		TimeLag;

		/// <summary>
		/// Fade in time fraction
		/// </summary>
		public float		FadeIn { set { BitUtils.UIntSetUNorm8( ref FadingImageIndexCount, value, 0 ); } }

		/// <summary>
		/// Fade out time fraction
		/// </summary>
		public float		FadeOut { set { BitUtils.UIntSetUNorm8( ref FadingImageIndexCount, value, 1 ); } }

		/// <summary>
		/// 1  bit — weapon or not
		/// 15 bit - image count
		/// 16 bit - image index
		/// </summary>
		public uint			FadingImageIndexCount;

		/// <summary>
		/// Index of the image in the texture atlas
		/// </summary>
		public int ImageIndex { set { BitUtils.UIntSetByte( ref FadingImageIndexCount, (byte)value, 2 ); } }
		
		/// <summary>
		/// Number of frames
		/// </summary>
		public int ImageCount { set { BitUtils.UIntSetByte( ref FadingImageIndexCount, (byte)value, 3 ); } }

		/// <summary>
		/// Zero means world-space basis
		/// </summary>
		public bool	 WeaponIndex { set { BitUtils.UIntSetByte( ref FXData, (byte)(value ? 1 : 0), 3 ); } }

		/// <summary>
		/// Index of the image in the texture atlas
		/// </summary>
		public ParticleFX	Effects 
		{ 
			set { BitUtils.UIntSetByte( ref FXData, (byte)value, 0 ); } 
			get { return (ParticleFX)BitUtils.UIntGetByte( FXData, 0 ); } 
		}

		public uint FXData;
		

		void CheckFloat ( float value )
		{
			if (float.IsNaN(value)) throw new NotFiniteNumberException();
			if (float.IsPositiveInfinity(value)) throw new NotFiniteNumberException();
			if (float.IsNegativeInfinity(value)) throw new NotFiniteNumberException();
		}


		[Conditional("DEBUG")]
		public void Check ()
		{
			CheckFloat( Position.X );
			CheckFloat( Position.Y );
			CheckFloat( Position.Z );

			CheckFloat( Velocity.X );
			CheckFloat( Velocity.Y );
			CheckFloat( Velocity.Z );

			CheckFloat( LifeTime );
			CheckFloat( Damping );
			CheckFloat( Gravity );

			if (LifeTime<=0) {
				throw new NotFiniteNumberException();
			}
		}
	}
}
