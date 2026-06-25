using System;

namespace BetterExperience.GameScopes;

public class SessionService : PluginService
{
	public GameSession Session { get; private set; }

	public SessionService()
	{
		base.Scope.OnInit += Scope_OnInit;
	}

	private void Scope_OnInit()
	{
		Session = Lookup<SessionTracker>().Current;
		if (Session == null)
		{
			throw new Exception("Unable to resolve session");
		}
	}
}
