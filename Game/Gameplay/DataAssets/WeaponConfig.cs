using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.Gameplay.Components;

namespace IronStar.Gameplay.DataAssets
{
	public class WeaponConfig : DataAsset
	{
		public TimeSpan	TimeWarmup		{ get; set; }	=	TimeSpan.FromMilliseconds(0);
		public TimeSpan	TimeCooldown	{ get; set; }	=	TimeSpan.FromMilliseconds(0);
		public TimeSpan	TimeOverheat	{ get; set; }	=	TimeSpan.FromMilliseconds(0);
		public TimeSpan	TimeReload		{ get; set; }	=	TimeSpan.FromMilliseconds(0);
		public TimeSpan	TimeDrop		{ get; set; }	=	TimeSpan.FromMilliseconds(350);
		public TimeSpan	TimeRaise		{ get; set; }	=	TimeSpan.FromMilliseconds(350);
		public TimeSpan	TimeNoAmmo		{ get; set; }	=	TimeSpan.FromMilliseconds(250);

		public string	BeamHitFX		{ get; set; }	=	null;
		public string	BeamTrailFX		{ get; set; }	=	null;
		public string	MuzzleFX		{ get; set; }	=	null;

		public string	ProjectileClass	{ get; set; }	=	null;
		public int		ProjectileCount	{ get; set; }	=	1;
		public int		Damage			{ get; set; }	=	0;
		public float	Impulse			{ get; set; }	=	0;
		public float	MaxSpread		{ get; set; }	=	0;

		public SpreadMode	SpreadMode	{ get; set; }	=	SpreadMode.Const;
		public float		Spread		{ get; set; }	=	0;

		public string	AmmoClass		{ get; set; }	=	"";
		public int		AmmoConsumption	{ get; set; }	=	1;
	}
}
