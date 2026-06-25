using System;
using System.Collections.Generic;
using Assets._ReusableScripts.CuchiCuchi.Dependentes.Characters.Controlladores;
using BetterExperience.GameScopes;
using BetterExperience.Utils;
using BetterExperience.Wrappers.Characters;
using BetterExperience.Wrappers.Physics;
using UnityEngine;

namespace BetterExperience.Features.BetterHand;

internal class HandPhysicsService : SessionService
{
	public class CollisionListener
	{
		public Func<CollisionTracker.CollisionKey, bool> KeyFilter { get; set; }

		public Action<ListDictionary<CollisionTracker.CollisionKey, List<ContactPoint>>> Callback { get; set; }
	}

	private CollisionTracker tracker;

	private HandControllerV2 handController;

	private List<CollisionListener> subscibers = new List<CollisionListener>();

	public override void OnStart()
	{
		base.OnStart();
		handController = base.Session.Player.GameObject.GetComponentInChildren<HandControllerV2>();
	}

	public void SubscribePhysics(CollisionListener listener, ScopeSupport scope)
	{
		if (tracker == null)
		{
			CreateTracker();
		}
		subscibers.Add(listener);
		scope.OnDispose += delegate
		{
			subscibers.Remove(listener);
		};
	}

	private void CreateTracker()
	{
		GuestCharacter guest = base.Session.Guest;
		PhysicalPuppet sender = new PhysicalPuppet(base.Session.Player.GameObject);
		tracker = new CollisionTracker(sender, guest.Puppet);
		tracker.GlobalFilter = () => handController.tipoDePose == HandTipoDePose.massage;
		tracker.SenderFilter = BitMask<PuppetSenderPart>.Of(PuppetSenderPart.handR);
		tracker.Start(base.Scope);
		Lookup<DispatcherService>().DoUpdate.Add(OnUpdate, base.Scope);
	}

	private void OnUpdate()
	{
		ListDictionary<CollisionTracker.CollisionKey, List<ContactPoint>> listDictionary = new ListDictionary<CollisionTracker.CollisionKey, List<ContactPoint>>();
		foreach (CollisionListener subsciber in subscibers)
		{
			listDictionary.Clear();
			if (subsciber.KeyFilter == null)
			{
				subsciber.Callback(tracker.Collisions);
				continue;
			}
			foreach (KeyValuePair<CollisionTracker.CollisionKey, List<ContactPoint>> collision in tracker.Collisions)
			{
				if (subsciber.KeyFilter(collision.Key))
				{
					listDictionary.Add(collision.Key, collision.Value);
				}
			}
			subsciber.Callback(listDictionary);
			listDictionary.Clear();
		}
		tracker.Collisions.Clear();
	}
}
