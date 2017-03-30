﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Content;
using Fusion.Engine.Server;
using Fusion.Engine.Client;
using Fusion.Core.Extensions;
using IronStar.SFX;
using Fusion.Core.IniParser.Model;
using Fusion.Engine.Graphics;
using IronStar.Entities;

namespace IronStar.Core {

	/// <summary>
	/// World represents entire game state.
	/// </summary>
	public partial class GameWorld {


		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool IsPlayer( uint id )
		{
			Entity e;

			if ( entities.TryGetValue( id, out e ) ) {
				return e.UserGuid == UserGuid;
			} else {
				return false;
			}
		}



		/// <summary>
		/// Check whether entity with id is dead.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool IsAlive ( uint id )
		{
			return entities.ContainsKey( id );
		}



		/// <summary>
		/// Get list of target entities.
		/// </summary>
		/// <param name="targetName"></param>
		/// <returns></returns>
		public IEnumerable<Entity> GetTargets ( string targetName )
		{
			if (string.IsNullOrWhiteSpace(targetName)) {
				return new Entity[0];
			}

			return GetEntities()
				.Where( e => e.TargetName == targetName )
				.ToArray();
		}



		/// <summary>
		/// Activates given targets
		/// </summary>
		/// <param name="targetName"></param>
		public void ActivateTargets ( Entity activator, string targetName )
		{
			var targets = GetTargets( targetName );
			foreach ( var target in targets ) {
				target.Controller?.Activate( activator );
			}
		}



		/// <summary>
		/// Attempts to use something by user-entity
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		public bool TryUse ( Entity user )
		{
			var character = user.Controller as Character;

			if (character==null) {
				Log.Warning("TryUse: user is not a character!");
				return false;
			}

			var dr = Game.RenderSystem.RenderWorld.Debug;

			foreach ( var ent in GetEntities().Where( e=>e.Controller.AllowUse ) ) {
				
				var from	=	character.GetPOV();
				var dir		=	Matrix.RotationQuaternion( user.Rotation ).Forward;
				var to		=	from + dir * 2.0f;

				Vector3 n, p;
				Entity e;
				
				var r = RayCastAgainstAll( from, to, out n, out p, out e, user );

				if (!r || e==null) {
					return false;
				}

				Log.Verbose("try use: {0}", e.Controller.GetType().Name);

				return e.Controller.Use( user );
			}	

			return false;
		}



		/// <summary>
		/// Gets entity with current id.
		/// If entity does not exist returns null
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Entity GetEntity ( uint id )
		{
			Entity e;
			if (entities.TryGetValue( id, out e )) {
				return e;
			} else {
				return null;
			}
		}


		public IEnumerable<Entity> GetEntities ()
		{
			return entities.Select( pair => pair.Value ).OrderBy( e => e.ID );
		}



		/// <summary>
		/// Gets entity with current id.
		/// If entity is dead -> exception...
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[Obsolete]
		public Entity GetEntityOrNull( string classname, Func<Entity, bool> predicate )
		{
			return GetEntities( classname ).FirstOrDefault( ent => predicate( ent ) );
		}


		/// <summary>
		/// Gets entity with current id.
		/// If entity is dead -> exception...
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[Obsolete]
		public Entity GetEntityOrNull( string classname )
		{
			return GetEntities( classname ).FirstOrDefault();
		}




		/// <summary>
		/// Gets entity with current id.
		/// If entity is dead -> exception...
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[Obsolete]
		public IEnumerable<Entity> GetEntities ( string classname )
		{
			var classId = Atoms[classname];
			return entities.Where( pair => pair.Value.ClassID==classId ).Select( pair1 => pair1.Value );
		}


		/// <summary>
		/// Performs action on each entity.
		/// </summary>
		/// <param name="action"></param>
		public void ForEachEntity ( Action<Entity> action )
		{
			var list = entities.Select( p => p.Value ).ToList();

			foreach ( var e in list ) {
				action( e );
			}
		}
	}
}
