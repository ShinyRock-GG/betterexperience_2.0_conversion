using System;
using System.Collections.Generic;
using Assets._ReusableScripts.CuchiCuchi.Skins;
using BetterExperience.GameScopes;
using BetterExperience.Utils;
using UnityEngine;

namespace BetterExperience.Wrappers.Physics;

internal class CollisionTracker
{
	public struct CollisionKey : IEquatable<CollisionKey>
	{
		public PuppetSenderPart SenderPart { get; set; }

		public Rigidbody Sender { get; set; }

		public PuppetReceiverPart ReceiverPart { get; set; }

		public Rigidbody Receiver { get; set; }

		public Collider ReceiverCollider { get; set; }

		public Collider SenderCollider { get; internal set; }

		public override bool Equals(object obj)
		{
			if (obj is CollisionKey other)
			{
				return Equals(other);
			}
			return false;
		}

		public bool Equals(CollisionKey other)
		{
			if (SenderPart == other.SenderPart && EqualityComparer<Rigidbody>.Default.Equals(Sender, other.Sender) && ReceiverPart == other.ReceiverPart)
			{
				return EqualityComparer<Rigidbody>.Default.Equals(Receiver, other.Receiver);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (((-219435366 * -1521134295 + SenderPart.GetHashCode()) * -1521134295 + EqualityComparer<Rigidbody>.Default.GetHashCode(Sender)) * -1521134295 + ReceiverPart.GetHashCode()) * -1521134295 + EqualityComparer<Rigidbody>.Default.GetHashCode(Receiver);
		}
	}

	public PhysicalPuppet Sender { get; }

	public PhysicalPuppet Receiver { get; }

	public BitMask<PuppetSenderPart> SenderFilter { get; set; } = BitMask<PuppetSenderPart>.AllOf();

	public BitMask<PuppetReceiverPart> ReceiverFilter { get; set; } = BitMask<PuppetReceiverPart>.AllOf();

	public Func<bool> GlobalFilter { get; set; } = () => true;

	public Observable<HitPartEnum, PuppetSenderPart, Collision> OnCollision { get; } = new Observable<HitPartEnum, PuppetSenderPart, Collision>();

	public ListDictionary<CollisionKey, List<ContactPoint>> Collisions { get; } = new ListDictionary<CollisionKey, List<ContactPoint>>();

	public Func<Collision, bool> CollisionFilter { get; set; } = (Collision c) => true;

	public bool CollectCollisions { get; set; } = true;

	public CollisionTracker(PhysicalPuppet sender, PhysicalPuppet receiver)
	{
		Sender = sender;
		Receiver = receiver;
	}

	public void Start(ScopeSupport scope)
	{
		Receiver.OnSkinCollisionStay.Add(OnReceive, scope);
	}

	private void OnReceive(HitSkin arg1, Collision arg2)
	{
		if (!GlobalFilter())
		{
			return;
		}
		PuppetSenderPart? bodyPart = Sender.GetBodyPart(arg2.rigidbody);
		if (!bodyPart.HasValue)
		{
			return;
		}
		PuppetReceiverPart hitParte = (PuppetReceiverPart)arg1.hitParte;
		if (!CollisionFilter(arg2) || !SenderFilter.Contains(bodyPart.Value) || !ReceiverFilter.Contains(hitParte))
		{
			return;
		}
		OnCollision.Invoke(arg1.hitParte, bodyPart.Value, arg2);
		if (!CollectCollisions)
		{
			return;
		}
		ContactPoint[] contacts = arg2.contacts;
		for (int i = 0; i < contacts.Length; i++)
		{
			ContactPoint item = contacts[i];
			CollisionKey key = new CollisionKey
			{
				ReceiverPart = hitParte,
				Receiver = item.thisCollider.attachedRigidbody,
				ReceiverCollider = item.thisCollider,
				SenderPart = bodyPart.Value,
				Sender = item.otherCollider.attachedRigidbody,
				SenderCollider = item.otherCollider
			};
			Collisions.GetValueOrAdd(key, () => new List<ContactPoint>()).Add(item);
		}
	}
}
