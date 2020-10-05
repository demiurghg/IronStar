using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;

namespace IronStar.Gameplay.Assets
{
	public class WeaponDescription : JsonContent
	{
		public static readonly WeaponDescription Default = new WeaponDescription();

		public TimeSpan	TimeWarmup		=	TimeSpan.FromMilliseconds(0);
		public TimeSpan	TimeCooldown	=	TimeSpan.FromMilliseconds(0);
		public TimeSpan	TimeOverheat	=	TimeSpan.FromMilliseconds(0);
		public TimeSpan	TimeReload		=	TimeSpan.FromMilliseconds(0);
		public TimeSpan	TimeDrop		=	TimeSpan.FromMilliseconds(350);
		public TimeSpan	TimeRaise		=	TimeSpan.FromMilliseconds(350);
		public TimeSpan	TimeNoAmmo		=	TimeSpan.FromMilliseconds(250);

		public string	Name			=	null;
		public string	NiceName		=	null;

		public string	BeamHitFX		=	null;
		public string	BeamTrailFX		=	null;
		public string	MuzzleFX		=	null;

		public string	ProjectileClass	=	null;
		public int		ProjectileCount	=	1;
		public int		Damage			=	0;
		public float	Impulse			=	0;
		public float	Spread			=	0;

		public string	AmmoClass		=	"";
		public int		AmmoConsumption	=	1;

		public bool		IsBeamWeapon { get { return ProjectileClass==null; } }
	}
}
