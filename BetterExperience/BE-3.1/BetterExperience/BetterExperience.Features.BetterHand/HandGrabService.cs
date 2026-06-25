using System.Collections.Generic;
using Assets._ReusableScripts.CuchiCuchi.Dependentes.Characters.Controlladores;
using BetterExperience.GameScopes;
using BetterExperience.Utils;
using BetterExperience.Wrappers.Characters;
using BetterExperience.Wrappers.Physics;
using UnityEngine;

namespace BetterExperience.Features.BetterHand;

internal class HandGrabService : SessionService
{
	private class GrabInstance
	{
		public ContactPoint? ContactPoint { get; private set; }

		public PuppetReceiverPart GrabPart { get; private set; }

		public FixedJoint GrabJoint { get; private set; }

		public bool IsGrab => GrabJoint != null;

		public bool CanGrab
		{
			get
			{
				if (!IsGrab)
				{
					return ContactPoint.HasValue;
				}
				return false;
			}
		}

		public PhysicalPuppet Puppet { get; internal set; }

		internal void SetCollision(CollisionTracker.CollisionKey key, List<ContactPoint> value)
		{
			foreach (ContactPoint item in value)
			{
				if (item.thisCollider.attachedRigidbody != null && item.otherCollider.attachedRigidbody != null)
				{
					ContactPoint = item;
					GrabPart = key.ReceiverPart;
					break;
				}
			}
		}

		internal void ResetCollision()
		{
			ContactPoint = null;
		}

		internal void Drop()
		{
			if (IsGrab)
			{
				Object.Destroy(GrabJoint);
				GrabJoint = null;
				Puppet.SetLimbRelaxed(GrabPart, relaxed: false);
			}
		}

		internal void Grab()
		{
			if (CanGrab)
			{
				ContactPoint value = ContactPoint.Value;
				Collider thisCollider = value.thisCollider;
				Collider otherCollider = value.otherCollider;
				Rigidbody rigidbody = Puppet.GetDraggableRigidbody(GrabPart);
				if (rigidbody == null)
				{
					rigidbody = thisCollider.attachedRigidbody;
				}
				GrabJoint = rigidbody.gameObject.AddComponent<FixedJoint>();
				GrabJoint.anchor = rigidbody.centerOfMass;
				GrabJoint.connectedBody = otherCollider.attachedRigidbody;
				GrabJoint.connectedAnchor = otherCollider.attachedRigidbody.centerOfMass;
				Puppet.SetLimbRelaxed(GrabPart, relaxed: true);
			}
		}

		internal bool ShouldBreakLink()
		{
			if (IsGrab && GrabJoint.currentForce.sqrMagnitude > 10000000f)
			{
				return true;
			}
			return false;
		}
	}

	private HandControllerV2 handController;

	private GrabInstance grabber = new GrabInstance();

	private SmartHandService smartHand;

	public bool ActivateByClick { get; set; }

	private bool IsHand
	{
		get
		{
			if (handController.tipoDePose != HandTipoDePose.massage)
			{
				return handController.tipoDePose == HandTipoDePose.finger;
			}
			return true;
		}
	}

	private bool IsGrabbing => grabber.IsGrab;

	private bool CanLock
	{
		get
		{
			if (IsHand)
			{
				return grabber.CanGrab;
			}
			return false;
		}
	}

	public override void OnStart()
	{
		handController = base.Session.Player.GameObject.GetComponentInChildren<HandControllerV2>();
		Plugin.DoUpdate.Add(OnUpdate, base.Scope);
		GuestCharacter guest = base.Session.Guest;
		PhysicalPuppet physicalPuppet = new PhysicalPuppet(base.Session.Player.GameObject);
		List<Collider> palmColliders = physicalPuppet.ColliderByName("CC_Base_Hand.R_Collider");
		physicalPuppet.ColliderByName("CC_Base_Finger12.R");
		Lookup<HandPhysicsService>().SubscribePhysics(new HandPhysicsService.CollisionListener
		{
			KeyFilter = delegate(CollisionTracker.CollisionKey key)
			{
				if (handController.tipoDePose == HandTipoDePose.massage)
				{
					return palmColliders.Contains(key.SenderCollider);
				}
				return handController.tipoDePose != HandTipoDePose.finger || key.SenderCollider.name == "CC_Base_Finger12.R";
			},
			Callback = OnPhysicsCallback
		}, base.Scope);
		grabber.Puppet = guest.Puppet;
		smartHand = base.Scope.Parent.Lookup<SmartHandService>();
	}

	public override void OnStop()
	{
		StopGrabbing();
	}

	private void OnUpdate()
	{
		HandleInput();
	}

	private void HandleInput()
	{
		if (IsGrabbing)
		{
			if (!IsHand)
			{
				StopGrabbing();
			}
			else if (grabber.ShouldBreakLink())
			{
				StopGrabbing();
			}
		}
		if (ActivateByClick)
		{
			HandleClickActivation();
		}
		else
		{
			HandleHoldActivation();
		}
	}

	private void OnPhysicsCallback(ListDictionary<CollisionTracker.CollisionKey, List<ContactPoint>> collisions)
	{
		KeyValuePair<CollisionTracker.CollisionKey, List<ContactPoint>> keyValuePair = SelectWhereMostContact(collisions);
		if (keyValuePair.Value == null)
		{
			grabber.ResetCollision();
		}
		else
		{
			grabber.SetCollision(keyValuePair.Key, keyValuePair.Value);
		}
	}

	private KeyValuePair<CollisionTracker.CollisionKey, List<ContactPoint>> SelectWhereMostContact(ListDictionary<CollisionTracker.CollisionKey, List<ContactPoint>> collisions)
	{
		KeyValuePair<CollisionTracker.CollisionKey, List<ContactPoint>> result = default(KeyValuePair<CollisionTracker.CollisionKey, List<ContactPoint>>);
		foreach (KeyValuePair<CollisionTracker.CollisionKey, List<ContactPoint>> collision in collisions)
		{
			if (result.Value == null || collision.Value.Count > result.Value.Count)
			{
				result = collision;
			}
		}
		return result;
	}

	private void StopGrabbing()
	{
		grabber.Drop();
		smartHand.LockGrip = false;
	}

	private void StartGrabbing()
	{
		grabber.Grab();
		smartHand.LockGrip = true;
	}

	private void HandleHoldActivation()
	{
		if (Input.GetMouseButton(0))
		{
			if (CanLock)
			{
				StartGrabbing();
			}
		}
		else if (IsGrabbing)
		{
			StopGrabbing();
		}
	}

	private void HandleClickActivation()
	{
		if (Input.GetMouseButtonDown(0))
		{
			if (IsGrabbing)
			{
				StopGrabbing();
			}
			else if (CanLock)
			{
				StartGrabbing();
			}
		}
	}
}
