print("TEST")
--error('FUCK')

function foo ()
	for i=0,10 do
		--print("TEST " .. i)
		--coroutine.yield()
	end
end