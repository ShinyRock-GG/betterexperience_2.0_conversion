using System;
using System.Collections.Generic;

namespace BetterExperience.GameScopes;

public class BaseObservable<T> where T : Delegate
{
	private delegate void dyninvoke(params object[] args);

	private List<T> subscribers = new List<T>();

	private Dictionary<T, ScopeSupport> delegateScopes = new Dictionary<T, ScopeSupport>();

	private bool invokingNow;

	private List<object[]> buffer = new List<object[]>();

	public bool Buffered { get; set; }

	public void InvokeDynamic(params object[] args)
	{
		if (Buffered)
		{
			buffer.Add(args);
		}
		else
		{
			InvokeDynamicImpl(args);
		}
	}

	private void InvokeDynamicImpl(params object[] args)
	{
		invokingNow = true;
		try
		{
			List<Action> list = null;
			foreach (T subscriber in subscribers)
			{
				try
				{
					subscriber.DynamicInvoke(args);
				}
				catch (Exception ex)
				{
					Exception ex2 = ex;
					Exception e = ex2;
					if (delegateScopes.TryGetValue(subscriber, out var scope))
					{
						if (list == null)
						{
							list = new List<Action>();
						}
						list.Add(delegate
						{
							scope.NotifyCrash(e);
						});
					}
					else
					{
						new Logger().Error(e, "Delegate failed");
					}
				}
			}
			if (list == null)
			{
				return;
			}
			foreach (Action item in list)
			{
				item();
			}
		}
		finally
		{
			invokingNow = false;
		}
	}

	public void Add(T impl)
	{
		if (invokingNow)
		{
			subscribers = new List<T>(subscribers);
		}
		subscribers.Add(impl);
	}

	public void Add(T impl, ScopeSupport scope)
	{
		Add(impl);
		scope.OnDispose += delegate
		{
			Remove(impl);
		};
		delegateScopes[impl] = scope;
	}

	public bool Remove(T impl)
	{
		if (invokingNow)
		{
			subscribers = new List<T>(subscribers);
		}
		delegateScopes.Remove(impl);
		return subscribers.Remove(impl);
	}

	public void Flush()
	{
		foreach (object[] item in buffer)
		{
			InvokeDynamicImpl(item);
		}
		buffer.Clear();
	}
}
