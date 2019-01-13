local model		= ...
model.load("scenes\\items\\medkit\\medkit10.fbx")
model.set_color(48, 96, 255)
model.set_intensity(3000)
model.set_scale(0.3)

local bright = true

model.sleep( math.random(0,100) )

coroutine.yield()

while true do
	if bright then
		model.set_intensity( 2000 )
	else
		model.set_intensity( 200 )
	end
	
	bright = not bright;
	
	model.sleep(math.random(100,120));
	coroutine.yield()
end