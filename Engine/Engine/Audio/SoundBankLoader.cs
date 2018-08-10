using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SharpDX.Multimedia;
using SharpDX.XAudio2;
using SharpDX.X3DAudio;
using Fusion.Core.Content;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Extensions;


namespace Fusion.Engine.Audio {
	[ContentLoader(typeof(SoundBank))]
	internal sealed class SoundBankLoader : ContentLoader {

		public override object Load ( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			var ss		= content.Game.GetService<SoundSystem>();
			var data	= stream.ReadAllBytes();

			return new SoundBank( ss, data );
		}
	}
		
}
