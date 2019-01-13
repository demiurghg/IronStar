local model		= ...
model.load("scenes\\items\\armor\\armor_lo.fbx")
model.set_color(255, 112, 8)
model.set_intensity(100)
model.set_scale(0.3)

local bright = true

model.sleep( math.random(0,100) )

coroutine.yield()

while true do
	if bright then
		model.set_intensity( 1000 )
	else
		model.set_intensity( 100 )
	end
	
	bright = not bright;
	
	model.sleep(math.random(100,120));
	coroutine.yield()
end