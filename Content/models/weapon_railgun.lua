local model		= 	...
local libwpn 	= 	require('libwpn')
local entity	=	model.get_entity();

local config = {
	muzzle_fx_name	=	"railMuzzle",
	muzzle_fx_scale	=	0.20,
}

model.load			( "scenes\\weapon2\\gauss_rifle\\gauss_rifle_view" )
model.set_color		( 107, 136, 255 )
model.set_intensity	( 200.0 )
model.set_fpv		( true, 0.01, "camera1" )

local timer = 2000;
local old_state = "idle"

function func(dtime)
	local state = entity.get_weapon_state()
	timer = timer + dtime;
	if old_state ~= state then
		old_state = state;
		if state=="cooldown" or state=="cooldown2" then
			timer = 0;
		end
		if state=="noammo" then
			timer = 0;
			model.set_color	( 255, 2, 2 )
		end
	end
	local factor = math.exp(-timer);
	model.set_intensity( 10 + 5000 * factor );
end

libwpn.generic_weapon_animator( model, entity, config, func )
