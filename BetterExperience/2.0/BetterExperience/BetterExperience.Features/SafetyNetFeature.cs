using System;
using System.Collections;
using System.Collections.Generic;
using Assets._ReusableScripts.CuchiCuchi.AI;
using Assets._ReusableScripts.CuchiCuchi.AI.Emociones;
using Assets._ReusableScripts.CuchiCuchi.Chars.Alteradores;
using Assets._ReusableScripts.CuchiCuchi.Chars.Alteradores.Holders;
using Assets._ReusableScripts.CuchiCuchi.PhysicsAndBonesScripts;
using Assets._ReusableScripts.CuchiCuchi.Skins;
using Assets._ReusableScripts.PhysicsScripts;
using BetterExperience.GameScopes;
using BetterExperience.Wrappers.Characters;
using HarmonyLib;
using UnityEngine;

namespace BetterExperience.Features;

internal class SafetyNetFeature : PluginFeature
{
	public class SafetyNetService : SessionService
	{
		private class ManagedHitSkin
		{
			private RecalculableJointBase joint;

			public SkinnedMeshRenderer smr { get; }

			public JointBodyAdmin admin { get; }

			public ManagedHitSkin(BaseDeTetaSkin l)
			{
				smr = l.skinnedMeshRenderer;
				admin = l.recalculableJoint.bodyAdmin;
				joint = l.recalculableJoint;
				l.rigid.maxLinearVelocity = 1f;
				l.rigid.maxDepenetrationVelocity = 1f;
				if (admin.joint.xMotion == ConfigurableJointMotion.Free)
				{
					admin.joint.xMotion = ConfigurableJointMotion.Limited;
				}
				if (admin.joint.yMotion == ConfigurableJointMotion.Free)
				{
					admin.joint.yMotion = ConfigurableJointMotion.Limited;
				}
				if (admin.joint.zMotion == ConfigurableJointMotion.Free)
				{
					admin.joint.zMotion = ConfigurableJointMotion.Limited;
				}
				SoftJointLimit limit = admin.joint.linearLimit;
				limit.limit = 0.5f;
				admin.joint.linearLimit = limit;
			}

			public ManagedHitSkin(MedioDeTetaSkin l)
			{
				smr = l.skinnedMeshRenderer;
				admin = l.recalculableJoint.bodyAdmin;
				joint = l.recalculableJoint;
			}

			internal bool IsExploded()
			{
				Vector3 sz = smr.bounds.size;
				float volume = sz.x * sz.y * sz.z;
				if (volume > 1f)
				{
					return true;
				}
				return false;
			}

			internal void Recover()
			{
				joint.FixAdmins();
				admin.KillForces();
			}
		}

		private List<ManagedHitSkin> hitskins = new List<ManagedHitSkin>();

		private List<AlteratorModifier> modifiers = new List<AlteratorModifier>();

		private List<AlteradorDeScalaDeBone> alteradors = new List<AlteradorDeScalaDeBone>();

		private PlacerBase placer;

		public override void OnStart()
		{
			base.OnStart();
			FemaleSkins skins = base.Session.Guest.Impl.GetComponentInChildren<FemaleSkins>();
			AlteracionesDeMeshDeSenosGeneral gen = base.Session.Guest.Impl.GetComponentInChildren<AlteracionesDeMeshDeSenosGeneral>();
			EmocionesFemeninas emotionsComponent = base.Session.Guest.Impl.GetComponentInChildren<EmocionesFemeninas>();
			placer = emotionsComponent.placer;
			AlteradorDeScalaDeBone a = Traverse.Create((object)gen).Field<AlteradorDeScalaDeBone>("scaler_R").Value;
			if (a != null)
			{
				alteradors.Add(a);
			}
			a = Traverse.Create((object)gen).Field<AlteradorDeScalaDeBone>("scaler_L").Value;
			if (a != null)
			{
				alteradors.Add(a);
			}
			hitskins.Add(new ManagedHitSkin(skins.hitSkins.partes.senos000.l));
			hitskins.Add(new ManagedHitSkin(skins.hitSkins.partes.senos000.r));
			hitskins.Add(new ManagedHitSkin(skins.hitSkins.partes.senos001.l));
			hitskins.Add(new ManagedHitSkin(skins.hitSkins.partes.senos001.r));
			modifiers.Add(base.Session.Guest.ModifierManager.Modifiers[DiccionarioDeNombresDeAlteradoresFemeninos.Scaler_Seno_R]);
			modifiers.Add(base.Session.Guest.ModifierManager.Modifiers[DiccionarioDeNombresDeAlteradoresFemeninos.Scaler_Seno_L]);
			Lookup<DispatcherService>().StartCoroutine(CheckLoop(), base.Scope);
		}

		private IEnumerator CheckLoop()
		{
			while (base.Scope.Started)
			{
				if (Allowed() && Any(hitskins, (ManagedHitSkin x) => x.IsExploded()))
				{
					hitskins.ForEach(delegate(ManagedHitSkin x)
					{
						x.Recover();
					});
					modifiers.ForEach(delegate(AlteratorModifier x)
					{
						x.Invalidate();
					});
					logger.Error("recover {0}", alteradors.Count);
					foreach (AlteradorDeScalaDeBone a in alteradors)
					{
						a.flagForceUpdate = true;
					}
				}
				yield return new WaitForSeconds(0.1f);
			}
		}

		private bool Allowed()
		{
			return !placer.valueAtMax;
		}

		private bool Any(List<ManagedHitSkin> hitskins, Func<ManagedHitSkin, bool> p)
		{
			foreach (ManagedHitSkin hs in hitskins)
			{
				if (p(hs))
				{
					return true;
				}
			}
			return false;
		}
	}

	public override bool Enabled => true;

	public override void OnStart()
	{
		base.OnStart();
		Lookup<SessionTracker>().InterviewServices.Add(() => new SafetyNetService());
	}
}
