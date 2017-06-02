

local root = ui.root()

print("width:"..root.width.."  height:"..root.height);

root.clear()	

function click_handler (f,x,y,key)
	print ("click at [" .. x .. ", " .. y .. ", " .. key .. "] on " .. f.text)
end

frame = ui.new(10,10, 100, 100, "Test1", "#80000000")
root.add( frame )
frame.on_click = click_handler;

frame = ui.new(120,10, 200, 100, "TEST2", "#80000000")
frame.anchor	("all");
frame.set_font	('fonts/armata30');
frame.fore_color 	= "#FFFFFFFF"
frame.back_color 	= "#FF000000"
frame.border_color	= "#FFFFFFFF"
frame.border 		= 5;
frame.alignment		= "middlecenter"

frame.on_click = click_handler;

print( type(true) )
root.add( frame )

print(frame.fore_color);
print(frame.back_color);
print(frame.border_color);
print("qqqqq")

frame.y = 10


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
frame.on_tick			=	function (f) end );
frame.on_click			=	function (f,x,y,key) end );
frame.on_dclick			=	function (f,x,y,key) end );
frame.on_move			=	function (f,x,y ) end );
frame.on_resize			=	function (f,w,h ) end );
frame.on_mouse_down 	=	function (f,x,y,key) end; );
frame.on_mouse_up   	=	function (f,x,y,key) end; );
frame.on_mouse_move 	=	function (f,x,y,key) end; );
frame.on_mouse_in   	=	function (f,x,y,key) end; );
frame.on_mouse_out  	=	function (f,x,y,key) end; );
frame.on_mouse_wheel	=	function (f,x,y,key) end; );
frame.on_hover			=	function (f) end; )
frame.on_press			=	function (f) end; )
frame.on_release		=	function (f) end; )

--]]