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
using IronStar.SFX;
using Newtonsoft.Json;
using Fusion.Core.Shell;
using Fusion.Core.Extensions;

namespace IronStar.Mapping {
	public class MapEntity : MapNode {


		/// <summary>
		/// for editor use only
		/// </summary>
		[XmlIgnore]
		[JsonIgnore]
		public Entity Entity = null;

		/// <summary>
		/// Entity target name
		/// </summary>
		[AECategory("Entity")]
		[Description("Entity target name")]
		public string TargetName { get; set; }

		/// <summary>
		/// Entity factory
		/// </summary>
		[Browsable(false)]
		[AEExpandable]
		[AECategory("Factory")]
		public EntityFactory Factory { get; set; }

		[AEIgnore]
		public string FactoryName { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public MapEntity ()
		{
			 FactoryName	=	Misc.GenerateRandomString(8);
		}



		public override void SpawnNode( GameWorld world )
		{
			Entity = world.Spawn( FactoryName );
			Entity?.Teleport( TranslateVector, RotateQuaternion );
		}



		public override void ActivateNode()
		{
			Entity?.Activate( null );
		}



		public override void UseNode()
		{
			Entity?.Use( null );
		}



		public override void DrawNode( GameWorld world, DebugRender dr, Color color, bool selected )
		{
			dr.DrawBasis( WorldMatrix, 1 );
			Factory.Draw( dr, WorldMatrix, color );

			if (selected) {
				if (Entity!=null) {
				
					var targets = world.GetTargets(Factory.TargetName);

					if (targets.Any()) {
						dr.DrawBox( new BoundingBox(0.5f, 0.5f, 0.5f), WorldMatrix, Color.Yellow );					}

					foreach ( var target in targets ) {

						var dir	= target.Position - Entity.Position;
						var len	= dir.Length();
						var dirn= dir.Normalized();

						var p0	= Entity.Position;
						var p1	= target.Position;
	
						dr.DrawLine( p0, p1, Color.Yellow, Color.Yellow, 1, 1 );
					}
				}
			}
		}



		public override void ResetNode( GameWorld world )
		{
			KillNode(world);
			SpawnNode(world);
		}



		public override void KillNode( GameWorld world )
		{
			if (Entity!=null) {
				world.KillImmediatly( Entity.ID );
			}
		}


		public override MapNode DuplicateNode()
		{
			var newNode = (MapEntity)MemberwiseClone();
			newNode.Factory		= Factory.Duplicate();
			newNode.Entity		= null;
			newNode.FactoryName	= Misc.GenerateRandomString(8);
			return newNode;
		}
	}
}
