using System.Collections.Generic;
using Assets._ReusableScripts.CuchiCuchi.Dependentes.Characters.Controlladores;
using BetterExperience.Features.BetterHand.Legacy;
using BetterExperience.GameScopes;
using BetterExperience.Utils;
using BetterExperience.Wrappers.Physics;
using UnityEngine;

namespace BetterExperience.Features.BetterHand;

internal class SmartHandService : SessionService
{
	private class ColliderInfo
	{
		public List<Collider> Colliders { get; private set; }

		public List<ContactPoint> Contacts { get; private set; }

		public MeshCollider MeshCollider { get; private set; }

		public ColliderInfo(List<Collider> info)
		{
			Colliders = info;
			Contacts = new List<ContactPoint>();
			foreach (Collider item in info)
			{
				if (item is MeshCollider)
				{
					MeshCollider = (MeshCollider)item;
				}
			}
		}

		internal void Reset()
		{
			Contacts.Clear();
		}
	}

	private HandControllerV2 handController;

	private IGripController gripController;

	private ColliderInfo handCollider;

	private ColliderInfo[] fingerColliders;

	private List<ColliderInfo> trackedColliders = new List<ColliderInfo>();

	private bool hasContact;

	private HandTipoDePose lastHandPose;

	private bool IsHand => handController.tipoDePose == HandTipoDePose.massage;

	public bool LockGrip { get; set; }

	public bool FeatureEnabled { get; set; } = true;

	public bool Experimental { get; set; }

	public bool LegacyGrip { get; set; }

	public override void OnStart()
	{
		base.OnStart();
		if (FeatureEnabled)
		{
			handController = base.Session.Player.GameObject.GetComponentInChildren<HandControllerV2>();
			_ = base.Session.Guest;
			PhysicalPuppet physicalPuppet = new PhysicalPuppet(base.Session.Player.GameObject);
			Lookup<HandPhysicsService>().SubscribePhysics(new HandPhysicsService.CollisionListener
			{
				Callback = OnPhysicsCallback
			}, base.Scope);
			if (LegacyGrip)
			{
				gripController = base.Scope.AddService(new LegacyGripController());
			}
			else
			{
				gripController = base.Scope.AddService(new GripController());
			}
			handCollider = new ColliderInfo(physicalPuppet.ColliderByName("CC_Base_Hand.R_Collider"));
			fingerColliders = new ColliderInfo[12]
			{
				new ColliderInfo(physicalPuppet.ColliderByName("CC_Base_Finger12.R")),
				new ColliderInfo(physicalPuppet.ColliderByName("CC_Base_Finger22.R")),
				new ColliderInfo(physicalPuppet.ColliderByName("CC_Base_Finger32.R")),
				new ColliderInfo(physicalPuppet.ColliderByName("CC_Base_Finger42.R")),
				new ColliderInfo(physicalPuppet.ColliderByName("CC_Base_Finger11.R")),
				new ColliderInfo(physicalPuppet.ColliderByName("CC_Base_Finger21.R")),
				new ColliderInfo(physicalPuppet.ColliderByName("CC_Base_Finger31.R")),
				new ColliderInfo(physicalPuppet.ColliderByName("CC_Base_Finger41.R")),
				new ColliderInfo(physicalPuppet.ColliderByName("CC_Base_Finger10.R")),
				new ColliderInfo(physicalPuppet.ColliderByName("CC_Base_Finger20.R")),
				new ColliderInfo(physicalPuppet.ColliderByName("CC_Base_Finger30.R")),
				new ColliderInfo(physicalPuppet.ColliderByName("CC_Base_Finger40.R"))
			};
			trackedColliders.Add(handCollider);
			trackedColliders.AddRange(fingerColliders);
			Plugin.DoUpdate.Add(OnUpdate, base.Scope);
			gripController.AllowGripUpdate = Experimental;
		}
	}

	private void OnPhysicsCallback(ListDictionary<CollisionTracker.CollisionKey, List<ContactPoint>> collisions)
	{
		foreach (ColliderInfo trackedCollider in trackedColliders)
		{
			trackedCollider.Reset();
		}
		foreach (KeyValuePair<CollisionTracker.CollisionKey, List<ContactPoint>> collision in collisions)
		{
			_ = collision.Value;
			foreach (ContactPoint item in collision.Value)
			{
				_ = item.thisCollider;
				Collider otherCollider = item.otherCollider;
				foreach (ColliderInfo trackedCollider2 in trackedColliders)
				{
					if (trackedCollider2.Colliders.Contains(otherCollider))
					{
						trackedCollider2.Contacts.Add(item);
					}
				}
			}
		}
		hasContact = false;
		foreach (ColliderInfo trackedCollider3 in trackedColliders)
		{
			hasContact |= trackedCollider3.Contacts.Count > 0;
		}
	}

	private void OnUpdate()
	{
		if (IsHand)
		{
			ComputeExpectedGrip();
		}
		SynchronizeBindingPose();
	}

	private void ComputeExpectedGrip()
	{
		if (LockGrip)
		{
			return;
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		for (int i = 0; i < 4; i++)
		{
			int num7 = i;
			int num8 = 0;
			bool flag = false;
			bool flag2 = false;
			if (fingerColliders[num7].Contacts.Count > 0)
			{
				num2++;
				num8++;
				flag2 = true;
				int num9 = 0;
				int num10 = 0;
				List<float> list = new List<float>();
				foreach (ContactPoint contact in fingerColliders[num7].Contacts)
				{
					float num11 = Vector3.Angle(-contact.normal, contact.otherCollider.transform.up);
					list.Add(num11);
					if (num11 <= 90f)
					{
						num9++;
					}
					else
					{
						num10++;
					}
				}
			}
			if (fingerColliders[num7 + 4].Contacts.Count > 0)
			{
				num8++;
				num5++;
			}
			if (fingerColliders[num7 + 8].Contacts.Count > 0)
			{
				num8++;
				num3++;
				flag = true;
			}
			if (num8 >= 2)
			{
				num++;
			}
			else if (flag2 && !flag)
			{
				num4++;
			}
			num6 += num8;
		}
		bool flag3 = handCollider.Contacts.Count > 0;
		GripPose gripPose = gripController.CurrentPose;
		bool forcePose = false;
		if (num3 >= 3 || num5 >= 3 || (flag3 && num6 == 0))
		{
			gripPose = GripPose.FingersDown;
		}
		else if (gripController.CurrentPose == GripPose.FingersDown && num < 2 && num3 < 4 && gripController.TransitionComplete)
		{
			gripPose = GripPose.Idle;
		}
		else if (gripController.CurrentPose == GripPose.FingersDown && gripController.TransitionComplete && !flag3)
		{
			gripPose = GripPose.Idle;
		}
		else if (gripController.CurrentPose == GripPose.FingersDown && gripController.TransitionComplete && num3 == 0)
		{
			gripPose = GripPose.Idle;
		}
		else if (gripController.CurrentPose == GripPose.FingersDown && (num3 > 0 || num4 > 0 || num > 0))
		{
			gripPose = GripPose.FingersDown;
		}
		else if (gripController.TransitionComplete)
		{
			gripPose = GripPose.Idle;
		}
		if (gripPose == GripPose.FingersDown && num <= 4 && gripController.TransitionComplete)
		{
			if (num3 == 0 && num5 == 0)
			{
				gripPose = GripPose.Idle;
			}
			else if (num2 < 3 && num < 3)
			{
				gripPose = GripPose.Idle;
			}
			else if (num3 >= 3 && num2 != 4)
			{
				forcePose = true;
			}
		}
		gripController.SetPose(gripPose, forcePose);
	}

	private void SynchronizeBindingPose()
	{
		if (IsHand)
		{
			if (hasContact && lastHandPose != handController.tipoDePose)
			{
				gripController.ResetIdlePose();
				lastHandPose = handController.tipoDePose;
			}
		}
		else if (lastHandPose != handController.tipoDePose)
		{
			gripController.ResetIdlePose();
			lastHandPose = handController.tipoDePose;
		}
		gripController.PoserEnabled = handController.tipoDePose == HandTipoDePose.massage && lastHandPose == HandTipoDePose.massage;
	}
}
