using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion.Core.Shell;
using Fusion.Core.Content;

namespace Fusion.Build.Processors {

	[AssetProcessor("Copy", "Copies file to target directory as byte array asset")]
	public class CopyProcessor : AssetProcessor {

		readonly Type targetType;


		public CopyProcessor ()
		{
			targetType = typeof(byte[]);
		}


		public CopyProcessor ( Type targetType )
		{
			this.targetType = targetType;
		}

		
		public override string GenerateParametersHash()
		{
			return ContentUtils.CalculateMD5Hash( GetType().AssemblyQualifiedName + "####" + targetType.AssemblyQualifiedName );
		}


		public override void Process ( AssetSource assetFile, IBuildContext context )
		{
			using ( var sourceStream = assetFile.OpenSourceStream() ) 
			{
				using ( var targetStream = assetFile.OpenTargetStream(targetType) ) 
				{
					sourceStream.CopyTo( targetStream );
				}
			}
		}
	}
}
