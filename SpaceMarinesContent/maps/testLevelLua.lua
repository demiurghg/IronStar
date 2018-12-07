
map = createMap(16,16)
map.setTileSet("tiles/space02/space02");

map.mazeDef("S", "INFO_START");
map.mazeDef("F", "INFO_FINISH");

map.maze(
[[
                
 2+1+2   +  +   
 xxxxx  xXz1**  
 xXxxz1xx    *+ 
 xz*zxxxz     c 
 x1zxxz1x1    c 
 +xxx+xx11+  1+ 
   x   x     *  
   x   x     s  
 +zxc+*z*caa*s  
    c   *   a   
    a   *   *   
 F  ***c*aa*z   
 **ss  ++++     
 S  *  *  *     
                
]]);


return map;