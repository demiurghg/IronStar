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
-- local track_barrel	=	composer.add_track ( "override", nil )
-- local track_shake0	=	composer.add_track ( "additive", nil )
-- local track_shake1	=	composer.add_track ( "additive", nil )
-- local track_shake2	=	composer.add_track ( "additive", nil )
-- local track_shake3	=	composer.add_track ( "additive", nil )
-- local pose_tilt		=	composer.add_pose  ( "additive", nil, ANIM_TILT )

track_weapon.sequence {
	take = ANIM_IDLE,
	loop = true,
}

local old_traction = false

while true do

	--[[ local traction = false;---entity.has_traction()
	
	if old_traction~=traction and traction then
		print("LANDING!")
	end
	old_traction = traction

	local vspeed = entity.get_vspeed();
	local gspeed = entity.get_gspeed();
	
	if math.abs(vspeed) > 0.01 then
		print('vspeed = ' .. vspeed );
	end
	
	if math.abs(gspeed) > 0.01 then
		print('gspeed = ' .. gspeed );
	end
	]]

	coroutine.yield()
end
