using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Parabox.Raytracer
{
	public class SphereHittable : Hittable
	{
		public float m_Radius = 1f;
		public Vector3 m_Center = new Vector3(0f, 0f, 5f);

		public override RayHit DoRaycast(Ray ray, float min, float max, ref HitRecord record)
		{
			Vector3 oc = ray.origin - m_Center;
			float a = Vector3.Dot(ray.direction, ray.direction);
			float b = Vector3.Dot(oc, ray.direction) * 2f;
			float c = Vector3.Dot(oc, oc) - (m_Radius * m_Radius);
			float d = b*b - 4*a*c;
			if(d < 0f)
				return RayHit.Empty;
			float param = (-b - Mathf.Sqrt(d)) / (2f * a);
			Vector3 point = m_Center - ray.PointAtParameter(param);
			return new RayHit(param, point, point);
		}
	}
}
