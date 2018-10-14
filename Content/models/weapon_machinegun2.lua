local model		= 	...
local libwpn 	= 	require('libwpn')
local entity	=	model.get_entity();

local config = {
	-- FIXME : should be: machinegunMuzzle2!!!
	muzzle_fx_name	=	"machinegunMuzzle",
	muzzle_fx_scale	=	0.13,
}

model.load			( "scenes\\weapon2\\battle_rifle\\battle_rifle_view" )
model.set_color		( 255, 80, 20 )
model.set_intensity	( 200 )
model.set_fpv		( true, 0.01, "camera1" )

libwpn.generic_weapon_animator( model, entity, config )
