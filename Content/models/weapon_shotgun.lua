local model		= 	...
local libwpn 	= 	require('libwpn')
local entity	=	model.get_entity();

local config = {
	muzzle_fx_name	=	"shotgunMuzzle",
	muzzle_fx_scale	=	0.20,
}

model.load			( "scenes\\weapon2\\canister_rifle\\canister_rifle_view" )
model.set_color		( 255, 80, 20 )
model.set_intensity	( 200.0 )
model.set_fpv		( true, 0.01, "camera1" )

libwpn.generic_weapon_animator( model, entity, config )
