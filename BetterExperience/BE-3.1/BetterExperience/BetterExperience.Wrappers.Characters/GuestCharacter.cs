using System;
using Assets;
using Assets._ReusableScripts.CuchiCuchi;
using Assets._ReusableScripts.CuchiCuchi.Chars.Alteradores;
using Assets._ReusableScripts.CuchiCuchi.Controllers;
using Assets._ReusableScripts.CuchiCuchi.Dependentes.ControllerPoses;
using Assets._ReusableScripts.Genetica.NPCs;
using BetterExperience.GameScopes;
using BetterExperience.Wrappers.Physics;
using BetterExperience.Wrappers.Pools;
using UnityEngine;

namespace BetterExperience.Wrappers.Characters;

public class GuestCharacter
{
	private BaseFemalePoseLoader poseLoader;

	private ISujetoIdentificableNpc _providedGeneticsChar;

	public ScopeSupport Scope { get; } = new ScopeSupport
	{
		Name = "GuestChar"
	};

	public Observable GuestValuesChanged { get; } = new Observable();

	public PhysicalPuppet Puppet { get; private set; }

	public GuestHeadController HeadController { get; private set; }

	public FemaleChar Impl { get; }

	public GuestInstance GuestInstance { get; private set; }

	public RadialMenu RadialMenu { get; private set; }

	public GameObject RootObject => Impl.gameObject;

	public event Action PoseChanged = delegate
	{
	};

	public event Action GuestMaterialized = delegate
	{
	};

	public GuestCharacter(FemaleChar currentFemaleCharacter, ISujetoIdentificableNpc providedGenetics)
	{
		GuestCharacter guestCharacter = this;
		Impl = currentFemaleCharacter;
		_providedGeneticsChar = providedGenetics;
		if (!currentFemaleCharacter.isStared)
		{
			Scope.EventHandler(delegate(CustomMonobehaviourEventHandler x)
			{
				currentFemaleCharacter.stared += x;
			}, delegate(CustomMonobehaviourEventHandler x)
			{
				currentFemaleCharacter.stared -= x;
			}, delegate
			{
				guestCharacter.Materialize();
			});
		}
		poseLoader = Impl.GetComponent<BaseFemalePoseLoader>();
		Scope.EventHandler(delegate(Action<AnimController> x)
		{
			guestCharacter.poseLoader.poseChanged += x;
		}, delegate(Action<AnimController> x)
		{
			guestCharacter.poseLoader.poseChanged -= x;
		}, delegate
		{
			guestCharacter.PoseChanged();
		});
	}

	public void Materialize()
	{
		Puppet = new PhysicalPuppet(Impl.gameObject);
		HeadController = new GuestHeadController(Impl.gameObject);
		Scope.AddChild(HeadController);
		RadialMenu = new RadialMenu(Impl.gameObject);
		PoolManager poolManager = Scope.Lookup<GameSession>().PoolManager;
		GuestInstance = poolManager.FindGuest(Impl.ID_Unico.ToString());
		if (GuestInstance == null && poolManager.Count > 0)
		{
			GuestInstance = new GuestInstance(_providedGeneticsChar, poolManager.AnyPool);
		}
		this.GuestMaterialized();
	}

	public string GetCurrentPoseStr()
	{
		return poseLoader.currentPose.ToString();
	}

	public void SynchronizeCharacterWithInstance()
	{
		Impl.GetComponentEnRoot<AlteradoresDeAparienciaFemenina>().flagToForceUpdateValores = true;
		Impl.GetComponentEnRoot<AlteradoresDePersonalidadFemenina>().flagToForceUpdateValores = true;
		GuestValuesChanged.Invoke();
	}
}
