using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMOD {
	using Fusion;
	using Fusion.Core.Mathematics;
	using Studio;

	public static class FmodExt {


		public static VECTOR Convert( Vector3 vector )
		{
			VECTOR result;
			result.x	=	vector.X;
			result.y	=	vector.Y;
			result.z	=	vector.Z;
			return result;
		}


		public static Vector3 Convert( VECTOR vector )
		{
			Vector3 result;
			result.X	=	vector.x;
			result.Y	=	vector.y;
			result.Z	=	vector.z;
			return result;
		}


		static internal void ERRCHECK( FMOD.RESULT result )
		{
			switch ( result ) {
				case FMOD.RESULT.OK: break;
				default: Log.Warning("FMOD Error: {0}", result); break;
			}
		}


		public static string GetPath( this EventDescription e )
		{
			string r;
			ERRCHECK( e.getPath(out r) );
			return r;
		}


		public static bool Is3D( this EventDescription e )
		{
			bool r;
			ERRCHECK( e.is3D(out r) );
			return r;
		}


		public static bool IsOneshot( this EventDescription e )
		{
			bool r;
			ERRCHECK( e.isOneshot(out r) );
			return r;
		}


		public static bool IsSnapshot( this EventDescription e )
		{
			bool r;
			ERRCHECK( e.isSnapshot(out r) );
			return r;
		}


		public static bool IsStream( this EventDescription e )
		{
			bool r;
			ERRCHECK( e.isStream(out r) );
			return r;
		}

	}
}
