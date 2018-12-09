using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpaceMarines.Core;

namespace SpaceMarines.Entities {
	public class MonsterFactory : EntityFactory {

		public int		max_health			=	100;
		public string	weapon				=	"WEAPON2";
		public int		team				=	1;

		public float	pic_size			=	2.0f;
		public string	body_pic			=	"sprites/actors/marine_body.tga";
		public string	body_shadow			=	"sprites/actors/marine_body_shadow.tga";
		public string	legs_pic			=	"sprites/actors/marine_legs.tga";
		public string	legs_shadow			=	"sprites/actors/marine_legs_shadow.tga";
		public float	gun_offset			=	0.45f;
		public float	shadow_offset		=	0.2f;
		public string	pain_gfx			=	"BLOOD_SPATTER";
		public string	pain_sound			=	"";
		public string	death_gfx			=	"MEAT_FLESH2";
		public string	death_sound			=	"sound/voices/human_dying.ogg";
	
		public int		max_shield			=	250		;
		public float	shield_regen_time	=	0.1f	;
		public string	shield_gfx  		=	"SHIELD";
	
		public float	size				=	1.5f	;
		public float	linear_speed		=	15		;
		public float	linear_accel		=	120		;
		public float	angular_speed		=	1000	;

		public float	ai_think_quantum	=	100		;
		public float	ai_aim_speed		=	400		;	
		public float	ai_aim_deviation	=	0.999f	;	
		public float	ai_attack_dist		=	40		;	
		public float	ai_smell_dist		=	10		;
		public float	ai_follow_dist		=	20		;  
		public float	ai_patrol_area		=	20		;
		

		public override Entity Spawn( uint id, GameWorld world )
		{
			return new Monster( id, world, this );
		}
	}
}
