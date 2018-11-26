local model		= 	...
local libwpn 	= 	require('libwpn')
local entity	=	model.get_entity();

local config = {
	muzzle_fx_name	=	"rocketMuzzle",
	muzzle_fx_scale	=	0.20,
}

model.load			( "scenes\\weapon2\\rocket_launcher\\rocket_launcher_view" )
model.set_color		( 255, 24, 16 )
model.set_intensity	( 100.0 )
model.set_fpv		( true, 0.0125, "camera1" )

local timer = 0;

function func(dtime)
	model.set_intensity( math.random(100,200) + 50*math.sin(timer*math.pi*3) );
end

libwpn.generic_weapon_animator( model, entity, config, func )
