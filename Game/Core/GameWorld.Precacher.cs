using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Content;
using Fusion.Engine.Server;
using Fusion.Engine.Client;
using Fusion.Core.Extensions;
using IronStar.SFX;
using Fusion.Core.IniParser.Model;
using Fusion.Engine.Graphics;
using IronStar.Mapping;
using System.Threading;
using Fusion.Engine.Graphics.Lights;

namespace IronStar.Core {

	/// <summary>
	/// World represents entire game state.
	/// </summary>
	public partial class GameWorld  {

		public class Precacher : IContentPrecacher {

			readonly ContentManager content;
			readonly string serverInfo;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="content"></param>
			/// <param name="serverInfo"></param>
			public Precacher ( ContentManager content, string serverInfo )
			{
				this.content	=	content;
				this.serverInfo	=	serverInfo;
			}


			/// <summary>
			/// 
			/// </summary>
			void IContentPrecacher.LoadContent()
			{
				content	.EnumerateAssets("fx")
						.Select( name => content.PrecacheSafe<FXFactory>(@"fx\"+name) )
						.ToArray();


				//content	.EnumerateAssets("models")
				//		.Select( name => content.PrecacheSafe<ModelFactory>(@"models\"+name) )
				//		.ToArray();

				content	.EnumerateAssets("entities")
						.Select( name => content.PrecacheSafe<EntityFactory>(@"entities\"+name) )
						.ToArray();

				content.PrecacheSafe<TextureAtlas>(@"sprites\particles");
				content.PrecacheSafe<TextureAtlas>(@"spots\spots");
				content.PrecacheSafe<VirtualTexture>("*megatexture");

				var mapName =  serverInfo;

				content.PrecacheSafe<LightProbeGBufferCache>(Path.Combine(RenderSystem.LightmapPath, mapName + "_irrcache"));
				content.PrecacheSafe<LightMap>				(Path.Combine(RenderSystem.LightmapPath, mapName + "_irrmap"));

				var map = content.PrecacheSafe<Map>(@"maps\" + mapName);
			}
		}

	}
}
