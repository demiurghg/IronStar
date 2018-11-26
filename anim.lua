

local model = arg[0].model
local entity = arg[0].entity

model.load("scenes\marine")

model.scale 		= 	0.125
model.glowColor 	= 	color(192,126,12)
model.glowIntensity	=	500

while true do
	model.glowIntensity	=	math.random();
	data = coroutine.yield{ wait = 100 }
end


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


track1 = animator.add_track("root", "additive")
track2 = animator.add_track("root", "additive")

track.sequence(ANIM_IDLE, true, true)



function initializeAnimator ()
	
end


function updateAnimator ()
	
end