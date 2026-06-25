using System;
using System.Reflection;
using Assets;
using Assets._ReusableScripts.CuchiCuchi;
using Assets._ReusableScripts.CuchiCuchi.Chars.Alteradores;
using Assets._ReusableScripts.CuchiCuchi.Controllers;
using Assets._ReusableScripts.CuchiCuchi.Controllers.Ojos.Parpadeos;
using Assets._ReusableScripts.CuchiCuchi.Dependentes.Controllers;
using Assets._ReusableScripts.Genetica.NPCs;
using BetterExperience.GameScopes;
using BetterExperience.Wrappers.Physics;
using BetterExperience.Wrappers.Pools;
using HarmonyLib;
using UnityEngine;

namespace BetterExperience.Wrappers.Characters;

public class GuestCharacter
{
	// BaseFemalePoseLoader is [Obsolete(error:true)] in SMA 23.1 — access via reflection
	private static readonly Type _poseLoaderType =
		AccessTools.TypeByName("Assets._ReusableScripts.CuchiCuchi.Dependentes.ControllerPoses.BaseFemalePoseLoader");
	private static readonly EventInfo _poseChangedEvent =
		_poseLoaderType?.GetEvent("poseChanged");
	private static readonly PropertyInfo _currentPoseProp =
		_poseLoaderType?.GetProperty("currentPose");

	private Component poseLoader;

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

	public ModifierManager ModifierManager { get; private set; }

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
		poseLoader = _poseLoaderType != null ? Impl.GetComponent(_poseLoaderType) : null;
		Scope.EventHandler(delegate(Action<AnimController> x)
		{
			if (guestCharacter.poseLoader != null)
				_poseChangedEvent?.AddEventHandler(guestCharacter.poseLoader, x);
		}, delegate(Action<AnimController> x)
		{
			if (guestCharacter.poseLoader != null)
				_poseChangedEvent?.RemoveEventHandler(guestCharacter.poseLoader, x);
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
		ModifierManager = new ModifierManager(Impl.gameObject);
		IsMaterialized = true;
		this.GuestMaterialized();
	}

	public string GetCurrentPoseStr()
	{
		return (_currentPoseProp?.GetValue(poseLoader))?.ToString() ?? "";
	}

	public void SynchronizeCharacterWithInstance()
	{
		Impl.GetComponentEnRoot<AlteradoresDeAparienciaFemenina>().flagToForceUpdateValores = true;
		Impl.GetComponentEnRoot<AlteradoresDePersonalidadFemenina>().flagToForceUpdateValores = true;
		GuestValuesChanged.Invoke();
	}
}
