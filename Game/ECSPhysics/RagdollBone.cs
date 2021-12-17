using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics.Scenes;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.CollisionShapes.ConvexShapes;
using Fusion.Engine.Graphics;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.PositionUpdating;
using BepuEntity = BEPUphysics.Entities.Entity;
using BEPUphysics.EntityStateManagement;

namespace IronStar.ECSPhysics
{
	public class RagdollBone
	{
		readonly public int Index;
		readonly public BepuEntity PhysEntity;
		readonly public Node Node;
		readonly Matrix physToBone;

		public RagdollBone( int index, Node node, BepuEntity physEntity )
		{
			Index		=	index;
			Node		=	node;
			PhysEntity	=	physEntity;

			var physTransform	=	MathConverter.Convert( physEntity.MotionState.WorldTransform );
			var bindPose		=	node.BindPose;

			physToBone			=	bindPose * Matrix.Invert( physTransform );
		}

		public Matrix GetPhysicsTransform()
		{
			return MathConverter.Convert( PhysEntity.WorldTransform );
		}

		public Matrix ComputeSimulatedBoneTransform( Matrix invModelWorldTransform )
		{
			var physTransform	=	MathConverter.Convert( PhysEntity.WorldTransform );
			return	physToBone * physTransform * invModelWorldTransform;
		}
	}
}
