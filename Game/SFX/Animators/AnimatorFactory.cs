using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Extensions;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Fusion.Engine.Graphics;
using IronStar.Core;

namespace IronStar.SFX {

	public abstract class AnimatorFactory : JsonObject {

		public abstract Animator Create ( GameWorld world, Entity entity, ModelInstance model );
	}
}
