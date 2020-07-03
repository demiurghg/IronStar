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
using FMOD;
using FMOD.Studio;
using Fusion.Core.Input;
using Fusion.Core.Mathematics;
using Fusion.Core.Content;
using Fusion.Core.Extensions;

namespace Fusion.Engine.Audio {

	[ContentLoader(typeof(SoundBank))]
	public sealed class SoundBankLoader : ContentLoader
	{
		public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			return new SoundBank( content.Game.SoundSystem, stream.ReadAllBytes() );
		}
	}

	public sealed partial class SoundSystem : GameComponent {

		internal FMOD.Studio.System system;
		internal FMOD.System		lowlevel;


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
			var studioFlags		=	FMOD.Studio.INITFLAGS.NORMAL | FMOD.Studio.INITFLAGS.LIVEUPDATE;
			var lowlevelFlags	=	FMOD.INITFLAGS.NORMAL;
			var speakerMode		=	FMOD.SPEAKERMODE._5POINT1;

			FmodExt.ERRCHECK( FMOD.Studio.System.create( out system ) );
			FmodExt.ERRCHECK( system.getLowLevelSystem( out lowlevel ) );
			FmodExt.ERRCHECK( lowlevel.setSoftwareFormat( 0, speakerMode, 0 ) );
			FmodExt.ERRCHECK( system.initialize( 1024, studioFlags, lowlevelFlags, IntPtr.Zero ) );

			lowlevel.set3DSettings( 1, 3.28f, 1 );

			uint plugin;
			FmodExt.ERRCHECK( lowlevel.loadPlugin("fmod_distance_filter", out plugin ) );
        }


		public SoundBank LoadSoundBank ( ContentManager content, string path )
		{
			return content.Load<SoundBank>( path );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
			if (disposing) {
				if (system!=null) {
					FmodExt.ERRCHECK( system.release() );
					system = null;
				}
			}
        }


		/// <summary>
		/// Updates sound.
		/// </summary>			
		public override void Update( GameTime gameTime )
		{
			FmodExt.ERRCHECK( system.update() );
		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	3D sound stuff :
		 * 
		-----------------------------------------------------------------------------------------*/

		/// <summary>
		/// 
		/// </summary>
		/// <param name="position"></param>
		/// <param name="forward"></param>
		/// <param name="up"></param>
		public void SetListener ( Vector3 position, Vector3 forward, Vector3 up, Vector3 velocity )
		{
			FMOD.Studio._3D_ATTRIBUTES attrs;

			attrs.forward	=	FmodExt.Convert( -forward	);
			attrs.position	=	FmodExt.Convert( position	);
			attrs.up		=	FmodExt.Convert( up		);
			attrs.velocity	=	FmodExt.Convert( velocity	);

			FmodExt.ERRCHECK( system.setListenerAttributes( 0, attrs ) );
		}



		/// <summary>
		/// Gets event by name
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public SoundEvent GetEvent ( string path )
		{
			EventDescription eventDesc;
			string eventPath = Path.Combine(@"event:/", path);

			var result = system.getEvent( eventPath, out eventDesc );

			if (result!=FMOD.RESULT.OK) {
				throw new SoundException( result, eventPath );
			}

			return new SoundEvent( this, eventDesc );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public ReverbZone CreateReverbZone ()
		{
			return new ReverbZone( this );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public bool Play2DEvent ( string path )
		{
			EventDescription desc;
			EventInstance inst;
			var eventPath = Path.Combine(@"event:/", path);

			FmodExt.ERRCHECK( system.getEvent(eventPath, out desc) );

			if (desc==null) {
				Log.Warning("Failed to play event: {0}", eventPath );
				return false;
			}

			bool is3d;
			FmodExt.ERRCHECK( desc.is3D( out is3d ) );

			if (is3d) {
				Log.Warning("Event '{0}' is 3D", eventPath);
			}

			FmodExt.ERRCHECK( desc.createInstance( out inst ) );

			if (inst==null) {
				return false;
			}

			inst.start();
			inst.release();

			return true;
		}
	}

}
