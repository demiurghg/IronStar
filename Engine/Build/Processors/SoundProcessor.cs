using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion.Core.Shell;
using Fusion.Engine.Audio;
using Fusion.Core.Content;

namespace Fusion.Build.Processors {

	[AssetProcessor("Sounds", "No description")]
	public class SoundProcessor : AssetProcessor {
		
		public override string GenerateParametersHash()
		{
			return ContentUtils.CalculateMD5Hash(GetType().AssemblyQualifiedName);
		}


		public override void Process ( AssetSource assetFile, IBuildContext context )
		{
			using ( var sourceStream = assetFile.OpenSourceStream() ) {
				using ( var targetStream = assetFile.OpenTargetStream(typeof(SoundBank)) ) {
					sourceStream.CopyTo( targetStream );
				}
			}
		}
	}
}
