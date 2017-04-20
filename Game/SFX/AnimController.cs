using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Engine.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using Fusion.Engine.Audio;


namespace IronStar.SFX {
	public class AnimController {


		public Matrix[] Transforms {
			get {
				throw new NotImplementedException();
			}
		}

		
		public AnimController ( Scene scene, Scene[] clips, int numTracks )
		{
			throw new NotImplementedException();
		}


		public AnimTrack GetTrack( int index )
		{
			throw new NotImplementedException();
		}


		public void Evaluate ( TimeSpan deltaTime )
		{
			throw new NotImplementedException();
		}
	}
}
