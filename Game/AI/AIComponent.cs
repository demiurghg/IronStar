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
	public enum AIOptions
	{
		None	=	0x0000,
		Roaming	=	0x0001,
		Camper	=	0x0002,
		NoToken	=	0x0004,
	}

	public class AIComponent : IComponent
	{
		public AIOptions	Options;
		public Vector3		HoldPoint;

		//	general AI stuff :
		public Timer	ThinkTimer = new Timer();
		public DMNode	DMNode = DMNode.Stand;

		//	standing stuff :
		public Timer	StandTimer = new Timer();
		public Timer	CoverTimer = new Timer();

		//	movement stuff :
		public Route	Route	=	null;


		//	combat stuff :
		public AIToken		CombatToken = null;
		public bool			AllowFire = true;
		public bool			FocusTarget = true;
		public Timer		AttackTimer = new Timer();
		public Timer		GapeTimer = new Timer();
		public Timer		StunTimer = new Timer();
		public AITarget		Target = null;
		public readonly AITargetCollection	Targets =	new AITargetCollection();
		public Vector3		PrevAimError = Vector3.Zero;
		public Vector3		NextAimError = Vector3.Zero;

		public readonly List<EQPoint> CombatPoints = new List<EQPoint>();

		public AIComponent()
		{
		}


		public AIComponent( Vector3 origin, AIOptions options )
		{
			HoldPoint	=	origin;
			Options		=	options;
		}


		public void UpdateTimers( GameTime gameTime )
		{
			ThinkTimer.Update( gameTime );
			StandTimer.Update( gameTime );
			AttackTimer.Update( gameTime );
			GapeTimer.Update( gameTime );
			StunTimer.Update( gameTime );
			CoverTimer.Update( gameTime );
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
