﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using IronStar.Animation;
using IronStar.ECS;
using IronStar.Gameplay.Components;
using IronStar.SFX2;

namespace IronStar.Gameplay.Systems
{
	public class AttachmentData
	{
		public int BoneIndex = -1;
	}

	public class AttachmentSystem : ProcessingSystem<AttachmentData,AttachmentComponent,Transform>
	{

		protected override AttachmentData Create( Entity entity, AttachmentComponent attachment, Transform transform )
		{
			var data			=	new AttachmentData();
			var rootTransform	=	Matrix.Identity;

			TryGetRootBoneIndex( entity.gs, attachment, out data.BoneIndex );
			TryGetRootTransform( data, attachment, out rootTransform );

			if (attachment.AutoAttach)
			{
				var localTransform			=	transform.TransformMatrix * Matrix.Invert( rootTransform );
				attachment.LocalTransform	=	localTransform;

				return data;
			}
			else
			{
				transform.TransformMatrix	=	attachment.LocalTransform * rootTransform;
				return data;
			}
		}


		protected override void Destroy( Entity entity, AttachmentData resource )
		{
		}


		protected override void Process( Entity entity, GameTime gameTime, AttachmentData data, AttachmentComponent attachment, Transform transform )
		{
			//	target entity is dead: kill or drop entity
			if (!entity.gs.Exists(attachment.Target))
			{
				if (attachment.DropOnKill)
				{
					entity.RemoveComponent<AttachmentComponent>();
				}
				else
				{
					entity.Kill();
				}
			}
			else
			{
				var rootTransform = Matrix.Identity;

				if (TryGetRootTransform( data, attachment, out rootTransform ))
				{
					transform.TransformMatrix	=	attachment.LocalTransform * rootTransform;
				}
			}
		}

		/*-----------------------------------------------------------------------------------------
		 *	Utils :
		-----------------------------------------------------------------------------------------*/

		bool TryGetRootTransform( AttachmentData data, AttachmentComponent attachment, out Matrix rootTransform )
		{
			rootTransform = Matrix.Identity;
			
			var targetTransform = attachment.Target.GetComponent<Transform>();

			if (targetTransform==null)
			{	
				Log.Warning("AttachmentSystem: target entity does not have transform component");
				return false;
			}
			
			if (data.BoneIndex<0)
			{
				rootTransform	=	targetTransform.TransformMatrix;
				return true;
			}
			else
			{
				var bones = attachment.Target.GetComponent<BoneComponent>();

				if (bones==null)
				{
					rootTransform	=	targetTransform.TransformMatrix;
					Log.Warning("AttachmentSystem: target entity does not have bone component");
					return false;
				}
				else
				{
					rootTransform	=	bones.Bones[ data.BoneIndex ] * targetTransform.TransformMatrix;
					return true;
				}
			}
		}

	
		bool TryGetRootBoneIndex( IGameState gs, AttachmentComponent attachment, out int boneIndex )
		{
			boneIndex		=	-1;

			if (string.IsNullOrWhiteSpace(attachment.Bone))
			{
				return true;
			}
			else
			{
				var model	=	attachment.Target?.GetComponent<RenderModel>();
				var bones	=	attachment.Target?.GetComponent<BoneComponent>();

				if (model==null)
				{
					Log.Warning("AttachmentSystem: target model component is null");
					return false;
				}

				if (bones==null)
				{
					Log.Warning("AttachmentSystem: target bone component is null");
					return false;
				}

				var scene = model.LoadScene(gs);

				boneIndex = scene.GetNodeIndex( attachment.Bone );

				if (boneIndex<0)
				{
					Log.Warning("AttachmentSystem: bone {} does not exist", attachment.Bone);
					return false;
				}

				return true;
			}
		}
	}
}
