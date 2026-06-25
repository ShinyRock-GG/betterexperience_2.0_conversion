using System;
using Assets;
using Assets._ReusableScripts.Globales.Mapas;
using BepInEx.Configuration;
using BetterExperience.Wrappers.Characters;
using UnityEngine;

namespace BetterExperience.Features.SceneCameras;

internal class CameraRig : IDisposable
{
	private const float ACCEPTABLE_PIVOT_RAYCAST_DISTANCE = 2f;

	private const float ROTATION_SMOOTHNESS = 5000000f;

	private const float TRANSLATION_SMOOTHNESS = 10f;

	private const float MINIMAL_LINECAST_DISTANCE = 0f;

	private const float PIVOT_LINECAST_PRECISSION = 0f;

	private Logger logger = new Logger("Camera Rig");

	private PlayerCharacter player;

	private Camera mainCamera;

	private GameObject shadowGo;

	private Camera shadowCamera;

	private int cameraRaycastLayerMask;

	private CameraSettings currentSettings;

	private Transform rigTransform;

	public Transform RootTransform { get; }

	public Transform ConnectorTransform { get; }

	public Transform PivotTransform { get; }

	public ConfigEntry<bool> UseCameraLinecast { get; set; }

	public ConfigEntry<bool> UseRagdollPivot { get; set; }

	private Transform CameraTarget { get; set; }

	public ConfigEntry<bool> UseDetachedRig { get; internal set; }

	private Transform NewTransform(string name, Transform parent)
	{
		GameObject gameObject = new GameObject(name);
		gameObject.transform.parent = parent;
		return gameObject.transform;
	}

	public CameraRig(PlayerCharacter player)
	{
		this.player = player;
		mainCamera = Camera.main;
		rigTransform = NewTransform("Shadow Rig", player.Character.animatorRootMotionTransform);
		RootTransform = NewTransform("Shadow Root", rigTransform);
		ConnectorTransform = NewTransform("Shadow Root Connector", RootTransform);
		shadowGo = UnityEngine.Object.Instantiate(Camera.main.gameObject, ConnectorTransform);
		shadowCamera = shadowGo.GetComponent<Camera>();
		shadowCamera.enabled = false;
		shadowCamera.name = "Shadow Camera";
		shadowGo.SetActive(value: false);
		ConfiguracionGlobal.Layers layers = MapaSingleton<ConfiguracionGlobal>.instance.layers;
		cameraRaycastLayerMask = layers.skins.ToLayerMask() | layers.convexSkins.ToLayerMask() | layers.ragdoll.ToLayerMask();
		Component[] componentsInChildren = shadowGo.GetComponentsInChildren<Component>();
		foreach (Component component in componentsInChildren)
		{
			if (component is Rigidbody)
			{
				UnityEngine.Object.Destroy(component);
			}
			else if (component is SphereCollider && component.name == "Collider")
			{
				UnityEngine.Object.Destroy(component);
			}
		}
		PivotTransform = NewTransform("Shadow Pivot", ConnectorTransform);
		CameraTarget = NewTransform("Camera Target", ConnectorTransform);
		PivotTransform.gameObject.SetActive(value: false);
	}

	public void SetShadowCameraEnabled(bool state)
	{
		if (shadowCamera.enabled != state)
		{
			shadowCamera.enabled = state;
			mainCamera.enabled = !state;
			shadowGo.SetActive(state);
			if (state)
			{
				shadowCamera.transform.position = mainCamera.transform.position;
				shadowCamera.transform.rotation = mainCamera.transform.rotation;
				CameraTarget.position = shadowCamera.transform.position;
				CameraTarget.rotation = shadowCamera.transform.rotation;
			}
		}
	}

	internal void Translate(Vector3 translate)
	{
		CameraTarget.transform.Translate(translate);
	}

	public void UpdateDistanceFactor(float dv)
	{
		float value = Vector3.Distance(PivotTransform.position, CameraTarget.position) + dv;
		value = Mathf.Clamp(value, 0.1f, 2f);
		Vector3 vector = new Vector3(0f, 0f, 0f - value);
		Vector3 position = PivotTransform.position + CameraTarget.rotation * vector;
		CameraTarget.position = position;
	}

	public CameraSettings GetSettings()
	{
		CameraSettings cameraSettings = new CameraSettings();
		Transform transform = shadowCamera.transform;
		cameraSettings.Position = new float[3]
		{
			transform.localPosition.x,
			transform.localPosition.y,
			transform.localPosition.z
		};
		cameraSettings.Rotation = new float[4]
		{
			transform.localRotation.x,
			transform.localRotation.y,
			transform.localRotation.z,
			transform.localRotation.w
		};
		if (currentSettings != null)
		{
			cameraSettings.Name = currentSettings.Name;
		}
		return cameraSettings;
	}

	public void SetSettings(CameraSettings settings, bool pivotEnabled)
	{
		if (settings != null)
		{
			Transform transform = shadowCamera.transform;
			transform.localPosition = new Vector3(settings.Position[0], settings.Position[1], settings.Position[2]);
			transform.localRotation = new Quaternion(settings.Rotation[0], settings.Rotation[1], settings.Rotation[2], settings.Rotation[3]);
			CameraTarget.localPosition = transform.localPosition;
			CameraTarget.localRotation = transform.localRotation;
		}
		PivotTransform.gameObject.SetActive(pivotEnabled);
		if (pivotEnabled)
		{
			UpdatePivot();
		}
		else
		{
			SetRootMotionEnabled(state: true);
		}
		currentSettings = settings;
	}

	private void SetRootMotionEnabled(bool state)
	{
		if (!UseDetachedRig.Value)
		{
			state = true;
		}
		if (state)
		{
			ConnectorTransform.parent = RootTransform.parent;
		}
		else
		{
			ConnectorTransform.parent = null;
		}
	}

	public void SetPivot(Vector3 anchor, bool rootMotion)
	{
		PivotTransform.position = anchor;
		SetRootMotionEnabled(rootMotion);
	}

	public void Rotate2D(float dx, float dy)
	{
		Rotate2DInternal(dx, dy);
	}

	public void PivotRotate2D(float dx, float dy)
	{
		RotateAround2D(PivotTransform.position, dx, dy);
	}

	private void RotateAround2D(Vector3 anchor, float dx, float dy)
	{
		Transform cameraTarget = CameraTarget;
		float num = Vector3.Distance(PivotTransform.position, CameraTarget.position);
		Rotate2DInternal(dx, dy);
		Vector3 vector = new Vector3(0f, 0f, 0f - num);
		Vector3 position = cameraTarget.rotation * vector + anchor;
		CameraTarget.position = position;
	}

	internal void Rotate2DInternal(float dx, float dy)
	{
		Vector3 euler = CurrentEuler();
		euler.y += dx;
		euler.x += dy;
		euler.x = Mathf.Clamp(euler.x, -89f, 89f);
		CameraTarget.localRotation = Quaternion.Slerp(CameraTarget.localRotation, Quaternion.Euler(euler), 5000000f * Time.deltaTime);
	}

	private Vector3 CurrentEuler()
	{
		Vector3 rEuler = CameraTarget.localEulerAngles;
		NormalizeEuler(ref rEuler);
		return rEuler;
	}

	public void NormalizeEuler(ref Vector3 rEuler)
	{
		if (rEuler.x < -180f)
		{
			rEuler.x += 360f;
		}
		else if (rEuler.x > 180f)
		{
			rEuler.x -= 360f;
		}
		if (rEuler.y < -180f)
		{
			rEuler.y += 360f;
		}
		else if (rEuler.y > 180f)
		{
			rEuler.y -= 360f;
		}
	}

	public bool RagdollRaycast(out Vector3 point)
	{
		if (Physics.Raycast(new Ray(CameraTarget.position, CameraTarget.forward), out var hitInfo, 2f, cameraRaycastLayerMask))
		{
			point = hitInfo.point;
			return true;
		}
		point = Vector3.zero;
		return false;
	}

	private (Vector3, bool) ComputeRotationPivot()
	{
		if (UseRagdollPivot.Value && RagdollRaycast(out var point))
		{
			return (point, false);
		}
		Plane plane = new Plane(RootTransform.right, RootTransform.position);
		Plane plane2 = new Plane(RootTransform.up, player.Character.pene.lookAtTarget.position);
		Ray ray = new Ray(CameraTarget.position, CameraTarget.forward);
		if (plane.Raycast(ray, out var enter) && enter <= 2f)
		{
			return (ray.GetPoint(enter), true);
		}
		if (plane2.Raycast(ray, out enter) && enter <= 2f)
		{
			return (ray.GetPoint(enter), true);
		}
		return (RootTransform.position, true);
	}

	public void Dispose()
	{
		if (rigTransform != null)
		{
			if (ConnectorTransform.parent == null)
			{
				UnityEngine.Object.Destroy(ConnectorTransform.gameObject);
			}
			UnityEngine.Object.Destroy(rigTransform.gameObject);
			rigTransform = null;
		}
	}

	internal void UpdatePivot()
	{
		var (anchor, rootMotion) = ComputeRotationPivot();
		SetPivot(anchor, rootMotion);
	}

	internal void SyncRootTransform()
	{
		Transform transform = player.Character.bones.head.transform;
		if (RootTransform.position != transform.position)
		{
			RootTransform.position = transform.position;
			if (PivotTransform.gameObject.activeSelf)
			{
				PivotRotate2D(0f, 0f);
			}
		}
		if (CameraTarget.localRotation != shadowCamera.transform.localRotation)
		{
			shadowCamera.transform.localRotation = CameraTarget.localRotation;
		}
		if (!(CameraTarget.localPosition != shadowCamera.transform.localPosition))
		{
			return;
		}
		if (PivotTransform.gameObject.activeSelf)
		{
			float num = Vector3.Distance(PivotTransform.position, CameraTarget.position);
			if (UseCameraLinecast.Value && Physics.Linecast(PivotTransform.position, CameraTarget.position, out var hitInfo, cameraRaycastLayerMask) && hitInfo.distance > 0f)
			{
				num = hitInfo.distance;
			}
			Vector3 vector = new Vector3(0f, 0f, 0f - num);
			Vector3 vector2 = CameraTarget.rotation * vector + PivotTransform.position;
			if (!ExtendedMonoBehaviour.AlmostEqual(shadowCamera.transform.position, vector2, 0f))
			{
				shadowCamera.transform.position = Vector3.Lerp(shadowCamera.transform.position, vector2, Time.deltaTime * 10f);
			}
		}
		else
		{
			shadowCamera.transform.position = Vector3.Lerp(shadowCamera.transform.position, CameraTarget.position, Time.deltaTime * 10f);
		}
	}
}
