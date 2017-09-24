using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Parabox.Raytracer
{
	/**
	 * An object that is visible to the raytracer camera.
	 */
	public abstract class Hittable : MonoBehaviour
	{
		public abstract RayHit DoRaycast(Ray ray, float min, float max, ref HitRecord record);
	}
}
