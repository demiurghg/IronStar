﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Common;
using Fusion.Core.Mathematics;


namespace IronStar.Core {
	public abstract class EntityController {

		public readonly Game Game;
		public readonly GameWorld World;
		public readonly Entity Entity;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="world"></param>
		/// <param name="entity"></param>
		public EntityController ( Entity entity, GameWorld world )
		{
			this.World	=	world;
			this.Entity	=	entity;
			this.Game	=	world.Game;
		}



		/// <summary>
		/// Reset controller: reset internal counters, targets etc.
		/// Read position from entity.
		/// For editor use only.
		/// </summary>
		public virtual void Reset ()
		{
			
		}


		/// <summary>
		/// Called when user try to do action
		/// </summary>
		/// <param name="action"></param>
		public virtual void Action ( UserAction action )
		{
		}



		/// <summary>
		/// Called when user try to do action
		/// </summary>
		/// <param name="action"></param>
		public virtual void CancelAction ( UserAction action )
		{
		}



		/// <summary>
		/// Called when user moves
		/// </summary>
		/// <param name="forward"></param>
		/// <param name="right"></param>
		/// <param name="up"></param>
		public virtual void Move ( float forward, float right, float up )
		{
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="activator">Entity that activates given entity. Could be NULL</param>
		public virtual void Activate ( Entity activator )
		{
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="user"></param>
		public virtual bool Use ( Entity user )
		{
			return false;
		}



		/// <summary>
		/// Updates controller.
		/// </summary>
		/// <param name="gameTime"></param>
		public virtual void Update ( float elapsedTime ) 
		{
		}



		/// <summary>
		/// Indicates whether given entity could be used by player
		/// </summary>
		public virtual bool AllowUse { get { return false; } }



		/// <summary>
		/// 
		/// </summary>
		/// <param name="damage"></param>
		/// <param name="kickImpulse"></param>
		/// <param name="kickPoint"></param>
		/// <param name="damageType"></param>
		public virtual bool Damage ( uint targetID, uint attackerID, short damage, Vector3 kickImpulse, Vector3 kickPoint, DamageType damageType )
		{
			return false;
		}


		/// <summary>
		/// Called when entity has died.
		/// </summary>
		/// <param name="id"></param>
		public virtual void Killed () 
		{
		}
	}
}
