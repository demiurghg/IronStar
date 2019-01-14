using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Fusion.Core.Mathematics;
using IronStar.Core;
using Fusion.Engine.Graphics;
using Fusion.Engine.Audio;
using IronStar.SFX;
using Fusion.Development;
using System.Drawing.Design;
using Fusion;
using Fusion.Core.Shell;
using Fusion.Core.Extensions;

namespace IronStar.Mapping {

	public class MapSound : MapNode {

		[AECategory("Sound")]
		public string SoundEvent { get; set; } = "";

		[AECategory("Sound")]
		[AEValueRange(0,1, 0.125f, 0.125f/16)]
		public float ReverbLevel { get; set; } = 1;

		SoundEvent soundEvent;
		SoundEventInstance soundInstance;


		/// <summary>
		/// 
		/// </summary>
		public MapSound ()
		{
		}



		public override void SpawnNode( GameWorld world )
		{
			try {

				var ss		=	world.Game.GetService<SoundSystem>();

				if (string.IsNullOrWhiteSpace(SoundEvent)) {
					return;
				}
				
				soundEvent		=	ss.GetEvent( SoundEvent );

				soundInstance	=	soundEvent.CreateInstance();
				
				soundInstance.Set3DParameters( TranslateVector );
				soundInstance.ReverbLevel = ReverbLevel;
				soundInstance.Start();
				

			} catch ( SoundException e ) {
				Log.Warning( e.Message );
			}
		}



		public override void ResetNode( GameWorld world )
		{
			soundInstance?.Set3DParameters( TranslateVector );

			if (soundEvent!=null && soundEvent.Path!=SoundEvent) {
				KillNode(world);
				SpawnNode(world);
			}
		}



		public override void ActivateNode()
		{
		}



		public override void UseNode()
		{
		}



		public override void DrawNode( GameWorld world, DebugRender dr, Color color, bool selected )
		{
			var transform	=	WorldMatrix;

			var dispColor   =	color; 

			dr.DrawPoint( transform.TranslationVector, 1, color, 1 );

			if (soundEvent!=null) {

				float innerRadius = soundEvent.MinimumDistance;
				float outerRadius = soundEvent.MaximumDistance;

				dr.DrawPoint ( transform.TranslationVector, 0.33f, dispColor, 1 );

				if (selected) {
					dr.DrawSphere( transform.TranslationVector, innerRadius, dispColor );
					dr.DrawRing	 ( transform.TranslationVector, outerRadius, dispColor );
				} else {
					dr.DrawSphere( transform.TranslationVector, innerRadius, dispColor );
				}

			} else {

				dr.DrawPoint ( transform.TranslationVector, 0.33f, Color.Red, 2 );

				if (selected) {

					dr.DrawSphere( transform.TranslationVector, 1, color );
					dr.DrawRing	 ( transform.TranslationVector, 2, color );

				} else {
					
					dr.DrawSphere( transform.TranslationVector, 1, Color.Red );
					dr.DrawRing	 ( transform.TranslationVector, 2, Color.Red );
					
				}

			}
		}



		public override void KillNode( GameWorld world )
		{
			soundInstance?.Stop(true);
			soundInstance?.Release();
			soundInstance = null;
		}


		public override MapNode DuplicateNode()
		{
			var newNode = (MapSound)MemberwiseClone();
			newNode.soundEvent		= null;
			newNode.soundInstance	= null;
			return newNode;
		}



		public override BoundingBox GetBoundingBox()
		{
			#warning Need more smart bounding box for entitites!
			return new BoundingBox( 4, 4, 4 );
		}
	}
}
