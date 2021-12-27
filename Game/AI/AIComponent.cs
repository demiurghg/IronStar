using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using IronStar.ECS;

namespace IronStar.AI
{
	public class AIComponent : IComponent
	{
		//	general AI stuff :
		public Timer	ThinkTimer;
		public DMNode	DMNode = DMNode.Stand;

		//	standing stuff :
		public Timer	StandTimer;

		//	movement stuff :
		public Route	Route	=	null;


		//	combat stuff :
		public bool			AllowFire = true;
		public Timer		AttackTimer;
		public Timer		GapeTimer;
		public AITarget		Target = null;
		public readonly List<AITarget>	Targets =	new List<AITarget>();
		public Vector3		PrevAimError = Vector3.Zero;
		public Vector3		NextAimError = Vector3.Zero;


		public void UpdateTimers( GameTime gameTime )
		{
			ThinkTimer.Update( gameTime );
			StandTimer.Update( gameTime );
			AttackTimer.Update( gameTime );
			GapeTimer.Update( gameTime );
		}


		public IComponent Clone()
		{
			return (IComponent)MemberwiseClone();
		}

		public IComponent Interpolate( IComponent previous, float dt, float factor )
		{
			return Clone();
		}

		public void Save( GameState gs, BinaryWriter writer )
		{
		}

		public void Load( GameState gs, BinaryReader reader )
		{
		}
	}
}
