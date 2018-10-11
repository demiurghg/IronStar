local model		= 	...
local entity	=	model.get_entity();

local libweapon = 	require('libweapon')
local mathx		=	require('mathx')

-----------------------------------------------------------

local ANIM_TILT			=	"tilt"
local ANIM_IDLE			=	"idle"			
local ANIM_WARMUP		=	"warmup"		
local ANIM_COOLDOWN		=	"cooldown"		
local ANIM_LANDING		=	"landing"		
local ANIM_JUMP			=	"jump"			
local ANIM_SHAKE		=	"shake"			
local ANIM_WALKLEFT		=	"step_left"		
local ANIM_WALKRIGHT	=	"step_right"	
local ANIM_FIRSTLOOK	=	"examine"		
local ANIM_RAISE		=	"raise"			
local ANIM_DROP			=	"drop"			

local SOUND_LANDING		=	"player/landing"
local SOUND_STEP		=	"player/step"	
local SOUND_JUMP		=	"player/jump"	
local SOUND_NO_AMMO		=	"weapon/noAmmo"	

local JOINT_MUZZLE		=	"muzzle"		

-----------------------------------------------------------

model.load			( "scenes\\weapon2\\assault_rifle\\assault_rifle_view.FBX" )
model.set_color		( 255, 80, 20 )
model.set_intensity	( 200 )
model.set_fpv		( true, 0.01, "camera1" )

content.dofile('models\\test')

local composer 		=	model.get_composer()

local track_weapon	=	composer.add_track ( "override", nil )
local track_barrel	=	composer.add_track ( "override", nil )
local track_shake0	=	composer.add_track ( "additive", nil )
local track_shake1	=	composer.add_track ( "additive", nil )
local track_shake2	=	composer.add_track ( "additive", nil )
local track_shake3	=	composer.add_track ( "additive", nil )
local pose_tilt		=	composer.add_pose  ( "additive", nil, ANIM_TILT )

track_weapon.sequence {
	take = ANIM_IDLE,
	loop = true,
}

local old_traction 	= false
local old_vspeed	= 0
local old_gspeed 	= 0
local tilt_factor	= 0
local old_state		= ""
local step_timer	= -0.05
local step_left		= false

while true do

	local traction 	= entity.has_traction();
	local vspeed 	= entity.get_vspeed();
	local gspeed 	= entity.get_gspeed();
	local dtime		= model.get_dt();
	
	---- weapon operation ----
	
	local state		= entity.get_weapon_state();
	
	if old_state~=state then
	
		if state=="cooldown" or state=="cooldown2" then
			track_weapon.sequence { take=ANIM_COOLDOWN, crossfade=0 }
			composer.play_fx ("machinegunMuzzle", JOINT_MUZZLE, 0.13 )
		end
		
		if state=="idle" then
			track_weapon.sequence { take=ANIM_IDLE, loop=true }
		end
	
		if state=="raise" then
			track_weapon.sequence { take=ANIM_RAISE, crossfade=0 }
		end
	
		if state=="drop" then
			track_weapon.sequence { take=ANIM_DROP, crossfade=0 }
		end
	
		if state=="noammo" then
			composer.play_sound(SOUND_NO_AMMO)
		end
	
	end

	---- landing and jumping ----
	
	if old_traction~=traction then
		if traction then
			local weight = 0.7 * math.min( 0.5, math.abs( old_vspeed / 20 ) );
			track_shake0.sequence {	take = ANIM_LANDING, crossfade = 0	}
			track_shake0.set_weight( weight )
			composer.play_sound(SOUND_LANDING)
		else
			track_shake1.sequence {	take = ANIM_JUMP	}
			track_shake1.set_weight( 0.7 )
			composer.play_sound(SOUND_JUMP)
		end
	end
	
	---- strafing and turning ----
	
	local tilt_target = 0
	if entity.is_strafe_right() then  tilt_target = tilt_target + 1; end
	if entity.is_strafe_left () then  tilt_target = tilt_target - 1; end
	if entity.is_turn_right  () then  tilt_target = tilt_target + 1; end
	if entity.is_turn_left   () then  tilt_target = tilt_target - 1; end
	
	tilt_target = math.min( tilt_target,  1 )
	tilt_target = math.max( tilt_target, -1 )
	
	tilt_factor	= mathx.drift( tilt_factor, tilt_target, dtime*2 );

	if tilt_factor > 0 then
		pose_tilt.set_frame( 1 )
		pose_tilt.set_weight( mathx.smoothstep(math.abs(tilt_factor)) )
	elseif tilt_factor < 0 then
		pose_tilt.set_frame( 2 )
		pose_tilt.set_weight( mathx.smoothstep(math.abs(tilt_factor)) )
	else
		pose_tilt.set_frame( 0 )
		pose_tilt.set_weight( 0 )
	end
	
	---- steps ----
	
	if traction and gspeed > 0.1 then
	
		step_timer = step_timer + dtime;
	
		local weight = math.min( 1, gspeed / 10 ) * 0.5
		
		if step_timer > 0.3 then
			step_left = not step_left;
			step_timer = 0
			
			if step_left then
				composer.play_sound( SOUND_STEP )
				track_shake2.sequence { take=ANIM_WALKLEFT, crossfade=0 }
				track_shake2.set_weight( weight )
			else
				composer.play_sound( SOUND_STEP )
				track_shake3.sequence { take=ANIM_WALKRIGHT, crossfade=0 }
				track_shake3.set_weight( weight )
			end
		end
	
	else
		step_timer = -0.01
	end
	
	-- store values for future use :
	old_traction 	=	traction
	old_gspeed		=	gspeed;
	old_vspeed		=	vspeed;
	old_state		=	state;

	coroutine.yield()
end
