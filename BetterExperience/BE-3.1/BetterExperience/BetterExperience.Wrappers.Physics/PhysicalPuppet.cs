using System;
using System.Collections.Generic;
using System.Linq;
using Assets;
using Assets._ReusableScripts.CuchiCuchi.Dependentes.Puppet;
using Assets._ReusableScripts.CuchiCuchi.Skins;
using Assets._ReusableScripts.PhysicsScripts;
using Assets.TValle.BeachGirl;
using BetterExperience.GameScopes;
using BetterExperience.Utils;
using HarmonyLib;
using RootMotion.Dynamics;
using UnityEngine;

namespace BetterExperience.Wrappers.Physics;

public class PhysicalPuppet
{
	private static List<Collider> EMPTY_COLLIDER_LIST = new List<Collider>();

	private Logger logger = new Logger();

	private ICharacter character;

	private PuppetPartes puppetPartsInfo;

	private PuppetMaster puppetMaster;

	private Dictionary<PuppetPart, PuppetSenderPart> bodyPartMapping = new Dictionary<PuppetPart, PuppetSenderPart>();

	public Observable<PuppetSenderPart, ColisionPhysicaV2> OnCollisionStay = new Observable<PuppetSenderPart, ColisionPhysicaV2>();

	public Observable<HitSkin, Collision> OnSkinCollisionStay = new Observable<HitSkin, Collision>();

	private Dictionary<string, List<Collider>> colliderNames = new Dictionary<string, List<Collider>>();

	private Dictionary<string, PuppetBone> bones = new Dictionary<string, PuppetBone>();

	private HashSet<Collider> myColliders = new HashSet<Collider>();

	private Dictionary<Transform, Transform> ikBones = new Dictionary<Transform, Transform>();

	private GameObject root;

	public List<PuppetPart> BodyParts { get; private set; }

	public List<HitSkin> Skins { get; }

	public Observable OnIKsUpdated { get; } = new Observable();

	public PuppetMaster PuppetMaster => puppetMaster;

	public GameObject Root => root;

	public PhysicalPuppet(GameObject root)
	{
		logger.Prefix = "[ Puppet " + root.name + "]";
		BodyParts = new List<PuppetPart>(root.GetComponentsInChildren<PuppetPart>());
		foreach (PuppetPart part in BodyParts)
		{
			part.collisionStay += delegate(PuppetPart.PartColision c)
			{
				OnPartCollision(part, c);
			};
			PuppetParte value = Traverse.Create((object)part).Field("m_parte").GetValue<PuppetParte>();
			bodyPartMapping[part] = (PuppetSenderPart)value;
		}
		Skins = new List<HitSkin>(root.GetComponentsInChildren<HitSkin>());
		foreach (HitSkin hs in Skins)
		{
			hs.onCollisionStay += delegate(Collision c)
			{
				OnSkinCollisionStay.Invoke(hs, c);
			};
		}
		character = root.GetComponentInChildren<ICharacter>();
		puppetPartsInfo = root.GetComponentInChildren<PuppetPartes>();
		puppetMaster = root.GetComponentInChildren<PuppetMaster>();
		Collider[] componentsInChildren = root.GetComponentsInChildren<Collider>();
		foreach (Collider collider in componentsInChildren)
		{
			colliderNames.GetValueOrAdd(collider.name, () => new List<Collider>()).Add(collider);
			myColliders.Add(collider);
		}
		foreach (KeyValuePair<string, List<Collider>> colliderName in colliderNames)
		{
			Transform transform = colliderName.Value[0].transform;
			bones[transform.name] = new PuppetBone(transform, colliderName.Value);
		}
		root.GetComponent<IIKUpdater>().onPhysicsIKUpdated += Updater_onPhysicsIKUpdated;
		this.root = root;
	}

	private void Updater_onPhysicsIKUpdated(IIKUpdater obj)
	{
		foreach (KeyValuePair<Transform, Transform> ikBone in ikBones)
		{
			ikBone.Value.position = ikBone.Key.position;
			ikBone.Value.rotation = ikBone.Key.rotation;
		}
		OnIKsUpdated.Invoke();
	}

	public bool ContainsCollider(Collider c)
	{
		return myColliders.Contains(c);
	}

	public List<Collider> ColliderByName(string name)
	{
		return Extensions.GetValueOrDefault(colliderNames, name, EMPTY_COLLIDER_LIST);
	}

	public Transform GetIKBoneTransform(HumanBodyBones bone)
	{
		Transform boneTransform = GetBoneTransform(bone);
		if (boneTransform == null)
		{
			return null;
		}
		if (!ikBones.TryGetValue(boneTransform, out var value))
		{
			value = UnityUtils.NewTransform("IK_" + boneTransform.name, root.transform);
			ikBones[boneTransform] = value;
		}
		return value;
	}

	public Transform GetIKBoneTransform(Transform b)
	{
		if (!ikBones.TryGetValue(b, out var value))
		{
			value = UnityUtils.NewTransform("IK_" + b.name, root.transform);
			ikBones[b] = value;
		}
		return value;
	}

	public Transform GetBoneTransform(HumanBodyBones bone)
	{
		return character.bodyAnimator.GetBoneTransform(bone);
	}

	public Transform GetBoneTransform(string boneName)
	{
		return character.bodyAnimator.GetBoneTransform(boneName);
	}

	public List<Collider> GetBoneColliders(HumanBodyBones bone)
	{
		Transform boneTransform = GetBoneTransform(bone);
		return ColliderByName(boneTransform.name);
	}

	public Muscle GetMuscle(HumanBodyBones bone)
	{
		Muscle muscle = puppetMaster.GetMuscle(bone);
		if (muscle == null)
		{
			logger.Warn("No muscle for {0}", bone);
		}
		return muscle;
	}

	private void OnPartCollision(PuppetPart part, PuppetPart.PartColision c)
	{
		OnCollisionStay.Invoke((PuppetSenderPart)c.parte, c);
	}

	public PuppetSenderPart? GetBodyPart(Rigidbody sender)
	{
		if (sender == null)
		{
			return null;
		}
		foreach (PuppetPart bodyPart in BodyParts)
		{
			if (CollideUsing(bodyPart.rigid, sender))
			{
				bodyPartMapping.TryGetValue(bodyPart, out var value);
				return value;
			}
		}
		return null;
	}

	internal Rigidbody GetDraggableRigidbody(PuppetReceiverPart grabPart)
	{
		if (Constants.skinToBoneMap.TryGetValue(grabPart, out var value))
		{
			Muscle muscle = GetMuscle(value);
			if (muscle != null)
			{
				return muscle.rigidbody;
			}
		}
		return null;
	}

	public void SetLimbRelaxed(PuppetReceiverPart part, bool relaxed)
	{
		if (!Constants.skinToBoneMap.TryGetValue(part, out var bone))
		{
			return;
		}
		HumanBodyBones[] array = Constants.limbs.FirstOrDefault((HumanBodyBones[] x) => Array.IndexOf(x, bone) != -1);
		if (array == null)
		{
			return;
		}
		HumanBodyBones[] array2 = array;
		foreach (HumanBodyBones humanBodyBone in array2)
		{
			if (relaxed)
			{
				puppetMaster.SetMuscleWeights(humanBodyBone, 0.1f, 0f, 1f, 10f);
			}
			else
			{
				puppetMaster.SetMuscleWeights(humanBodyBone, 1f);
			}
		}
	}

	private bool CollideUsing(Rigidbody who, Rigidbody with)
	{
		if (!(who == with) && !(who.transform == with.transform))
		{
			return with.transform.IsChildOf(who.transform);
		}
		return true;
	}
}
