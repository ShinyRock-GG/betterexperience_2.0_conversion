using Assets._ReusableScripts.CuchiCuchi;
using Assets._ReusableScripts.CuchiCuchi.Dependentes.Characters.Controlladores;
using Assets._ReusableScripts.CuchiCuchi.Dependentes.Controllers;
using Assets._ReusableScripts.CuchiCuchi.PhysicsAndBonesScripts;
using BepInEx.Configuration;
using BetterExperience.Features.PluginOptions;
using BetterExperience.GameScopes;
using UnityEngine;

namespace BetterExperience.Features;

internal class DragControl : PluginFeature
{
	private class DragControlService : SessionService
	{
		private class DragContext
		{
			public Vector3 motionAxis;

			public float thrust;
		}

		private DragContext context;

		private PelvisMovementController controller;

		private HandControllerV2 handController;

		private bool CanDrag => !handController.handEstaSiendoUsada;

		public override void OnStart()
		{
			base.OnStart();
			Lookup<DispatcherService>().DoUpdate.Add(HandleInput, base.Scope);
			controller = base.Session.Player.GameObject.GetComponentInChildren<PelvisMovementController>();
			handController = base.Session.Player.GameObject.GetComponentInChildren<HandControllerV2>();
		}

		private Vector3 GetMouseMotion()
		{
			return new Vector3(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
		}

		private void ApplyThrust()
		{
			float num = context.thrust * Time.deltaTime;
			context.thrust -= num;
			Penis pene = base.Session.Player.Character.pene;
			if (pene.isPenetrating)
			{
				BoneStretchedChain boneStretchedChain = pene.TryGetPenetratingHole();
				if (boneStretchedChain != null && boneStretchedChain.penetraciones.currentHits.count < 4 && num < 0f)
				{
					num = 0f;
				}
				num = Mathf.Sign(num) * Mathf.Min(7f, Mathf.Abs(num));
			}
			controller.ControlProfundidadDelta(num * 0.05f);
		}

		private void HandleInput()
		{
			if (Input.GetMouseButton(0) && CanDrag)
			{
				if (context != null)
				{
					Vector3 mouseMotion = GetMouseMotion();
					Vector3 a = Vector3.Scale(mouseMotion, context.motionAxis);
					Vector3 vector = Vector3.Scale(a, context.motionAxis);
					if (a.sqrMagnitude >= (mouseMotion - vector).sqrMagnitude)
					{
						float num = a.x + a.y + a.z;
						num /= Time.deltaTime;
						num *= 0.01f;
						context.thrust += num;
					}
					ApplyThrust();
					return;
				}
				context = new DragContext();
				Vector3 position = base.Session.Player.Character.pene.root.position;
				Vector3 position2 = ((Penetrador)base.Session.Player.Character.pene).tip.position;
				Vector3 vector2 = base.Session.MainCamera.WorldToScreen(position);
				Vector3 vector3 = base.Session.MainCamera.WorldToScreen(position2);
				Vector3 normalized = (vector2 - vector3).normalized;
				if (Mathf.Abs(normalized.x) > Mathf.Abs(normalized.y))
				{
					if (normalized.x > 0f)
					{
						context.motionAxis = Vector3.left;
					}
					else
					{
						context.motionAxis = Vector3.right;
					}
				}
				else
				{
					context.motionAxis = Vector3.up;
				}
			}
			else
			{
				context = null;
			}
		}
	}

	private ConfigEntry<bool> enableFeature;

	public override bool Enabled => enableFeature.Value;

	public override void Configure(ConfigFile config)
	{
		enableFeature = config.Bind<bool>("Features", "EnableDragControl", false, "Enable DragControl: Move pelvis with LMB drag");
	}

	public override void OnInit()
	{
		base.OnInit();
		Lookup<PluginOptionsService>().Expose(enableFeature, base.Scope);
	}

	public override void OnStart()
	{
		Lookup<SessionTracker>().InterviewServices.Add(() => new DragControlService());
	}
}
