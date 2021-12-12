using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using Fusion.Engine.Graphics.Scenes;
using IronStar.Animation;

namespace IronStar.ECSPhysics
{
	public class RagdollController
	{
		readonly PhysicsCore physics;
		readonly Scene scene;
		readonly BipedMapping mapping;

		Matrix[] bindPose;



		public RagdollController( PhysicsCore physics, Scene scene )
		{
			this.physics	=	physics;	
			this.scene		=	scene;
			mapping			=	new BipedMapping(scene);

			bindPose		=	scene.GetBindPose();
		}


		public void DrawDebug( DebugRender dr )
		{
			foreach ( var bt in bindPose )
			{
				dr.DrawBasis( bt, 0.3f, 1 );
			}

			mapping.DrawLimbCapsule( dr, mapping.LeftShoulder,	mapping.LeftArm		);
			mapping.DrawLimbCapsule( dr, mapping.RightShoulder,	mapping.RightArm	);

			mapping.DrawLimbCapsule( dr, mapping.LeftArm,		mapping.LeftHand	);
			mapping.DrawLimbCapsule( dr, mapping.RightArm,		mapping.RightHand	);

			mapping.DrawLimbCapsule( dr, mapping.LeftHip,		mapping.LeftShin	);
			mapping.DrawLimbCapsule( dr, mapping.LeftShin,		mapping.LeftFoot	);
			mapping.DrawLimbCapsule( dr, mapping.LeftFoot,		mapping.LeftToe		);

			mapping.DrawLimbCapsule( dr, mapping.RightHip,		mapping.RightShin	);
			mapping.DrawLimbCapsule( dr, mapping.RightShin,		mapping.RightFoot	);
			mapping.DrawLimbCapsule( dr, mapping.RightFoot,		mapping.RightToe	);
		}


		public void Destroy()
		{
			
		}
	}
}
