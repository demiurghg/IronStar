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
using IronStar.ECSPhysics;

namespace IronStar.Animation
{
	enum BipedSide
	{
		Left, Right,
	}

	public sealed class BipedMapping
	{
		readonly Scene scene;

		//	 0:                                  -1  
		//	 1:    camera1                        0  
		//	 2:    persp1                         0  
		//	 3:    marine                         0  mesh #0
		//	 4:    base                           0  
		//	 5:      pelvis                       4  
		//	 6:        spine1                     5  
		//	 7:          spine2                   6  
		//	 8:            chest                  7  
		//	 9:              head                 8  
		//	10:              L_shoulder           8  
		//	11:                L_arm             10  
		//	12:                  L_hand          11  
		//	13:              R_shoulder           8  
		//	14:                R_arm             13  
		//	15:                  R_hand          14  
		//	16:        L_hip                      5  
		//	17:          L_shin                  16  
		//	18:            L_foot                17  
		//	19:              L_toe               18  
		//	20:        R_hip                      5  
		//	21:          R_shin                  20  
		//	22:            R_foot                21  
		//	23:              R_toe               22  

		public readonly Node	Base;
		public readonly Node	Pelvis;
		public readonly Node	  Spine1;
		public readonly Node	  Spine2;
		public readonly Node	  Spine3;
		public readonly Node	  Spine4;
		public readonly Node	  Spine5;
		public readonly Node	    Chest;
		public readonly Node	      Head;
		public readonly Node	      LeftShoulder;
		public readonly Node	        LeftArm;
		public readonly Node	          LeftHand;
		public readonly Node	      RightShoulder;
		public readonly Node	        RightArm;
		public readonly Node	          RightHand;
		public readonly Node	  LeftHip;
		public readonly Node	    LeftShin;
		public readonly Node	      LeftFoot;
		public readonly Node	        LeftToe;
		public readonly Node	  RightHip;
		public readonly Node	    RightShin;
		public readonly Node	      RightFoot;
		public readonly Node	        RightToe;


		public BipedMapping( Scene scene )
		{
			this.scene		=	scene;

			Base			=	FindNode( "base"	);
			Pelvis			=	FindNode( "pelvis"	);
			Spine1			=	FindNode( "spine1"	);
			Spine2			=	FindNode( "spine2"	);
			Spine3			=	FindNode( "spine3"	);
			Spine4			=	FindNode( "spine4"	);
			Spine5			=	FindNode( "spine5"	);
			Chest			=	FindNode( "chest"	);
			Head			=	FindNode( "head"	);

			LeftShoulder	=	FindNode( BipedSide.Left,  "shoulder"	);
			LeftArm			=	FindNode( BipedSide.Left,  "arm"		);
			LeftHand		=	FindNode( BipedSide.Left,  "hand"		);
			RightShoulder	=	FindNode( BipedSide.Right, "shoulder"	);
			RightArm		=	FindNode( BipedSide.Right, "arm"		);
			RightHand		=	FindNode( BipedSide.Right, "hand"		);
			
			LeftHip			=	FindNode( BipedSide.Left,  "hip"		);
			LeftShin		=	FindNode( BipedSide.Left,  "shin"		);
			LeftFoot		=	FindNode( BipedSide.Left,  "foot"		);
			LeftToe			=	FindNode( BipedSide.Left,  "toe"		);
			RightHip		=	FindNode( BipedSide.Right, "hip"		);
			RightShin		=	FindNode( BipedSide.Right, "shin"		);
			RightFoot		=	FindNode( BipedSide.Right, "foot"		);
			RightToe		=	FindNode( BipedSide.Right, "toe"		);
		}


		Node FindNode( BipedSide side, string name )
		{
			var prefix1 = side==BipedSide.Left ? "L"    : "R"		;
			var prefix2 = side==BipedSide.Left ? "L_"   : "R_"		;
			var prefix3 = side==BipedSide.Left ? "Left" : "Right"	;
			return FindNode( prefix1 + name, prefix2 + name, prefix3 + name );
		}


		Node FindNode( params string[] names )
		{
			for (int i=0; i<scene.Nodes.Count; i++)
			{
				for (int j=0; j<names.Length; j++)
				{
					if (string.Equals(names[j], scene.Nodes[i].Name, StringComparison.OrdinalIgnoreCase))
					{
						return scene.Nodes[i];
					}
				}
			}

			return null;
		}


		public RagdollBone GetLimbCapsule( Node rootLimb, Node nextLimb, float radius )
		{
			//	#TODO #RAGDOLL -- ged limb radius from predefined body shape: bulky, skinny, male, female
			//	capsule is aligned along Y axis
			//	Z-axis of capsule looks backward
			var start		=	rootLimb.BindPose.TranslationVector;
			var end			=	nextLimb.BindPose.TranslationVector;
			var translation	=	0.5f * ( start + end );

			var axisY		=	(end - start).Normalized();
			var axisZ		=	Vector3.ForwardRH;
			var axisX		=	Vector3.Cross( axisY, axisZ ).Normalized();
				axisZ		=	Vector3.Cross( axisX, axisY ).Normalized();

			var basis		=	new Matrix();
			basis.Right		=	axisX;
			basis.Up		=	axisY;
			basis.Backward	=	axisZ;
			var rotation	=	Quaternion.RotationMatrix( basis );

			var dir			=	end - start;
				dir.Normalize();

			var length		=	Vector3.Distance( start, end );
				length		=	Math.Max(0, length - 2 * radius );

			var ms	=	new MotionState();
			ms.Orientation		=	MathConverter.Convert( rotation );
			ms.Position			=	MathConverter.Convert( translation );
			ms.LinearVelocity	=	BEPUutilities.Vector3.Zero;
			ms.AngularVelocity	=	BEPUutilities.Vector3.Zero;

			var capsule		=	new Capsule( ms, length, radius, 5 );
			capsule.PositionUpdateMode	=	PositionUpdateMode.Continuous;
			capsule.Material.KineticFriction *= 0.5f;
			capsule.Material.StaticFriction *= 0.5f;

			return new RagdollBone( scene.Nodes.IndexOf(rootLimb), rootLimb, capsule );
		}


		public RagdollBone GetBoneBox( Node node1, Node node2, float mass )
		{
			if (node1==null) return null;

			var bbox = FitBBox(node1, node2);
			var pos  = MathConverter.Convert( bbox.Center() );

			var box  = new Box( pos, bbox.Size().X * 0.7f, bbox.Size().Y * 0.7f, bbox.Size().Z * 0.7f, mass );
			box.PositionUpdateMode	=	PositionUpdateMode.Continuous;
			box.Material.KineticFriction *= 0.5f;
			box.Material.StaticFriction *= 0.5f;

			return new RagdollBone( scene.Nodes.IndexOf(node1), node1, box );
		}


		bool AcceptVertex( MeshVertex vertex, int index, float threshold = 0.5f )
		{
			if (index<0) return false;
			if (vertex.SkinIndices.X==index && vertex.SkinWeights.X > threshold) return true;
			if (vertex.SkinIndices.Y==index && vertex.SkinWeights.Y > threshold) return true;
			if (vertex.SkinIndices.Z==index && vertex.SkinWeights.Z > threshold) return true;
			if (vertex.SkinIndices.W==index && vertex.SkinWeights.W > threshold) return true;
			return false;
		}


		BoundingBox FitBBox( Node node1, Node node2 = null )
		{
			var points = new List<Vector3>();
			var index1 = scene.Nodes.IndexOf(node1);
			var index2 = scene.Nodes.IndexOf(node2);

			foreach ( var mesh in scene.Meshes )
			{
				foreach ( var vertex in mesh.Vertices )
				{
					if (AcceptVertex(vertex, index1) || AcceptVertex(vertex, index2))
					{
						points.Add( vertex.Position );
					}
				}
			}

			return BoundingBox.FromPoints( points );
		}
	}
}
