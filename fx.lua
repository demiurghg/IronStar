

local fx = registerFX("boxBurning")

fx.sound.event 	= "weapon/burning"
fx.sound.reverb	= 1

fx.light.intensity		=	1
fx.light.color			=	temperature(3500);
fx.light.innerRadius	=	1
fx.light.outerRadius	=	30
fx.light.offset			=	vector3( 0, 0, 0.25 );

fx.addParticleStages(3);

fx.particles[0].sprite 		=	"fire0,fire1,fire2";
fx.particles[0].setupLifetime	( "gauss", 1, 3 );
fx.particles[0].setupTiming		( "gauss", 1, 3 );

fx.light(

table = {
  "$type" = "IronStar.SFX.FXFactory, IronStar",
  Period = 0.1,
  SoundStage = {
    $type = "IronStar.SFX.FXSoundStage, IronStar",
    Enabled = false,
    Sound = "",
    Attenuation = "loud"
  },
  LightStage = {
    Enabled 		= true,
    Period 			= 1.0,
    Intensity 		= 100.0,
    Color 			= "255, 108, 22, 255",
    InnerRadius 	= 0.0,
    OuterRadius 	= 3.0,
    PulseString 	= "m",
    LightStyle 		= "inverseSaw",
    OffsetDirection = "none",
    OffsetFactor 	= 0.0
  },
  CameraShake = {
    $type = "IronStar.SFX.FXCameraShake, IronStar",
    Enabled = true
  },
  ParticleStage1 = {
    Enabled = true,
    Sprite = "fire",
    Effect = "soft",
    Count = 50,
    Period = 1.0,
    Timing = {
      Delay = 0.0,
      Bunch = 0.9,
      FadeIn = 0.1,
      FadeOut = 0.2
    },
    Color = "255, 129, 40, 255",
    Alpha = 0.7,
    Roughness = 0.5,
    Metallic = 0.0,
    Intensity = 3000.0,
    Scattering = 1.0,
    Lifetime = {
      $type = "IronStar.SFX.FXLifetime, IronStar",
      Distribution = "gauss",
      MinLifetime = 0.2,
      MaxLifetime = 0.5
    },
    Shape = {
      $type = "IronStar.SFX.FXShape, IronStar",
      Size0 = 1.0,
      Size1 = 0.7,
      EnableRotation = true,
      InitialAngle = 360.0,
      MinAngularVelocity = 0.0,
      MaxAngularVelocity = 0.0
    },
    Position = {
      $type = "IronStar.SFX.FXPosition, IronStar",
      OffsetDirection = "none",
      OffsetFactor = 0.0,
      Distribution = "gaussRadial",
      MinSize = 0.0,
      MaxSize = 0.4
    },
    Velocity = {
      $type = "IronStar.SFX.FXVelocity, IronStar",
      Direction = "localUp",
      LinearDistribution = "gauss",
      LinearVelocityMin = 0.0,
      LinearVelocityMax = 0.0,
      RadialDistribution = "gaussRadial",
      RadialVelocityMin = 0.0,
      RadialVelocityMax = 0.3,
      Advection = 0.0
    },
    Acceleration = {
      $type = "IronStar.SFX.FXAcceleration, IronStar",
      GravityFactor = -1.0,
      Damping = 0.2,
      DragForce = 1.0,
      Turbulence = 1.0
    }
  },
  "ParticleStage2 = {
    $type = "IronStar.SFX.FXParticleStage, IronStar",
    Enabled = true,
    Sprite = "explosionSmoke2",
    Effect = "softLitShadow",
    Count = 15,
    Period = 1.0,
    Timing = {
      $type = "IronStar.SFX.FXTiming, IronStar",
      Delay = 0.0,
      Bunch = 1.0,
      FadeIn = 0.1,
      FadeOut = 0.5
    },
    Color = "5, 5, 5, 255",
    Alpha = 1.0,
    Roughness = 0.3,
    Metallic = 0.0,
    Intensity = 100.0,
    Scattering = 0.0,
    Lifetime = {
      $type = "IronStar.SFX.FXLifetime, IronStar",
      Distribution = "gauss",
      MinLifetime = 0.3,
      MaxLifetime = 0.7
    },
    Shape = {
      $type = "IronStar.SFX.FXShape, IronStar",
      Size0 = 1.0,
      Size1 = 1.0,
      EnableRotation = true,
      InitialAngle = 360.0,
      MinAngularVelocity = 0.0,
      MaxAngularVelocity = 0.0
    },
    Position = {
      $type = "IronStar.SFX.FXPosition, IronStar",
      OffsetDirection = "none",
      OffsetFactor = 0.0,
      Distribution = "uniformRadial",
      MinSize = 0.0,
      MaxSize = 0.0
    },
    Velocity = {
      $type = "IronStar.SFX.FXVelocity, IronStar",
      Direction = "localUp",
      LinearDistribution = "gauss",
      LinearVelocityMin = 0.0,
      LinearVelocityMax = 0.0,
      RadialDistribution = "gaussRadial",
      RadialVelocityMin = 0.0,
      RadialVelocityMax = 1.5,
      Advection = 0.0
    },
    Acceleration = {
      $type = "IronStar.SFX.FXAcceleration, IronStar",
      GravityFactor = -1.0,
      Damping = 1.0,
      DragForce = 0.0,
      Turbulence = 0.0
    }
  },
  "ParticleStage3 = {
    $type = "IronStar.SFX.FXParticleStage, IronStar",
    Enabled = false,
    Sprite = "railDot",
    Effect = "hard",
    Count = 35,
    Period = 1.0,
    Timing = {
      $type = "IronStar.SFX.FXTiming, IronStar",
      Delay = 0.0,
      Bunch = 1.0,
      FadeIn = 0.1,
      FadeOut = 0.1
    },
    Color = "255, 79, 5, 255",
    Alpha = 0.9,
    Roughness = 0.2,
    Metallic = 0.0,
    Intensity = 4000.0,
    Scattering = 0.0,
    Lifetime = {
      $type = "IronStar.SFX.FXLifetime, IronStar",
      Distribution = "gauss",
      MinLifetime = 1.0,
      MaxLifetime = 1.0
    },
    Shape = {
      $type = "IronStar.SFX.FXShape, IronStar",
      Size0 = 0.05,
      Size1 = 0.05,
      EnableRotation = true,
      InitialAngle = 360.0,
      MinAngularVelocity = -250.0,
      MaxAngularVelocity = 250.0
    },
    Position = {
      $type = "IronStar.SFX.FXPosition, IronStar",
      OffsetDirection = "none",
      OffsetFactor = 0.0,
      Distribution = "uniformRadial",
      MinSize = 0.0,
      MaxSize = 0.5
    },
    Velocity = {
      $type = "IronStar.SFX.FXVelocity, IronStar",
      Direction = "localUp",
      LinearDistribution = "gauss",
      LinearVelocityMin = 2.0,
      LinearVelocityMax = 3.0,
      RadialDistribution = "gaussRadial",
      RadialVelocityMin = 0.0,
      RadialVelocityMax = 1.0,
      Advection = 0.0
    },
    Acceleration = {
      $type = "IronStar.SFX.FXAcceleration, IronStar",
      GravityFactor = 0.0,
      Damping = 0.0,
      DragForce = 0.0,
      Turbulence = 0.0
    }
  },
  "ParticleStage4 = {
    $type = "IronStar.SFX.FXParticleStage, IronStar",
    Enabled = false,
    Sprite = "hazeMap",
    Effect = "distortive",
    Count = 10,
    Period = 1.0,
    Timing = {
      $type = "IronStar.SFX.FXTiming, IronStar",
      Delay = 0.0,
      Bunch = 1.0,
      FadeIn = 0.1,
      FadeOut = 0.1
    },
    Color = "255, 255, 255, 255",
    Alpha = 0.25,
    Roughness = 0.5,
    Metallic = 0.0,
    Intensity = 100.0,
    Scattering = 0.0,
    Lifetime = {
      $type = "IronStar.SFX.FXLifetime, IronStar",
      Distribution = "uniform",
      MinLifetime = 1.0,
      MaxLifetime = 1.0
    },
    Shape = {
      $type = "IronStar.SFX.FXShape, IronStar",
      Size0 = 2.0,
      Size1 = 2.0,
      EnableRotation = true,
      InitialAngle = 360.0,
      MinAngularVelocity = 0.0,
      MaxAngularVelocity = 0.0
    },
    Position = {
      $type = "IronStar.SFX.FXPosition, IronStar",
      OffsetDirection = "none",
      OffsetFactor = 0.0,
      Distribution = "uniformRadial",
      MinSize = 0.0,
      MaxSize = 0.0
    },
    Velocity = {
      $type = "IronStar.SFX.FXVelocity, IronStar",
      Direction = "localUp",
      LinearDistribution = "gauss",
      LinearVelocityMin = 1.0,
      LinearVelocityMax = 4.0,
      RadialDistribution = "uniformRadial",
      RadialVelocityMin = 0.0,
      RadialVelocityMax = 1.0,
      Advection = 0.0
    },
    Acceleration = {
      $type = "IronStar.SFX.FXAcceleration, IronStar",
      GravityFactor = 0.0,
      Damping = 0.0,
      DragForce = 0.0,
      Turbulence = 0.0
    }
  }
}