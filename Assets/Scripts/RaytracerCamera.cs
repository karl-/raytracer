#define RAY_DEBUG

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Ray = Parabox.Raytracer.Ray;
#if RAY_DEBUG
using System.Diagnostics;
#endif

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
		private int m_Fov = 60;
		private float m_FrameRate = 1f;
		private Vector3 m_SceneLowerLeft;
		private float m_SceneWidth;
		private float m_SceneHeight;
		private Hittable[] m_Hittables = null;

		// Is anti-aliasing enabled?
		private bool m_EnableAA = false;
		// Anti-aliasing random number generator.
		private System.Random m_RandomDouble = new System.Random();
		// Number of random samples per-pixel when anti-aliasing is enabled.
		private uint m_AASamples = 8;
		private float m_InvAASamples = 1f / 8f;

		private void Awake()
		{
			m_Quad = GetComponentsInChildren<MeshFilter>().First(x => x.gameObject.name.Equals("Billboard")).gameObject;

			// Set up orthographic camera to actually render the scene
			GameObject orthoCam = new GameObject();
			orthoCam.name = "Raytracer Orthographic Camera";
			orthoCam.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
			orthoCam.transform.SetParent(transform, false);
			m_OrthoCamera = orthoCam.AddComponent<Camera>();
			m_OrthoCamera.orthographic = true;
			m_OrthoCamera.clearFlags = CameraClearFlags.SolidColor;
			m_OrthoCamera.orthographicSize = .5f;
			m_OrthoCamera.allowHDR = false;
			m_OrthoCamera.allowMSAA = false;

			m_Camera = GetComponent<Camera>();

			Assert.IsNotNull(m_Quad);
			Assert.IsNotNull(m_Camera);
			Assert.IsNotNull(m_OrthoCamera);

			m_InvAASamples = 1f / m_AASamples;

			m_Hittables = FindObjectsOfType<Hittable>();

			InvokeRepeating("Render", 0f, m_FrameRate);
		}

#if RAY_DEBUG
		private Stopwatch m_FrameTimer = new Stopwatch();
		private Rect m_DebugWindowRect = new Rect(4, 4, 350, 400);
		private bool m_ShowDebugWindow = false;
		private double m_LastFrameDuration = 0.0;
		private Rect m_FpsRect = new Rect(0f, 8f, 64f, 24f);

		private void OnGUI()
		{
			// Always show the last frame duration
			m_FpsRect.x = (Screen.width - m_FpsRect.width) - 8;
			GUI.Box(m_FpsRect, m_LastFrameDuration.ToString("F3"));

			// Toggle debug window on/off
			if( Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.BackQuote)
				m_ShowDebugWindow = !m_ShowDebugWindow;

			if(m_ShowDebugWindow)
				m_DebugWindowRect = GUILayout.Window("m_DebugWindow".GetHashCode(), m_DebugWindowRect, DebugWindow, "Debug Window");
		}

		private void DebugWindow(int id)
		{
			m_EnableAA = GUILayout.Toggle(m_EnableAA, "Anti-aliasing Enabled");
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
#if RAY_DEBUG
			m_FrameTimer.Reset();
			m_FrameTimer.Start();
#endif

			// Set quad size and camera dimensions
			SetupCamera();

			// Do ray
			DoRays();

			// Render texture
			BlitColorBuffer();
#if RAY_DEBUG
			m_FrameTimer.Stop();
			m_LastFrameDuration = m_FrameTimer.ElapsedMilliseconds / 1000.0;
#endif
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
			Ray ray = new Ray(Vector3.zero, new Vector3(0f, 0f, m_SceneLowerLeft.z));
			int index = -1;

			if(m_EnableAA)
			{

				for(int y = 0; y < m_Height; y++)
				{
					for(int x = 0; x < m_Width; x++)
					{
						int r = 0, g = 0, b = 0;
						for(int n = 0; n < m_AASamples; n++)
						{
							float u = (x + (float)m_RandomDouble.NextDouble()) / (float) m_Width;
							float v = (y + (float)m_RandomDouble.NextDouble()) / (float) m_Height;
							ray.direction.x = m_SceneLowerLeft.x + (u * m_SceneWidth);
							ray.direction.y = m_SceneLowerLeft.y + (v * m_SceneHeight);
							Color32 res = GetColor(ray);
							r += res.r;
							g += res.g;
							b += res.b;
						}

						m_ColorBuffer[++index] = new Color32(
							(byte) Mathf.Clamp(r * m_InvAASamples, 0, 255),
							(byte) Mathf.Clamp(g * m_InvAASamples, 0, 255),
							(byte) Mathf.Clamp(b * m_InvAASamples, 0, 255),
							255 );
					}
				}
			}
			else
			{
				for(int y = 0; y < m_Height; y++)
				{
					for(int x = 0; x < m_Width; x++)
					{
						float u = x / (float) m_Width;
						float v = y / (float) m_Height;
						ray.direction.x = m_SceneLowerLeft.x + (u * m_SceneWidth);
						ray.direction.y = m_SceneLowerLeft.y + (v * m_SceneHeight);
						m_ColorBuffer[++index] = GetColor(ray);
					}
				}
			}
		}

		private void SetupCamera()
		{
			int w = (int) m_OrthoCamera.pixelRect.width;
			int h = (int) m_OrthoCamera.pixelRect.height;
			int f = (int) m_Camera.fieldOfView;

			if(w == m_Width && h == m_Height && f == m_Fov)
				return;

			m_Width = w;
			m_Height = h;
			m_Fov = f;

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
