using UnityEngine;

namespace Parabox.Raytracer
{
	public struct Ray
	{
		public Vector3 origin;
		public Vector3 direction;

		/**
		 * Create a ray from an origin and direction.
		 * @origin is a postion.
		 * @direction is assumed to be a unit vector.
		 */
		public Ray(Vector3 origin, Vector3 direction)
		{
			this.origin = origin;
			this.direction = direction;
		}

		public Vector3 PointAtParameter(float p)
		{
			return origin + direction * p;
		}
	}
}
