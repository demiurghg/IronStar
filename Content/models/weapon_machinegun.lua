local model		= 	...
local entity	=	model.entity();

model.load("scenes\\weapon2\\assault_rifle\\assault_rifle_view.FBX")
model.setColor(255, 80, 20)
model.setIntensity(200)
model.setFpv(true, 0.01, "camera1")

local composer 	=	model.composer()

local track1	=	composer.addTrack("override", "root")
--model.addTrack
--model.setScale(0.01)

local old_traction = false

while true do

	local traction = entity.traction()
	
	if old_traction~=traction and traction then
		print("LANDING!")
	end
	old_traction = traction

	local vspeed = entity.vspeed();
	local gspeed = entity.gspeed();
	
	if math.abs(vspeed) > 0.01 then
		print('vspeed = ' .. vspeed );
	end
	
	if math.abs(gspeed) > 0.01 then
		print('gspeed = ' .. gspeed );
	end

	coroutine.yield()
end


--[[

local trackWeapon	=	model.newTrack { additive = false, channel = "root", weight = 1 }
local trackBarrel	=	model.newTrack { additive = true , channel = "root", weight = 1 }
local trackShake0	=	model.newTrack { additive = true , channel = "root", weight = 1 }
local trackShake1	=	model.newTrack { additive = true , channel = "root", weight = 1 }
local trackShake2	=	model.newTrack { additive = true , channel = "root", weight = 1 }
local trackShake3	=	model.newTrack { additive = true , channel = "root", weight = 1 }
local poseTilt		=	model.newPose  { additive = true , channel = "root", weight = 1 }
	  
trackWeapon.sequence{ 
	take	= "idle", 
	loop	= true, 
	xfade 	= 0,
}

while true do

	if entity.event.traction then
		model.playFx("player_landing")
		--runshake(
	end
		

	coroutine.yield()
end
]]
  
	  
-- {  
  -- "$type": "IronStar.SFX.ModelFactory, IronStar",
  -- "ScenePath": "scenes\\weapon2\\assault_rifle\\assault_rifle_view.FBX",
  -- "Scale": 0.01,
  -- "AnimController": "machinegun",
  -- "AnimEnabled": true,
  -- "Color": "255, 80, 20, 255",
  -- "Intensity": 200.0,
  -- "FPVEnable": true,
  -- "FPVCamera": "camera1",
  -- "Prefix": "anim_",
  -- "Clips": "",
  -- "BoxWidth": 1.0,
  -- "BoxHeight": 1.0,
  -- "BoxDepth": 1.0,
  -- "BoxColor": "154, 205, 50, 255"
-- }