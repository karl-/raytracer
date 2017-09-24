#define RAY_DEBUG

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Parabox.Raytracer
{
	public class RaytracerCamera : MonoBehaviour
	{
		public Color32 clearColor = new Color32(255, 255, 255, 255);

		private Camera m_Camera;
		private Camera m_OrthoCamera;
		private GameObject m_Quad;
		private Texture2D m_RenderTexture;
		private Color32[] m_ColorBuffer;
		// Driven by camera
		private int m_Width = 0, m_Height = 0;
		private float m_FrameRate = 1f;

		private void Awake()
		{
			m_Quad = GetComponentsInChildren<MeshFilter>().First(x => x.gameObject.name.Equals("Billboard")).gameObject;
			m_OrthoCamera = GetComponentsInChildren<Camera>().First(x => x.orthographic);
			m_Camera = GetComponent<Camera>();

			Assert.IsNotNull(m_Quad);
			Assert.IsNotNull(m_Camera);
			Assert.IsNotNull(m_OrthoCamera);

			InvokeRepeating("Render", 0f, m_FrameRate);
		}

		private void ClearBuffer()
		{
#if RAY_DEBUG
			for(int y = 0; y < m_Height; y++)
			{
				for(int x = 0; x < m_Width; x++)
				{
					byte r = (byte) Mathf.Clamp( ((x / (float) m_Width) * 255f), 0, 255);
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
			ClearBuffer();

			// Do ray
			// ...

			// Set quad size and camera dimensions
			SetupCamera();

			// Render texture
			BlitColorBuffer();
		}

		private void SetupCamera()
		{
			int w = (int) m_Camera.pixelRect.width;
			int h = (int) m_Camera.pixelRect.height;

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
			Vector3 tr = m_OrthoCamera.ScreenToWorldPoint(new Vector3(m_OrthoCamera.pixelWidth, m_OrthoCamera.pixelHeight, 0f));

			m_Quad.transform.localScale = new Vector3(tr.x - bl.x, tr.y - bl.y);
		}

		private void BlitColorBuffer()
		{
			m_RenderTexture.SetPixels32(m_ColorBuffer);
			m_RenderTexture.Apply();
		}
	}
}
