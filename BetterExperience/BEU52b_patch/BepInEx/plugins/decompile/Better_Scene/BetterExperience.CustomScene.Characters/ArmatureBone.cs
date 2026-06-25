using System;
using BetterExperience.GameScopes;
using RootMotion.Dynamics;
using UnityEngine;

namespace BetterExperience.CustomScene.Characters;

public class ArmatureBone : MonoBehaviour
{
	private LineRenderer lineRenderer;

	private ScopeSupport boneScope = new ScopeSupport();

	private Vector3 offset = Vector3.right;

	public Transform Target { get; set; }

	public Armature Armature { get; set; }

	public Muscle Muscle { get; internal set; }

	public ArmatureBone()
	{
		((BaseObservable<Action<bool>>)(object)Armature.DrawArmature).Add((Action<bool>)delegate
		{
			UpdateDrawArmatureState();
		}, boneScope);
	}

	internal void MapToRecursive(float dt)
	{
		Target.rotation = Quaternion.RotateTowards(Target.rotation, base.transform.rotation, Armature.RotationVelocity * dt);
		Target.position = Vector3.MoveTowards(Target.position, base.transform.position, Armature.TranslationVelocity * dt);
		for (int i = 0; i < base.transform.childCount; i++)
		{
			Transform c = base.transform.GetChild(i);
			c.GetComponent<ArmatureBone>().MapToRecursive(dt);
		}
	}

	internal void ReadRecursive()
	{
		if (Armature.RootBone == this || Armature.RootBone.Target == Target.parent)
		{
			base.transform.position = Target.position;
		}
		Quaternion localRot = Quaternion.Inverse(Armature.RootBone.Target.rotation) * Target.rotation;
		Quaternion rot = Armature.RootBone.Target.rotation * localRot;
		base.transform.rotation = rot;
		base.transform.localScale = Target.localScale;
		for (int i = 0; i < base.transform.childCount; i++)
		{
			Transform c = base.transform.GetChild(i);
			c.GetComponent<ArmatureBone>().ReadRecursive();
		}
	}

	private void OnDestroy()
	{
		boneScope.Dispose();
	}

	private void OnEnable()
	{
		UpdateDrawArmatureState();
	}

	private void UpdateDrawArmatureState()
	{
		if (!base.enabled)
		{
			return;
		}
		if (CanDrawArmature())
		{
			if (GetComponent<LineRenderer>() == null)
			{
				lineRenderer = base.gameObject.AddComponent<LineRenderer>();
				lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
				lineRenderer.startColor = Color.white;
				lineRenderer.endColor = Color.white;
				lineRenderer.startWidth = 0.01f;
				lineRenderer.endWidth = 0.05f;
				lineRenderer.alignment = LineAlignment.View;
			}
		}
		else
		{
			if ((bool)lineRenderer)
			{
				UnityEngine.Object.DestroyImmediate(lineRenderer);
			}
			lineRenderer = null;
		}
	}

	private bool CanDrawArmature()
	{
		if (base.transform.parent.GetComponent<ArmatureBone>() != null)
		{
			return Armature.DrawArmature.Value;
		}
		return false;
	}

	private void Update()
	{
		if (lineRenderer != null)
		{
			float dist = Vector3.Distance(base.transform.position, base.transform.parent.position);
			Vector3 vec = base.transform.parent.forward * dist;
			lineRenderer.SetPosition(0, base.transform.parent.position + vec + offset);
			lineRenderer.SetPosition(1, base.transform.parent.position + offset);
		}
	}
}
