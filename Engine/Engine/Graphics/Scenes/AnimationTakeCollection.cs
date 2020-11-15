using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using SharpDX;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Drivers.Graphics;
using Fusion.Core;
using Fusion.Core.Content;

namespace Fusion.Engine.Graphics.Scenes {

	public class AnimationTakeCollection : List<AnimationTake> {

		public AnimationTake this [ string name ] {
			get {
				if (name==null) return this.FirstOrDefault();
				else return this.FirstOrDefault( t => t.Name == name );
			}
		}

	}
}
