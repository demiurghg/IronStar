local model		= 	...
local entity	=	model.get_entity();

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

function drift ( current, target, velocity )
	if current==target then return target end

	-- go down :
	if current>target then
		if current>target+velocity then
			return current - velocity;
		else
			return target;
		end
	end

	-- go up:
	if current<target then
		if current<target-velocity then
			return current + velocity;
		else
			return target;
		end
	end

	return current;
end

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
			print("**** NO AMMO ****");
		end
	
	end

	---- landing and jumping ----
	
	if old_traction~=traction then
		if traction then
			local weight = math.min( 0.5, math.abs( old_vspeed / 20 ) );
			track_shake0.sequence {	take = ANIM_LANDING, crossfade = 0	}
			track_shake0.set_weight( weight )
			print( weight );
		else
			track_shake1.sequence {	take = ANIM_JUMP	}
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
	
	tilt_factor	= drift( tilt_factor, tilt_target, dtime * 2 );

	if tilt_factor > 0 then
		pose_tilt.set_frame( 1 )
		pose_tilt.set_weight( math.abs(tilt_factor) )
	elseif tilt_factor < 0 then
		pose_tilt.set_frame( 2 )
		pose_tilt.set_weight( math.abs(tilt_factor) )
	else
		pose_tilt.set_frame( 0 )
		pose_tilt.set_weight( 0 )
	end
	-- if math.abs(vspeed) > 0.01 then
		-- print('vspeed = ' .. vspeed );
	-- end
	
	-- if math.abs(gspeed) > 0.01 then
		-- print('gspeed = ' .. gspeed );
	-- end
	
	-- store values for future use :
	old_traction 	=	traction
	old_gspeed		=	gspeed;
	old_vspeed		=	vspeed;
	old_state		=	state;

	coroutine.yield()
end
