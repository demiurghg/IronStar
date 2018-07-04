using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using Fusion.Core.Extensions;
using IronStar.Core;
using Fusion.Core;
using System.IO;
using BEPUphysics;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.EntityStateManagement;
using BEPUphysics.PositionUpdating;
using Fusion.Core.IniParser.Model;
using System.ComponentModel;
using IronStar.Entities;
using Fusion.Core.Shell;

namespace IronStar.Items {

	public partial class Weapon {


		readonly Random rand = new Random();


		public Weapon()
		{
		}


		[AECategory("Shooting")]
		public bool BeamWeapon { get; set; }
		
		[AECategory("Shooting")]
		public float BeamLength { get; set; }
		
		[AECategory("Shooting")]
		[AEClassname("entities")]
		public string Projectile { get; set; } = "";

		[AECategory("Shooting")]
		public int Damage { get; set; }

		[AECategory("Shooting")]
		public int ProjectileCount { 
			get { return projectileCount; }
			set { projectileCount = MathUtil.Clamp(value, 0, 100); }
		}
		int projectileCount = 1;

		[AECategory("Shooting")]
		public float AngularSpread { get; set; }

		[AECategory("Shooting")]
		public int Cooldown {
			get { return cooldown; }
			set { cooldown = MathUtil.Clamp(value, 0, 10000); }
		}
		int cooldown = 1;



		/// <summary>
		/// 
		/// </summary>
		public virtual bool Fire ( Entity attacker, GameWorld world )
		{
			var shooter = (IShooter)attacker;

			if (!shooter.TrySetCooldown( cooldown / 1000.0f )) {
				return false;
			}

			if (BeamWeapon) {

				//	...
				return true;

			} else {

				for (int i=0; i<ProjectileCount; i++) {
					FireProjectile( attacker, shooter, world );
				}

				return true;
			}
		}



		/// <summary>
		/// Fires projectile
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="world"></param>
		/// <param name="origin"></param>
		void FireProjectile ( Entity attacker, IShooter shooter, GameWorld world )
		{
			var e = world.Spawn( Projectile );

			if (e==null) {
				Log.Warning("Unknown class: {0}", Projectile);
			}

			var p = shooter.GetWeaponPOV();
			var q = attacker.Rotation;
			var d = GetFireDirection(q);

			e.ParentID	=	attacker.ID;
			e.Teleport( p, q );
			(e as Projectile)?.FixServerLag(1/60.0f);

			//world.SpawnFX( "MZBlaster",	attacker.ID, origin );
		}



		/// <summary>
		/// Gets firing direction
		/// </summary>
		/// <param name="rotation"></param>
		/// <returns></returns>
		Vector3 GetFireDirection ( Quaternion rotation )
		{ 
			var spreadVector	= GetSpreadVector( AngularSpread );
			var rotationMatrix	= Matrix.RotationQuaternion( rotation );
			return Vector3.TransformNormal( spreadVector, rotationMatrix );
		}



		/// <summary>
		/// Gets radial spread vector
		/// </summary>
		/// <param name="spreadAngle"></param>
		/// <returns></returns>
		Vector3 GetSpreadVector ( float spreadAngle )
		{
			var randomAngle	 = MathUtil.DegreesToRadians( spreadAngle );

			var theta	=	rand.NextFloat( 0, MathUtil.TwoPi );
			var tMin	=	(float)Math.Cos( randomAngle );
			var t		=	rand.NextFloat( tMin, 1 );
			var phi		=	(float)Math.Acos( t );
			var sinPhi	=	(float)Math.Sin( phi );

			var x		=	(float)( Math.Sin( phi ) * Math.Cos( theta ) );
			var y		=	(float)( Math.Sin( phi ) * Math.Sin( theta ) );
			var z		=	-(float)( Math.Cos( phi ) ); // because forward is -Z

			return new Vector3( x, y, -z );
		}
	}


}

