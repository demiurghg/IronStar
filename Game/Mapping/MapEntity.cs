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
	public class MapEntity : MapNode, IFactory 
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
		public EntityFactory Factory { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public MapEntity ()
		{
		}



		public void Construct( Entity entity, IGameState gs )
		{
			( (IFactory)Factory ).Construct( entity, gs );
		}


		public override void SpawnNodeECS( ECS.IGameState gs )
		{
			//	OLD STUFF :
			/*ecsEntity			=	Factory.SpawnECS(gs);
			ecsEntity.Tag		=	this;

			if (ecsEntity!=null)
			{
				gs.Teleport( ecsEntity, Translation, Rotation );
			}

			Factory2	=	(EntityFactory)Factory.GenerateEntityFactory( gs ).Clone();*/
			
			//	NEW STUFF :
			Factory.Position	=	this.Translation;
			Factory.Rotation	=	this.Rotation;
			Factory.Scaling	=	this.Scaling.X;

			ecsEntity		=	gs.Spawn( this );
			ecsEntity.Tag	=	this;
		}


		public override MapNode DuplicateNode()
		{
			var newNode		=	(MapEntity)base.DuplicateNode();
			newNode.Factory	=	(EntityFactory)Factory.Clone();
			return newNode;
		}


		public override BoundingBox GetBoundingBox( IGameState gs )
		{
			// #TODO #MAP -- Need more smart bounding box for entitites!
			return new BoundingBox( 4, 4, 4 );
		}
	}
}
