local model		= 	...
local libwpn 	= 	require('libwpn')
local entity	=	model.get_entity();

model.load			( "scenes\\weapon2\\assault_rifle\\assault_rifle_view_import_clips.FBX" )
model.set_color		( 255, 80, 20 )
model.set_intensity	( 200 )
model.set_fpv		( true, 0.01, "camera1" )

libwpn.generic_weapon_animator( model, entity )
