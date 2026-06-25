using System.Collections;
using Assets._ReusableScripts.CuchiCuchi.Ropa;
using Assets._ReusableScripts.CuchiCuchi.Ropa.Glases;
using BetterExperience.GameScopes;
using HarmonyLib;
using RootMotion.Dynamics;
using UnityEngine;

namespace Better_Cloth;

public class ClothFeature : PluginFeature
{
	public class ClothRemovalService : SessionService
	{
		private PiezaDeRopaBase loader;

		private CapsuleCollider[] capsules;

		public Observable<ClothRemovalRequest> OnClothRemove { get; } = new Observable<ClothRemovalRequest>();

		public override void OnStart()
		{
			base.OnStart();
			ClothManagerHarmony.OnClothHidden.Add(OnPieceOfClothHidden, base.Scope);
			PuppetMaster puppet = base.Session.Guest.RootObject.GetComponentInChildren<PuppetMaster>();
			if (puppet != null)
			{
				capsules = puppet.transform.GetComponentsInChildren<CapsuleCollider>();
			}
			else
			{
				capsules = new CapsuleCollider[0];
			}
		}

		private void OnPieceOfClothHidden(PiezaDeRopaBase obj)
		{
			if ((Object)(object)loader == null)
			{
				loader = base.Session.Guest.RootObject.GetComponentInChildren<PiezaDeRopaBase>();
			}
			if ((Object)(object)loader != null && !((Behaviour)(object)obj).enabled)
			{
				Transform cloth = CloneCloth(obj);
				ClothRemovalRequest req = TransformToCloth(obj, cloth);
				OnClothRemove.Invoke(req);
				if (!req.Intercepted)
				{
					req.Proceed();
				}
			}
		}

		private Transform CloneCloth(PiezaDeRopaBase obj)
		{
			GameObject clone = Object.Instantiate(((Component)(object)obj).transform.gameObject);
			clone.transform.parent = ((Component)(object)obj).transform.parent;
			PiezaDeRopaBase ropa = clone.gameObject.GetComponent<PiezaDeRopaBase>();
			Object.DestroyImmediate((Object)(object)ropa);
			GlasesSkin glass = clone.gameObject.GetComponent<GlasesSkin>();
			if ((Object)(object)glass != null)
			{
				Object.DestroyImmediate((Object)(object)glass);
			}
			clone.gameObject.GetComponent<SkinnedMeshRenderer>().enabled = true;
			clone.name += " UCloth";
			return clone.transform;
		}

		private ClothRemovalRequest TransformToCloth(PiezaDeRopaBase obj, Transform dynamicCloth)
		{
			Cloth cloth = dynamicCloth.gameObject.AddComponent<Cloth>();
			cloth.damping = 0.7f;
			cloth.capsuleColliders = capsules;
			cloth.enabled = false;
			return new ClothRemovalRequest(obj, dynamicCloth.gameObject);
		}
	}

	public class ClothRemovalRequest
	{
		public PiezaDeRopaBase Cloth { get; }

		public GameObject UnityClothRoot { get; }

		public bool Intercepted { get; set; }

		public ClothRemovalRequest(PiezaDeRopaBase cloth, GameObject unityClothRoot)
		{
			Cloth = cloth;
			UnityClothRoot = unityClothRoot;
		}

		public void Proceed()
		{
			UnityClothRoot.GetComponent<Cloth>().enabled = true;
			((MonoBehaviour)(object)Cloth).StartCoroutine(DestroyLater(UnityClothRoot.gameObject));
		}

		private IEnumerator DestroyLater(GameObject go)
		{
			yield return new WaitForSeconds(10f);
			Object.DestroyImmediate(go);
		}
	}

	public override bool Enabled => true;

	public override void OnInit()
	{
		base.OnInit();
		Harmony.CreateAndPatchAll(typeof(ClothManagerHarmony), (string)null);
	}

	public override void OnStart()
	{
		base.OnStart();
		Lookup<SessionTracker>().InterviewServices.Add(() => new ClothRemovalService());
	}
}
