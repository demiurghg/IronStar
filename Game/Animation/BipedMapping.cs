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

namespace IronStar.Animation
{
	enum BipedSide
	{
		Left, Right,
	}

	public sealed class BipedMapping
	{
		readonly Scene scene;

		/// <summary>
		/// Average ratio between limb's length and radius
		/// </summary>
		const float LimbRatio	=	2.7f;

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
			this.scene	=	scene;

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


		public bool TryGetLimbCapsule( Node rootLimb, Node nextLimb, out Matrix transform, out CapsuleShape capsule )
		{
			var origin = rootLimb.BindPose.TranslationVector;
			var dir    = nextLimb.BindPose.TranslationVector - origin;

			transform	=	MathUtil.ComputeAimedBasis( dir, origin + dir * 0.5f );
			var length	=	dir.Length();
			var radius	=	length / LimbRatio;

			capsule		=	new CapsuleShape( length, radius );

			return true;
		}


		public void DrawLimbCapsule( DebugRender dr, Node rootLimb, Node nextLimb )
		{
			CapsuleShape capsule;
			Matrix transform;

			if (TryGetLimbCapsule( rootLimb, nextLimb, out transform, out capsule ))
			{
				dr.DrawCapsule( capsule, transform, Color.Yellow );
			}
		}
	}
}
