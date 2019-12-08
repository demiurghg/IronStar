﻿using System;
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

namespace Fusion.Build.Processors {

	[AssetProcessor("Json", "Copies Json file and assigns proper type name in asset file")]
	public class JsonProcessor : AssetProcessor {
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sourceStream"></param>
		/// <param name="targetStream"></param>
		public override void Process ( AssetSource assetFile, BuildContext context )
		{
			var settings = JsonFactory.SerialiazationSettings;

			using ( var sourceStream = assetFile.OpenSourceStream() ) 
			{
				using ( var streamReader = new StreamReader( sourceStream ) ) 
				{
					var inputText = streamReader.ReadToEnd();
					var jsonObj = JsonConvert.DeserializeObject(inputText, settings);

					using ( var targetStream = assetFile.OpenTargetStream(jsonObj.GetType()) ) 
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
