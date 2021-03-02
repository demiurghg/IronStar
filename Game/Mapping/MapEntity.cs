using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using IronStar.SFX;
using Newtonsoft.Json;
using Fusion.Core.Shell;
using Fusion.Core.Extensions;
using Fusion.Widgets.Advanced;

namespace IronStar.Mapping {
	public class MapEntity : MapNode {

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
		public EntityFactoryContent Factory { get; set; }

		[AEIgnore]
		public string FactoryName { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public MapEntity ()
		{
			 FactoryName	=	Misc.GenerateRandomString(8);
		}



		public override void SpawnNodeECS( ECS.GameState gs )
		{
			ecsEntity = Factory.SpawnECS(gs);

			if (ecsEntity!=null)
			{
				gs.Teleport( ecsEntity, TranslateVector, RotateQuaternion );
			}
		}


		public override MapNode DuplicateNode()
		{
			var newNode = (MapEntity)MemberwiseClone();
			newNode.Factory		= Factory.Duplicate();
			newNode.FactoryName	= Misc.GenerateRandomString(8);
			newNode.NodeGuid = Guid.NewGuid();
			return newNode;
		}


		public override BoundingBox GetBoundingBox()
		{
			#warning Need more smart bounding box for entitites!
			return new BoundingBox( 4, 4, 4 );
		}
	}
}
