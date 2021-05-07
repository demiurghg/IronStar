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
using IronStar.ECS;

namespace IronStar.Mapping 
{
	public class MapEntity : MapNode 
	{

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

		/// <summary>
		/// 
		/// </summary>
		public MapEntity ()
		{
		}



		public override void SpawnNodeECS( ECS.GameState gs )
		{
			ecsEntity		=	Factory.SpawnECS(gs);
			ecsEntity.Tag	=	this;

			if (ecsEntity!=null)
			{
				gs.Teleport( ecsEntity, Translation, Rotation );
			}
		}


		public override MapNode DuplicateNode()
		{
			var newNode = (MapEntity)base.DuplicateNode();
			newNode.Factory		= Factory.Duplicate();
			return newNode;
		}


		public override BoundingBox GetBoundingBox( GameState gs )
		{
			#warning Need more smart bounding box for entitites!
			return new BoundingBox( 4, 4, 4 );
		}
	}
}
