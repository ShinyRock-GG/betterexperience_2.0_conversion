using System;
using Assets;
using Assets._ReusableScripts.CuchiCuchi;
using Assets._ReusableScripts.CuchiCuchi.Chars.Alteradores;
using Assets._ReusableScripts.CuchiCuchi.Controllers;
using Assets._ReusableScripts.CuchiCuchi.Controllers.Ojos.Parpadeos;
using Assets._ReusableScripts.CuchiCuchi.Dependentes.ControllerPoses;
using Assets._ReusableScripts.CuchiCuchi.Dependentes.Controllers;
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

	public ComboGestureController GesturesController { get; private set; }

	public FemaleChar Impl { get; }

	public GuestInstance GuestInstance { get; private set; }

	public RadialMenu RadialMenu { get; private set; }

	public GameObject RootObject => Impl.gameObject;

	public LookAtControllerV2 LookAtComponent { get; private set; }

	public OjosExpresionController EyesExpressionComponent { get; private set; }

	public bool IsMaterialized { get; private set; }

	public event Action PoseChanged = delegate
	{
	};

	public event Action GuestMaterialized = delegate
	{
	};

	public GuestCharacter(FemaleChar currentFemaleCharacter, ISujetoIdentificableNpc providedGenetics, ScopeSupport parentScope)
	{
		GuestCharacter guestCharacter = this;
		Impl = currentFemaleCharacter;
		_providedGeneticsChar = providedGenetics;
		parentScope.AddChild(Scope);
		DispatcherService dispatcher = Scope.Lookup<DispatcherService>();
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
				dispatcher.InvokeLater(guestCharacter.Materialize);
			});
		}
		else
		{
			dispatcher.InvokeLater(Materialize);
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
		HeadController = new GuestHeadController(Impl.gameObject, Scope);
		GesturesController = new ComboGestureController(Impl.gameObject, Scope);
		Scope.AddChild(HeadController);
		RadialMenu = new RadialMenu(Impl.gameObject);
		PoolManager pools = Scope.Lookup<GameSession>().PoolManager;
		GuestInstance = pools.FindGuest(Impl.ID_Unico.ToString());
		if (GuestInstance == null && pools.Count > 0)
		{
			GuestInstance = new GuestInstance(_providedGeneticsChar, pools.AnyPool);
		}
		LookAtComponent = Impl.GetComponentInChildren<LookAtControllerV2>();
		EyesExpressionComponent = Impl.GetComponentInChildren<OjosExpresionController>();
		IsMaterialized = true;
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
