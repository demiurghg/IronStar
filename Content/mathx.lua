
local mathx = {}

----------------------------------
--	mathx.drift  
----------------------------------
function mathx.drift ( current, target, velocity )
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

----------------------------------
--	mathx.smoothstep   
----------------------------------
function mathx.smoothstep(x)
	return x * x * (3 - 2 * x)
end

----------------------------------
return mathx;