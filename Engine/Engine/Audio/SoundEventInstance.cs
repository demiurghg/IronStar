#region License
// /*
// Microsoft Public License (Ms-PL)
// MonoGame - Copyright © 2009 The MonoGame Team
// 
// All rights reserved.
// 
// This license governs use of the accompanying software. If you use the software, you accept this license. If you do not
// accept the license, do not use the software.
// 
// 1. Definitions
// The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under 
// U.S. copyright law.
// 
// A "contribution" is the original software, or any additions or changes to the software.
// A "contributor" is any person that distributes its contribution under this license.
// "Licensed patents" are a contributor's patent claims that read directly on its contribution.
// 
// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
// each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
// each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
// 
// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, 
// your patent license from such contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution 
// notices that are present in the software.
// (D) If you distribute any portion of the software in source code form, you may do so only under this license by including 
// a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object 
// code form, you may only do so under a license that complies with this license.
// (E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees
// or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent
// permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular
// purpose and non-infringement.
// */
#endregion License

using System;
using SharpDX.XAudio2;
using FMOD;
using FMOD.Studio;
using Fusion.Core;
using Fusion.Core.Mathematics;


namespace Fusion.Engine.Audio
{
	public sealed class SoundEventInstance
	{
		readonly EventDescription desc;
		readonly EventInstance inst;
		readonly FMOD.Studio.System system;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="effect"></param>
		/// <param name="voice"></param>
        internal SoundEventInstance( SoundSystem device, EventDescription desc )
        {
			this.desc	=	desc;
			this.inst	=	null;
			this.system	=	device.system;

			FmodExt.ERRCHECK( desc.createInstance( out inst ) );

			inst.setReverbLevel(0,1);
        }


		public override string ToString()
		{
			string path;
			desc.getPath( out path );
			return string.Format("[{0}]", path);
		}


		/// <summary>
		/// 
		/// </summary>
		public void Release()
        {
			FmodExt.ERRCHECK( inst.release() );
		}


		public void Start ()
		{
			FmodExt.ERRCHECK( inst.start() );
		}


		public void Stop ( bool immediate )
		{
			var mode = immediate ? STOP_MODE.IMMEDIATE : STOP_MODE.ALLOWFADEOUT;
			FmodExt.ERRCHECK( inst.stop( mode ) );
		}



		public void Set3DParameters ( Vector3 position, Vector3 forward, Vector3 up, Vector3 velocity )
		{
			FMOD.Studio._3D_ATTRIBUTES attrs;

			attrs.forward	=	FmodExt.Convert( forward	);
			attrs.position	=	FmodExt.Convert( position	);
			attrs.up		=	FmodExt.Convert( up			);
			attrs.velocity	=	FmodExt.Convert( velocity	);

			FmodExt.ERRCHECK( inst.set3DAttributes( attrs ) );
		}


		public void Set3DParameters ( Vector3 position )
		{
			Set3DParameters( position, Vector3.ForwardRH, Vector3.Up, Vector3.Zero );
		}


		public void Set3DParameters ( Vector3 position, Vector3 velocity )
		{
			Set3DParameters( position, Vector3.ForwardRH, Vector3.Up, velocity );
		}



		PLAYBACK_STATE GetPlaybackState()
		{
			PLAYBACK_STATE result;
			FmodExt.ERRCHECK( inst.getPlaybackState( out result ) );
			return result;
		}


		public bool IsStopped {
			get {
				return GetPlaybackState() == PLAYBACK_STATE.STOPPED;
			}
		}


		public float ReverbLevel 
		{
			set 
			{
				FmodExt.ERRCHECK( inst.setReverbLevel( 0, value ) );
			}
			get 
			{
				float result;
				FmodExt.ERRCHECK( inst.getReverbLevel( 0, out result ) );
				return result;
			}
		}
	}
}
