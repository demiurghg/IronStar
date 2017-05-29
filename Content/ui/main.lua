

local root = ui.root()

print("width:"..root.width.."  height:"..root.height);

root.removeAll()	

local frame = ui.new(10,10, 100, 100, "Test1", "#80000000")
root.add( frame )

local frame = ui.new(120,10, 200, 100, "TEST2", "#80000000")
frame.anchor("all");
frame.setFont('fonts/armata30');
frame.foreColor 	= "#FFFFFFFF"
frame.backColor 	= "#FF000000"
frame.borderColor	= "#FFFFFFFF"
frame.border 		= 2;
frame.textAlignment	= "MiddleCenter"
frame.click = function (f,x,y)
	print ("click " .. x .. ", " .. y )
end
print( type(true) )
root.add( frame )

print(frame.foreColor);
print(frame.backColor);
print(frame.borderColor);
print("qqqqq")

frame.y = 10

