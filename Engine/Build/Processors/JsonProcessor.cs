using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion.Core.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Fusion.Core;
using Fusion.Core.Content;

namespace Fusion.Build.Processors {

	[AssetProcessor("Json", "Copies Json file and assigns proper type name in asset file")]
	public class JsonProcessor : AssetProcessor {
		
		public override string GenerateParametersHash()
		{
			return ContentUtils.CalculateMD5Hash(GetType().AssemblyQualifiedName);
		}


		public override void Process ( AssetSource assetFile, IBuildContext context )
		{
			var settings = JsonUtils.DefaultSettings;

			using ( var sourceStream = assetFile.OpenSourceStream() ) 
			{
				using ( var streamReader = new StreamReader( sourceStream ) ) 
				{
					var inputText	= streamReader.ReadToEnd();
					var jsonObj		= JsonConvert.DeserializeObject(inputText, settings);
					var targetType	= jsonObj.GetType();

					using ( var targetStream = assetFile.OpenTargetStream(targetType) ) 
					{
						settings.Formatting	=	Formatting.None;
						var outputText		= JsonConvert.SerializeObject( jsonObj, settings );

						using ( var streamWriter = new StreamWriter( targetStream ) ) 
						{
							streamWriter.Write( outputText );
						}
					}
				}
			}
		}
	}
}
