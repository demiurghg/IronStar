

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


local test = {}
local mt = {
	__newindex = function (t,k,a,b,c) 
		print( "key = " .. tostring(k) );
		print( ". . . " .. tostring(a) );
		print( ". . . " .. tostring(b) );
		print( ". . . " .. tostring(c) );
	end
}
setmetatable( test, mt )

test.qqq = 2,3,"qqq";

--[[
-- hierarchy, position & size 
frame.move 	( x, y );
frame.resize( w, h );
frame.add	( frame );
frame.remove( frame );

-- colors
frame.set_color_text		("#FF00FFFF");
frame.set_color_background	("#FF00FFFF");
frame.set_color_overall		("#FF00FFFF");
frame.set_color_border		(255,255,255,255);
frame.set_color_image		(255,255,255,255);

-- borders, padding & anchors
frame.set_borders(2,2,2,2);
frame.set_padding(2,2,2,2);
frame.set_anchors("LRTB");

-- text 
frame.set_text("Some Text");
frame.set_text_font		 ("fonts/armata30");
frame.set_text_color 	 ("#ffffff00");	-- alias for 'set_color_text'
frame.set_text_alignment ("R_");
frame.set_text_offset	 (2,20);

-- image
frame.set_image 		("ui/images/testImage");
frame.set_image_color 	("#ffff00aa");
frame.set_image_mode	("stretch");

-- handlers :
-- __newindex???
frame.on_tick		( function (f) end );
frame.on_click		( function (f,x,y,key) end );
frame.on_dclick		( function (f,x,y,key) end );
frame.on_move		( function (f,x,y ) end );
frame.on_resize		( function (f,w,h ) end );
frame.on_mouse_down ( function (f,x,y,key) end; );
frame.on_mouse_up   ( function (f,x,y,key) end; );
frame.on_mouse_move ( function (f,x,y,key) end; );
frame.on_mouse_in   ( function (f,x,y,key) end; );
frame.on_mouse_out  ( function (f,x,y,key) end; );
frame.on_mouse_wheel( function (f,x,y,key) end; );
frame.on_hover		( function (f) end; )
frame.on_press		( function (f) end; )
frame.on_release	( function (f) end; )

--]]