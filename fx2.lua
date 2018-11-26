local fx = registerFX("fire")

local function emit ()
	return {
		Position		=	gaussRadial(2,2)
		Velocity		=	gaussRadial(0,4)
		Acceleration	=	gaussRadial(0,4)
		Color			=	color.create( 2,2,2 )
		Alpha			=	0.7
		Roughness		=	0.7
		Metallic		=	0
		Intensity		=	1000
		Scattering		=	0
		BeamFactor		=	1
		Gravity			=	1               
		Damping			=	0               
		Size			=	vector2( 0.1, 0.3 )
		Rotation		=	vector2( rand(0,360), rand(0,360) )
		LifeTime		=	gauss( 0.1, 0.3 )
		TimeLag			=   0.1
		FadeIn			=   0.1
		FadeOut			=   0.2
		Image			=	choose("fire01", "fire02", "fire03")
		Effects			=	"gauss"      
	}
end

fx.cache( 1000, emit );

fx.light {
	color		=	color(127,128,129)
	intensity	=	1000
	innerRadius	=	1
	outerRadius	=	5
	offset		=	vector3( 0, 0.5, 0 )
}

fx.sound {
	event	=	"env/fire"
	reverb	=	0.2
}