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
using BEPUutilities.DataStructures;

namespace IronStar.ECSPhysics
{
	public class RagdollBone
	{
		struct NodeIndex
		{
			public NodeIndex( int idx, Node node ) { Index = idx; Node = node; }
			public readonly int Index;
			public readonly Node Node;
		}

		readonly public int Index;
		readonly public BepuEntity PhysEntity;
		readonly public Node Node;
		readonly Matrix physToBone;
		readonly Matrix boneToPhys;
		readonly NodeIndex[] children;

		public RagdollBone( Scene scene, Node node, BepuEntity physEntity, params Node[] children )
		{
			if (scene==null		) throw new ArgumentNullException("scene");
			if (node==null		) throw new ArgumentNullException("node");
			if (physEntity==null) throw new ArgumentNullException("physEntity");

			Index		=	scene.Nodes.IndexOf(node);
			Node		=	node;
			PhysEntity	=	physEntity;

			foreach (var child in children)
			{
				if (child!=null && child.ParentIndex!=Index) throw new ArgumentException("Only direct children nodes are supported");
			}

			this.children	=	children
								.Where( ch1 => ch1 != null )
								.Select( ch2 => new NodeIndex( scene.Nodes.IndexOf(ch2), ch2 ) )
								.ToArray();

			var physTransform	=	MathConverter.Convert( physEntity.MotionState.WorldTransform );
			var bindPose		=	node.BindPose;

			physToBone			=	bindPose * Matrix.Invert( physTransform );
			boneToPhys			=	physTransform * Matrix.Invert( bindPose );
		}

		public Matrix GetPhysicsTransform()
		{
			return MathConverter.Convert( PhysEntity.WorldTransform );
		}

		public void ApplySimulatedTransform( Matrix invModelWorldTransform, Matrix[] destination )
		{
			var physTransform	=	MathConverter.Convert( PhysEntity.WorldTransform );
			destination[ Index ] = physToBone * physTransform * invModelWorldTransform;

			foreach ( var child in children )
			{
				destination[ child.Index ] = child.Node.Transform * destination[ Index ];
			}
		}


		public void LoadAnimatedTransforms( Matrix worldTransform, Vector3 velocity, Matrix[] source )
		{
			var physTransform = boneToPhys * source[ Index ] * worldTransform;

			PhysEntity.WorldTransform = MathConverter.Convert( physTransform );
			PhysEntity.LinearVelocity = MathConverter.Convert( velocity );
		}
	}
}
