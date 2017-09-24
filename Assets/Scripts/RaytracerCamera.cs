#define RAY_DEBUG

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Ray = Parabox.Raytracer.Ray;

namespace Parabox.Raytracer
{
	public class RaytracerCamera : MonoBehaviour
	{
		public Color32 clearColor = new Color32(255, 255, 255, 255);
		private static readonly Color32 COLOR_WHITE = new Color32(255, 255, 255, 255);
		private static readonly Color32 COLOR_SKY_BLUE = new Color32(127, 200, 255, 255);

		private Camera m_Camera;
		private Camera m_OrthoCamera;
		private GameObject m_Quad;
		private Texture2D m_RenderTexture;
		private Color32[] m_ColorBuffer;
		// Driven by camera
		private int m_Width = 0, m_Height = 0;
		private float m_FrameRate = 1f;
		private Vector3 m_SceneLowerLeft;
		private float m_SceneWidth;
		private float m_SceneHeight;
		private Hittable[] m_Hittables = null;

		private void Awake()
		{
			m_Quad = GetComponentsInChildren<MeshFilter>().First(x => x.gameObject.name.Equals("Billboard")).gameObject;
			m_OrthoCamera = GetComponentsInChildren<Camera>().First(x => x.orthographic);
			m_Camera = GetComponent<Camera>();

			Assert.IsNotNull(m_Quad);
			Assert.IsNotNull(m_Camera);
			Assert.IsNotNull(m_OrthoCamera);

			m_Hittables = FindObjectsOfType<Hittable>();

			Debug.Log("hittables: " + m_Hittables.Length);

			InvokeRepeating("Render", 0f, m_FrameRate);
		}

#if RAY_DEBUG
		private Rect m_DebugWindowRect = new Rect(4, 4, 350, 400);

		private void OnGUI()
		{
			m_DebugWindowRect = GUILayout.Window("m_DebugWindow".GetHashCode(), m_DebugWindowRect, DebugWindow, "Debug Window");
		}

		private void DebugWindow(int id)
		{
			GUILayout.Label("m_SceneLowerLeft: " + m_SceneLowerLeft.ToString());
			GUILayout.Label("m_SceneWidth: " + m_SceneWidth);
			GUILayout.Label("m_SceneHeight: " + m_SceneHeight);
		}
#endif

		private void ClearBuffer()
		{
#if RAY_DEBUG
			for(int y = 0; y < m_Height; y++)
			{
				for(int x = 0; x < m_Width; x++)
				{
					byte r = (byte) Mathf.Clamp( ((x / (float) m_Width ) * 255f), 0, 255);
					byte g = (byte) Mathf.Clamp( ((y / (float) m_Height) * 255f), 0, 255);
					byte b = (byte) Mathf.Clamp( (.2f * 255f), 0, 255);
					m_ColorBuffer[y * m_Width + x] = new Color32(r, g, b, 255);
				}
			}
#else
			for(int i = 0; i < m_Width * m_Height; i++)
				m_ColorBuffer[i] = clearColor;
#endif
		}

		public void Render()
		{
			// Set quad size and camera dimensions
			SetupCamera();

			// ClearBuffer();

			// Do ray
			DoRays();

			// Render texture
			BlitColorBuffer();
		}

		private Color32 GetColor(Ray ray)
		{
			// Sphere
			HitRecord record = new HitRecord();
			RayHit hit = new RayHit();

			foreach(Hittable h in m_Hittables)
				hit = h.DoRaycast(ray, 0f, 0f, ref record);

			if(hit.IsValid())
			{
				return new Color32(
					(byte) Mathf.Clamp(Mathf.Abs(hit.normal.x) * 255f, 0, 255),
					(byte) Mathf.Clamp(Mathf.Abs(hit.normal.y) * 255f, 0, 255),
					(byte) Mathf.Clamp(Mathf.Abs(hit.normal.z) * 255f, 0, 255),
					(byte) 255 );
			}

			// Sky
			float t = (ray.direction.y + (m_SceneHeight * .5f)) / m_SceneHeight;
			return Color32.Lerp(COLOR_WHITE, COLOR_SKY_BLUE, t);
		}

		private void DoRays()
		{
			Ray ray = new Ray(Vector3.zero, Vector3.zero);

			for(int y = 0; y < m_Height; y++)
			{
				for(int x = 0; x < m_Width; x++)
				{
					float u = x / (float) m_Width;
					float v = y / (float) m_Height;
					ray.direction = new Vector3(m_SceneLowerLeft.x + (u * m_SceneWidth), m_SceneLowerLeft.y + (v * m_SceneHeight), m_SceneLowerLeft.z);
					m_ColorBuffer[y * m_Width + x] = GetColor(ray);
				}
			}
		}

		private void SetupCamera()
		{
			int w = (int) m_OrthoCamera.pixelRect.width;
			int h = (int) m_OrthoCamera.pixelRect.height;

			if(w == m_Width && h == m_Height)
				return;

			m_Width = w;
			m_Height = h;

			m_ColorBuffer = new Color32[m_Width * m_Height];

			ClearBuffer();

			if(m_RenderTexture != null)
				Object.DestroyImmediate(m_RenderTexture);

			m_RenderTexture = new Texture2D(m_Width, m_Height, TextureFormat.RGBA32, false, true);

			m_Quad.GetComponent<MeshRenderer>().sharedMaterial.mainTexture = m_RenderTexture;

			Vector3 bl = m_OrthoCamera.ScreenToWorldPoint(Vector3.zero);
			Vector3 tr = m_OrthoCamera.ScreenToWorldPoint(new Vector3(m_OrthoCamera.pixelWidth, m_OrthoCamera.pixelHeight, 1f));

			m_Quad.transform.localScale = new Vector3(tr.x - bl.x, tr.y - bl.y);

			bl = m_Camera.ScreenToWorldPoint(new Vector3(0f, 0f, 1f));
			tr = m_Camera.ScreenToWorldPoint(new Vector3(m_Camera.pixelWidth, m_Camera.pixelHeight, 1f));

			m_SceneLowerLeft = bl;
			m_SceneWidth = tr.x - bl.x;
			m_SceneHeight = tr.y - bl.y;

		}

		private void BlitColorBuffer()
		{
			m_RenderTexture.SetPixels32(m_ColorBuffer);
			m_RenderTexture.Apply();
		}
	}
}
