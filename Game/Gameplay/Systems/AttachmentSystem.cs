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
			return attachmentAspect;
		}

		public void Add( IGameState gs, Entity entityToAttach ) 
		{
			var attachment		=	entityToAttach.GetComponent<AttachmentComponent>();
			var targetEntity	=	attachment.Target;

			var attachTransform	=	entityToAttach?.GetComponent<Transform>();
			var targetTransform	=	targetEntity?.GetComponent<Transform>();

			if (attachTransform==null) 
			{
				Log.Warning("Attaching entity {0} has no transform", entityToAttach?.ToString() );
				return;
			}

			if (targetTransform==null) 
			{
				Log.Warning("Target entity {0} has no transform", targetEntity?.ToString() );
				return;
			}

			var localTransform			=	attachTransform.TransformMatrix * Matrix.Invert( targetTransform.TransformMatrix );
			attachment.LocalTransform	=	localTransform;
		}


		public void Remove( IGameState gs, Entity e ) {}

		
		public void Update( IGameState gs, GameTime gameTime )
		{
			foreach ( var entity in gs.QueryEntities(attachmentAspect) )
			{
				var attachment		=	entity.GetComponent<AttachmentComponent>();
				var transform		=	entity.GetComponent<Transform>();

				var targetEntity	=	attachment.Target;
				var targetTransform	=	targetEntity?.GetComponent<Transform>();

				//	target entity is dead
				//	kill current entity too
				if (!gs.Exists(targetEntity))
				{
					entity.Kill();
				}

				if (targetTransform!=null)
				{
					transform.TransformMatrix	=	attachment.LocalTransform * targetTransform.TransformMatrix;
				}
			}
		}
	}
}
