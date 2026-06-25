using System;
using Assets.Productos.Juegos.Reception.Scripts.Dependientes.ScenaManagers;
using BetterExperience.Wrappers.Cameras;
using BetterExperience.Wrappers.Characters;
using BetterExperience.Wrappers.Pools;
using BetterExperience.Wrappers.Windows;

namespace BetterExperience.GameScopes;

public class GameSession
{
	private EntrevistaConFemale interview;

	private GuestCharacter currentFemale;

	private PlayerCharacter currentPlayer;

	private MainCamera mainCamera;

	private MainModalWindow modals;

	private PoolManager poolManager;

	public GuestCharacter Guest => currentFemale;

	public PlayerCharacter Player => currentPlayer;

	public MainCamera MainCamera => mainCamera;

	public MainModalWindow Modal => modals;

	public PoolManager PoolManager => poolManager;

	public bool SingleMode { get; protected set; }

	public ScopeSupport Scope { get; } = new ScopeSupport
	{
		Autostart = false
	};

	public Observable PreSave { get; } = new Observable();

	public event Action<GuestCharacter> OnGuestReady = delegate
	{
	};

	public event Action<GuestCharacter> OnGuestLeft = delegate
	{
	};

	protected void SetInterviewInstance(EntrevistaConFemale obj)
	{
		interview = obj;
		GuestCharacter guest = new GuestCharacter(obj.currentFemaleCharacter, (obj is EntrevistaConSingleFemale) ? ((EntrevistaConSingleFemale)obj).currentNpc : null);
		Scope.AddChild(guest.Scope);
		guest.SynchronizeCharacterWithInstance();
		obj.femalePresenciaChanged += GuestPresenceChanged;
		guest.GuestMaterialized += delegate
		{
			currentFemale = guest;
			this.OnGuestReady(currentFemale);
		};
		if (SingleMode)
		{
			guest.Materialize();
		}
	}

	private void GuestPresenceChanged(EntrevistaConFemale.FemalePresencia last, EntrevistaConFemale.FemalePresencia current, EntrevistaConFemale sender)
	{
		if (current != EntrevistaConFemale.FemalePresencia.presente && sender == interview)
		{
			GuestCharacter guestCharacter = currentFemale;
			currentFemale = null;
			this.OnGuestLeft(guestCharacter);
			guestCharacter.Scope.Dispose();
		}
	}

	public GameSession()
	{
		Scope.Name = "GameSession";
		currentPlayer = new PlayerCharacter();
		Scope.AddChild(currentPlayer);
		mainCamera = new MainCamera();
		modals = new MainModalWindow();
		poolManager = new PoolManager();
		Scope.AddChild(poolManager.Scope);
		Scope.Provide(this);
	}
}
