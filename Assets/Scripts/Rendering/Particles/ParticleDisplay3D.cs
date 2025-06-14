using Compute;
using Seb.Helpers;
using Simulation;
using UnityEditor.Search;
using UnityEngine;

namespace Rendering.Particles
{

	public class ParticleDisplay3D : MonoBehaviour
	{

		[Header("Settings")] 
		public float scale;
		public Gradient colourMap;
		public int gradientResolution;
		public float velocityDisplayMax;
		public int meshResolution;

		[Header("References")] 
		public TornadoSim sim;
		public Shader shaderShaded;

		Mesh mesh;
		Material mat;
		ComputeBuffer argsBuffer;
		Texture2D gradientTexture;
		DisplayMode modeOld;
		bool needsUpdate;

		void LateUpdate()
		{
			UpdateSettings();

			Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 10000);
			Graphics.DrawMeshInstancedIndirect(mesh, 0, mat, bounds, argsBuffer);
		}

		void UpdateSettings()
		{
			mesh = SphereGenerator.GenerateSphereMesh(meshResolution);
			ComputeHelper.CreateArgsBuffer(ref argsBuffer, mesh, sim.PositionBuffer.count);

			mat = new Material(shaderShaded);

			mat.SetBuffer("Positions", sim.PositionBuffer);
			mat.SetBuffer("Velocities", sim.VelocityBuffer);

			if (!mat) return;
			
			if (needsUpdate)
			{
				needsUpdate = false;
				TextureFromGradient(ref gradientTexture, gradientResolution, colourMap);
				mat.SetTexture("ColourMap", gradientTexture);
			}

			mat.SetFloat("scale", scale * 0.01f);
			mat.SetFloat("velocityMax", velocityDisplayMax);

			Vector3 s = transform.localScale;
			transform.localScale = Vector3.one;
			Matrix4x4 localToWorld = transform.localToWorldMatrix;
			transform.localScale = s;

			mat.SetMatrix("localToWorld", localToWorld);
		}

		public static void TextureFromGradient(ref Texture2D texture, int width, Gradient gradient, FilterMode filterMode = FilterMode.Bilinear)
		{
			if (texture == null)
			{
				texture = new Texture2D(width, 1);
			}
			else if (texture.width != width)
			{
				texture.Reinitialize(width, 1);
			}

			if (gradient == null)
			{
				gradient = new Gradient();
				gradient.SetKeys(
					new GradientColorKey[] { new(Color.black, 0), new(Color.black, 1) },
					new GradientAlphaKey[] { new(1, 0), new(1, 1) }
				);
			}

			texture.wrapMode = TextureWrapMode.Clamp;
			texture.filterMode = filterMode;

			Color[] cols = new Color[width];
			for (int i = 0; i < cols.Length; i++)
			{
				float t = i / (cols.Length - 1f);
				cols[i] = gradient.Evaluate(t);
			}

			texture.SetPixels(cols);
			texture.Apply();
		}

		private void OnValidate()
		{
			needsUpdate = true;
		}

		void OnDestroy()
		{
			argsBuffer.Release();
		}
	}
}