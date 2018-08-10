using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using SharpDX;
using Fusion.Core;
using Fusion.Engine.Common;
using Fusion.Core.Configuration;
using FMOD.Studio;


namespace Fusion.Engine.Audio {
	public sealed partial class SoundSystem : GameComponent {

		
		FMOD.Studio.System studio;
		FMOD.System lowlevel;

		public FMOD.Studio.System StudioSystem {
			get { return studio; }
		}

		public FMOD.Studio.System LowLevelSystem {
			get { return studio; }
		}



		/// <summary>
		/// 
		/// </summary>
		public SoundSystem ( Game game ) : base(game)
		{
		}



		/// <summary>
		/// 
		/// </summary>
        public override void Initialize()
        {
			soundWorld = new SoundWorld(Game);

			ERRCHECK( FMOD.Studio.System.create( out studio ) );
			ERRCHECK( studio.getLowLevelSystem( out lowlevel ) );
			ERRCHECK( lowlevel.setSoftwareFormat( 0, FMOD.SPEAKERMODE._5POINT1, 0 ) );
			ERRCHECK( studio.initialize( 1024, INITFLAGS.NORMAL, FMOD.INITFLAGS.NORMAL, IntPtr.Zero ) );
        }



		/// <summary>
		/// Checks FMOD call
		/// </summary>
		/// <param name="result"></param>
		static internal void ERRCHECK( FMOD.RESULT result )
		{
			switch ( result ) {
				case FMOD.RESULT.OK: break;
				default: Log.Error("FMOD Error: {0}", result); break;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
			if (disposing) {
				if (studio!=null) {
					ERRCHECK( studio.release() );
					studio = null;
				}
			}
        }



		/// <summary>
		/// Gets default sound world.
		/// </summary>
		public SoundWorld SoundWorld {
			get { return soundWorld; }
		}


		SoundWorld soundWorld;


		/// <summary>
		/// Updates sound.
		/// </summary>
		internal void Update ( GameTime gameTime )
		{
		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	3D sound stuff :
		 * 
		-----------------------------------------------------------------------------------------*/

	}

}
