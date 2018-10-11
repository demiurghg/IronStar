
local lib = {}

local counter = 0;

function lib.foo()
	--print('FOO : ' .. counter)
	counter = counter + 1;
end

return lib;