using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Utils;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using System.IO;
using IronStar.SFX;
using Fusion.Engine.Graphics;
using Fusion.Core.Content;
using Fusion.Engine.Common;
using IronStar.Views;

namespace IronStar.Core 
{
	/// <summary>
	/// Entity component.
	/// </summary>
	public abstract class EntityComponent : DisposableBase
	{
		public virtual void Update ( GameTime gameTime ) {}
	}
}
