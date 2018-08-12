using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FMOD;
using FMOD.Studio;
using Fusion.Core;
using Fusion.Core.Mathematics;


namespace Fusion.Engine.Audio {
	public class ReverbZone {

		readonly FMOD.Studio.System system;
		readonly FMOD.System lowlevel;
		Reverb3D reverb;



		internal ReverbZone ( SoundSystem device )
		{				
			system		=	device.system;
			lowlevel	=	device.lowlevel;

			FmodExt.ERRCHECK( lowlevel.createReverb3D( out reverb ) );
			FmodExt.ERRCHECK( reverb.setActive( true ) );
		}



		public void Release ()
		{
			FmodExt.ERRCHECK( reverb.release() );
		}

		

		public void Set3DParameters ( Vector3 position, float minDistance, float maxDistance )
		{
			var pos = FmodExt.Convert( position );
			FmodExt.ERRCHECK( reverb.set3DAttributes( ref pos, minDistance, maxDistance ) );
		}



		public void SetReverbParameters ( ReverbPreset preset )
		{
			var reverbParams = PRESET.OFF();
			switch ( preset ) {
				case ReverbPreset.OFF				: reverbParams = PRESET.OFF				 (); break;
				case ReverbPreset.GENERIC			: reverbParams = PRESET.GENERIC			 (); break;
				case ReverbPreset.PADDEDCELL		: reverbParams = PRESET.PADDEDCELL		 (); break;
				case ReverbPreset.ROOM				: reverbParams = PRESET.ROOM			 (); break;
				case ReverbPreset.BATHROOM			: reverbParams = PRESET.BATHROOM		 (); break;
				case ReverbPreset.LIVINGROOM		: reverbParams = PRESET.LIVINGROOM		 (); break;
				case ReverbPreset.STONEROOM			: reverbParams = PRESET.STONEROOM		 (); break;
				case ReverbPreset.AUDITORIUM		: reverbParams = PRESET.AUDITORIUM		 (); break;
				case ReverbPreset.CONCERTHALL		: reverbParams = PRESET.CONCERTHALL		 (); break;
				case ReverbPreset.CAVE				: reverbParams = PRESET.CAVE			 (); break;
				case ReverbPreset.ARENA				: reverbParams = PRESET.ARENA			 (); break;
				case ReverbPreset.HANGAR			: reverbParams = PRESET.HANGAR			 (); break;
				case ReverbPreset.CARPETTEDHALLWAY	: reverbParams = PRESET.CARPETTEDHALLWAY (); break;
				case ReverbPreset.HALLWAY			: reverbParams = PRESET.HALLWAY			 (); break;
				case ReverbPreset.STONECORRIDOR		: reverbParams = PRESET.STONECORRIDOR	 (); break;
				case ReverbPreset.ALLEY				: reverbParams = PRESET.ALLEY			 (); break;
				case ReverbPreset.FOREST			: reverbParams = PRESET.FOREST			 (); break;
				case ReverbPreset.CITY				: reverbParams = PRESET.CITY			 (); break;
				case ReverbPreset.MOUNTAINS			: reverbParams = PRESET.MOUNTAINS		 (); break;
				case ReverbPreset.QUARRY			: reverbParams = PRESET.QUARRY			 (); break;
				case ReverbPreset.PLAIN				: reverbParams = PRESET.PLAIN			 (); break;
				case ReverbPreset.PARKINGLOT		: reverbParams = PRESET.PARKINGLOT		 (); break;
				case ReverbPreset.SEWERPIPE			: reverbParams = PRESET.SEWERPIPE		 (); break;
				case ReverbPreset.UNDERWATER		: reverbParams = PRESET.UNDERWATER		 (); break;
			}

			FmodExt.ERRCHECK( reverb.setProperties( ref reverbParams ) );
		}
	}
}
