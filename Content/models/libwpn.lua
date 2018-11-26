local lib = {}
--------------------------------------------------------------------------------

local mathx 	= 	require('mathx')

function lib.generic_weapon_animator ( model, entity, config, func )

	local anim_tilt			=	"tilt"
	local anim_idle			=	"idle"			
	local anim_warmup		=	"warmup"		
	local anim_cooldown		=	"cooldown"		
	local anim_landing		=	"landing"		
	local anim_jump			=	"jump"			
	local anim_shake		=	"shake"			
	local anim_walkleft		=	"step_left"		
	local anim_walkright	=	"step_right"	
	local anim_firstlook	=	"examine"		
	local anim_raise		=	"raise"			
	local anim_drop			=	"drop"			

	local sound_landing		=	"player/landing"
	local sound_step		=	"player/step"	
	local sound_jump		=	"player/jump"	
	local sound_no_ammo		=	"weapon/noammo"	

	local muzzle_joint		=	"muzzle"	

	local muzzle_fx_name	=	config.muzzle_fx_name	or "";
	local muzzle_fx_scale	=	config.muzzle_fx_scale  or 0.1;

	-----------------------------------------------------------

	local composer 		=	model.get_composer()

	local track_weapon	=	composer.add_track ( "override", nil )
	local track_barrel	=	composer.add_track ( "override", nil )
	local track_shake0	=	composer.add_track ( "additive", nil )
	local track_shake1	=	composer.add_track ( "additive", nil )
	local track_shake2	=	composer.add_track ( "additive", nil )
	local track_shake3	=	composer.add_track ( "additive", nil )
	local pose_tilt		=	composer.add_pose  ( "additive", nil, anim_tilt )

	track_weapon.sequence {
		take = anim_idle,
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

		if func then func(dtime) end;
		
		---- weapon operation ----
		
		local state		= entity.get_weapon_state();
		
		if old_state~=state then
		
			if state=="cooldown" or state=="cooldown2" then
				track_weapon.sequence { take=anim_cooldown, crossfade=0 }
				composer.play_fx ( muzzle_fx_name, muzzle_joint, muzzle_fx_scale )
			end
			
			if state=="idle" then
				track_weapon.sequence { take=anim_idle, loop=true }
			end
		
			if state=="raise" then
				track_weapon.sequence { take=anim_raise, crossfade=0 }
			end
		
			if state=="drop" then
				track_weapon.sequence { take=anim_drop, crossfade=0 }
			end
		
			if state=="noammo" then
				composer.play_sound(sound_no_ammo)
			end
		
		end

		---- landing and jumping ----
		
		if old_traction~=traction then
			if traction then
				local weight = 0.7 * math.min( 0.5, math.abs( old_vspeed / 20 ) );
				track_shake0.sequence {	take = anim_landing, crossfade = 0	}
				track_shake0.set_weight( weight )
				composer.play_sound(sound_landing)
			else
				track_shake1.sequence {	take = anim_jump	}
				track_shake1.set_weight( 0.7 )
				composer.play_sound(sound_jump)
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
					composer.play_sound( sound_step )
					track_shake2.sequence { take=anim_walkleft, crossfade=0 }
					track_shake2.set_weight( weight )
				else
					composer.play_sound( sound_step )
					track_shake3.sequence { take=anim_walkright, crossfade=0 }
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
end

--------------------------------------------------------------------------------
return lib;
