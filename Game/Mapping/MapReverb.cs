﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using Fusion.Engine.Audio;
using IronStar.SFX;
using Fusion.Development;
using System.Drawing.Design;
using Fusion;
using Fusion.Core.Shell;
using Fusion.Core.Extensions;

namespace IronStar.Mapping {

	public class MapReverb : MapNode {

		[AECategory("Reverb")]
		public ReverbPreset ReverbPreset { get; set; } = ReverbPreset.OFF;

		[AEDisplayName("Min Distance")]
		public float MinimumDistance { get; set; } = 10;

		[AEDisplayName("Max Distance")]
		public float MaximumDistance { get; set; } = 20;

		ReverbZone reverb;


		/// <summary>
		/// 
		/// </summary>
		public MapReverb ()
		{
		}



		public override void SpawnNode( GameWorld world )
		{
			try {

				var ss		=	world.Game.GetService<SoundSystem>();
				reverb		=	ss.CreateReverbZone();
				reverb.Set3DParameters( this.TranslateVector, MinimumDistance, MaximumDistance );
				reverb.SetReverbParameters( ReverbPreset );

			} catch ( SoundException e ) {
				Log.Warning( e.Message );
			}
		}



		public override MapNode DuplicateNode()
		{
			var newNode = (MapReverb)MemberwiseClone();
			newNode.reverb		= null;
			newNode.NodeGuid = Guid.NewGuid();
			return newNode;
		}


		public override BoundingBox GetBoundingBox()
		{
			float sz = MaximumDistance / (float)Math.Sqrt(3);
			return new BoundingBox( sz, sz, sz );
		}
	}
}
