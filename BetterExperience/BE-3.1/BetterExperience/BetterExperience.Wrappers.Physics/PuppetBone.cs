using System.Collections.Generic;
using UnityEngine;

namespace BetterExperience.Wrappers.Physics;

internal class PuppetBone
{
	public Transform Transform { get; set; }

	public MeshCollider MeshCollider { get; set; }

	public CapsuleCollider CapsuleCollider { get; set; }

	public BoxCollider BoxCollider { get; set; }

	public SphereCollider SphereCollider { get; set; }

	public List<Collider> Colliders { get; private set; } = new List<Collider>();

	public PuppetBone(Transform transform, List<Collider> colliders)
	{
		Transform = transform;
		foreach (Collider collider in colliders)
		{
			if (collider is MeshCollider)
			{
				MeshCollider = (MeshCollider)collider;
			}
			else if (collider is CapsuleCollider)
			{
				CapsuleCollider = (CapsuleCollider)collider;
			}
			else if (collider is BoxCollider)
			{
				BoxCollider = (BoxCollider)collider;
			}
			else if (collider is SphereCollider)
			{
				SphereCollider = (SphereCollider)collider;
			}
			Colliders.Add(collider);
		}
	}
}
