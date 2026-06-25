using System.Collections.Generic;
using Assets.Base.Bones.Gizmos.BeachGirl.Runtime;

namespace BetterExperience.CustomScene.Poser;

public class BoneMuscleData : Dictionary<string, MuscleConfig>
{
	internal void Save(GizmoDeBoneRMInfo x)
	{
		if (x.esMusculo)
		{
			string key = MuscleKey(x);
			if (!ContainsKey(key))
			{
				MuscleConfig cfg = new MuscleConfig();
				cfg.Pin = x.gizmoDeBone.boneMuscleConfig.musclePin;
				cfg.Spring = x.gizmoDeBone.boneMuscleConfig.muscleSpring;
				cfg.Damper = x.gizmoDeBone.boneMuscleConfig.muscleDamper;
				base[key] = cfg;
			}
		}
		if (x.isEffector)
		{
			string key2 = EffectorKey(x);
			if (!ContainsKey(key2))
			{
				MuscleConfig cfg2 = new MuscleConfig();
				cfg2.IsSupport = x.gizmoDeBone.boneMuscleConfig.puedeApoyarse;
				cfg2.CanInteract = x.gizmoDeBone.boneMuscleConfig.puedeInteractuar;
				cfg2.CanReact = x.gizmoDeBone.boneMuscleConfig.puedeReaccionar;
				base[key2] = cfg2;
			}
		}
	}

	internal void Apply(GizmoDeBoneRMInfo x)
	{
		if (x.esMusculo)
		{
			string key = MuscleKey(x);
			if (TryGetValue(key, out var cfg))
			{
				x.gizmoDeBone.boneMuscleConfig.musclePin = cfg.Pin;
				x.gizmoDeBone.boneMuscleConfig.muscleSpring = cfg.Spring;
				x.gizmoDeBone.boneMuscleConfig.muscleDamper = cfg.Damper;
			}
		}
		if (x.isEffector)
		{
			string key2 = EffectorKey(x);
			if (TryGetValue(key2, out var cfg2))
			{
				x.gizmoDeBone.boneMuscleConfig.puedeApoyarse = cfg2.IsSupport;
				x.gizmoDeBone.boneMuscleConfig.puedeInteractuar = cfg2.CanInteract;
				x.gizmoDeBone.boneMuscleConfig.puedeReaccionar = cfg2.CanReact;
			}
		}
	}

	private string MuscleKey(GizmoDeBoneRMInfo x)
	{
		return $"M-{x.muscle}-{x.side}";
	}

	private string EffectorKey(GizmoDeBoneRMInfo x)
	{
		return $"E-{x.effector}";
	}
}
