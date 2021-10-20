using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using IronStar.Gameplay.Components;

namespace IronStar.Gameplay.Systems
{
	class AttachmentSystem : ISystem
	{
		readonly Aspect attachmentAspect = new Aspect().Include<AttachmentComponent,Transform>();

		public Aspect GetAspect()
		{
			return Aspect.Empty;
		}

		public void Add( IGameState gs, Entity e ) {}
		public void Remove( IGameState gs, Entity e ) {}


		public static bool Attach( Entity entityToAttach, Entity targetEntity )
		{	
			if (targetEntity==null)
			{
				Log.Warning("Target entity to attach to is null");
				return false;
			}

			if (entityToAttach==null)
			{
				Log.Warning("Attaching entity is null");
				return false;
			}

			var attachTransform	=	entityToAttach?.GetComponent<Transform>();
			var targetTransform	=	targetEntity?.GetComponent<Transform>();

			if (attachTransform==null) 
			{
				Log.Warning("Attaching entity {0} has no transform", entityToAttach.ToString() );
				return false;
			}

			if (targetTransform==null) 
			{
				Log.Warning("Target entity {0} has no transform", targetEntity.ToString() );
				return false;
			}

			var localTransform	=	attachTransform.TransformMatrix * Matrix.Invert( targetTransform.TransformMatrix );

			entityToAttach.AddComponent( new AttachmentComponent( targetEntity.ID, localTransform ) ); 

			return true;
		}

		
		public void Update( IGameState gs, GameTime gameTime )
		{
			foreach ( var entity in gs.QueryEntities(attachmentAspect) )
			{
				var attachment		=	entity.GetComponent<AttachmentComponent>();
				var transform		=	entity.GetComponent<Transform>();

				var targetEntity	=	gs.GetEntity( attachment.TargetID );
				var targetTransform	=	targetEntity?.GetComponent<Transform>();

				if (targetTransform!=null)
				{
					transform.TransformMatrix	=	attachment.LocalTransform * targetTransform.TransformMatrix;
				}
			}
		}
	}
}
