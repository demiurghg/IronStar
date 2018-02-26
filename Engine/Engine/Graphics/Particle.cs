using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core;
using System.Runtime.InteropServices;

namespace Fusion.Engine.Graphics {

	/// <summary>
	/// Defines particle behavior.
	/// </summary>
	[Flags]
	public enum ParticleFX : uint {

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
		/// Acceleration of the particle regardless of gravity.
		/// </summary>
		public Vector3	Acceleration;          

		/// <summary>
		/// Color of particle, tint for glowing particles.
		/// </summary>
		public Color3	Color;

		/// <summary>
		/// Alpha factor, transparency or threshold
		/// </summary>
		public float	Alpha;

		public float	Roughness;

		public float	Metallic;

		public float	Intensity;

		public float	Temperature;

		public float	Scattering;

		/// <summary>
		/// Gravity influence.
		/// Zero means no gravity influence.
		/// Values between 0 and 1 means reduced gravity, like snowflakes or dust.
		/// Negative values means particle that has positive buoyancy.
		/// </summary>
		public float		Gravity;                

		/// <summary>
		/// Counter velocity deceleration
		/// </summary>
		public float		Damping;                

		/// <summary>
		/// Initial size of the particle
		/// </summary>
		public float		Size0;                  

		/// <summary>
		/// Terminal size of the particle
		/// </summary>
		public float		Size1;                  

		/// <summary>
		/// Initial rotation of the particle
		/// </summary>
		public float		Rotation0;                 

		/// <summary>
		/// Terminal rotation of the particle
		/// </summary>
		public float		Rotation1;                 

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
		public float		FadeIn;                

		/// <summary>
		/// Fade out time fraction
		/// </summary>
		public float		FadeOut;               

		/// <summary>
		/// Index of the image in the texture atlas
		/// </summary>
		public int		ImageIndex;            

		/// <summary>
		/// Index of the image in the texture atlas
		/// </summary>
		public ParticleFX	Effects;            
	}
}
