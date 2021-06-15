using System;
using FMOD;

namespace Fusion.Engine.Audio 
{
	[Serializable]
	public class SoundException : Exception 
	{

		public SoundException ()
		{
		}
		
		internal SoundException ( RESULT result, string format, params object[] args ) 
		 : base( result.ToString() + " : " + string.Format(format, args) )
		{
			
		}

		
		internal SoundException ( RESULT result, string message ) 
		 : base( result.ToString() + " : " + message )
		{
			
		}

	}
}
