using UnityEngine;

namespace Parabox.Raytracer
{
	public struct RayHit
	{
		private static readonly Vector3 Vec3_Zero = Vector3.zero;

		public float parameter;
		public Vector3 point;
		public Vector3 normal;

		public static readonly RayHit Empty = new RayHit(-1f, Vec3_Zero, Vec3_Zero);

		public RayHit(float parameter, Vector3 point, Vector3 normal)
		{
			this.parameter = parameter;
			this.point = point;
			this.normal = normal;
		}

		public bool IsValid()
		{
			return parameter > 0f;
		}
	}
}
