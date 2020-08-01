using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core;
using Fusion.Engine.Common;
using Fusion.Drivers.Graphics;
using System.Runtime.InteropServices;
using Fusion.Core.Configuration;
using Fusion.Engine.Graphics.Ubershaders;
using System.IO;

namespace Fusion.Engine.Graphics {

	/// <summary>
	/// Represents particle rendering and simulation system.
	/// </summary>
	public class ParticleSystem : DisposableBase {

		readonly Game Game;
		readonly RenderSystem rs;
		RenderWorld	renderWorld;

		public float SimulationStepTime { get; set; }

		
		ParticleStream			softStream;
		ParticleStream			hardStream;
		ParticleStream			dudvStream;

		public ParticleStream	SoftStream {
			get { return softStream; }
		}


		/// <summary>
		/// Gets and sets overall particle gravity.
		/// Default -9.8.
		/// </summary>
		public Vector3	Gravity { get; set; } = -9.81f * Vector3.Down;


		/// <summary>
		/// Sets and gets images for particles.
		/// This property must be set before particle injection.
		/// To prevent interference between textures in atlas all images must be padded with 16 pixels.
		/// </summary>
		#warning Remove this property, pass TextureAtlas from outside as paramter of Render() function
		public TextureAtlas Images {
			get {
				if (images!=null && images.Texture==null) return null;
				return images;
			}
			set {
				if (value!=null && value.Count>ParticleStream.MAX_IMAGES) {
					throw new ArgumentOutOfRangeException("Number of subimages in texture atlas is greater than " + ParticleStream.MAX_IMAGES.ToString() );
				}
				images = value;
			}
		}

		TextureAtlas	images = null;
		DynamicTexture		colorTemp;

		public DynamicTexture ColorTempMap {
			get { return colorTemp; }
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="rs"></param>
		internal ParticleSystem ( RenderSystem rs, RenderWorld renderWorld )
		{
			this.rs				=	rs;
			this.Game			=	rs.Game;
			this.renderWorld	=	renderWorld;

			softStream			=	new ParticleStream( rs, renderWorld, this, true, true );
			hardStream			=	new ParticleStream( rs, renderWorld, this, false, false );
			dudvStream			=	new ParticleStream( rs, renderWorld, this, false, false );

			colorTemp			=	CreateColorTemperatureMap();
		}



		DynamicTexture CreateColorTemperatureMap ()
		{
			var data = Temperature.GetRawTemperatureData().Select( v => new Color( v, 1.0f ) ).ToArray();

			var tex = new DynamicTexture( rs, data.Length, 1, typeof(Color), false, false );
			tex.SetData( data );

			return tex;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {	

				SafeDispose( ref softStream );
				SafeDispose( ref hardStream );
				SafeDispose( ref dudvStream );

				SafeDispose( ref colorTemp );
			}
			base.Dispose( disposing );
		}


		/// <summary>
		/// Injects hard particle.
		/// </summary>
		/// <param name="particle"></param>
		public void InjectParticle ( Particle particle )
		{
			switch ( particle.Effects ) {

				case ParticleFX.Hard:
				case ParticleFX.HardLit:
				case ParticleFX.HardLitShadow:
					hardStream.InjectParticle( ref particle );
					break;

				case ParticleFX.Soft:
				case ParticleFX.SoftLit:
				case ParticleFX.SoftLitShadow:
					softStream.InjectParticle( ref particle );
					break;

				case ParticleFX.Distortive:
					dudvStream.InjectParticle( ref particle );
					break;

				default:
					Log.Warning("Inject particle: bad FX type {0}", particle.Effects );
					break;
			}
		}


		/// <summary>
		/// Immediatly kills all living particles.
		/// </summary>
		/// <returns></returns>
		public void KillParticles ()
		{
			softStream?.KillParticles();
			hardStream?.KillParticles();
			dudvStream?.KillParticles();
		}


		/// <summary>
		/// Updates particle properties.
		/// </summary>
		/// <param name="gameTime"></param>
		internal void Simulate ( GameTime gameTime, Camera camera )
		{
			softStream?.Simulate( gameTime, camera );
			hardStream?.Simulate( gameTime, camera );
			dudvStream?.Simulate( gameTime, camera );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		internal void RenderHard ( GameTime gameTime, Camera camera, StereoEye stereoEye, HdrFrame viewFrame )
		{
			hardStream.RenderHard( gameTime, camera, stereoEye, viewFrame );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		internal void RenderSoft ( GameTime gameTime, Camera camera, StereoEye stereoEye, HdrFrame viewFrame )
		{
			softStream.RenderSoft	 ( gameTime, camera, stereoEye, viewFrame );
			softStream.RenderVelocity( gameTime, camera, stereoEye, viewFrame );

			dudvStream.RenderDuDv	 ( gameTime, camera, stereoEye, viewFrame );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		internal void RenderLight ( GameTime gameTime, Camera camera )
		{
			softStream.RenderLightMap( gameTime, camera );
			hardStream.RenderBasisLight( gameTime );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		internal void RenderShadow ( GameTime gameTime, Viewport viewport, Camera camera, RenderTargetSurface particleShadow, DepthStencilSurface depthBuffer )
		{
			softStream.RenderShadow( gameTime, viewport, camera, particleShadow, depthBuffer, true );
			hardStream.RenderShadow( gameTime, viewport, camera, particleShadow, depthBuffer, false );
		}
	}
}
