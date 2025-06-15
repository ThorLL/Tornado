using Compute;
using Simulation.Renderers.MeshGenerators;
using UnityEngine;

namespace Simulation.Renderers
{
	public class SphereRenderer : ParticleRenderer
	{
		static readonly int Positions = Shader.PropertyToID("Positions");
		static readonly int Velocities = Shader.PropertyToID("Velocities");
		static readonly int Scale = Shader.PropertyToID("scale");
		static readonly int VelocityMax = Shader.PropertyToID("velocityMax");
		static readonly int LocalToWorld = Shader.PropertyToID("localToWorld");

		[Header("Settings")] 
		public float scale;
		public float velocityDisplayMax;
		public int meshResolution;

		Shader _shaderShaded;

		Mesh _mesh;
		Material _mat;
		ComputeBuffer _argsBuffer;

		protected override void Enabled()
		{
			_shaderShaded = Shader.Find("Simulation/Particle3DSurf");
		}

		void LateUpdate()
		{
			UpdateSettings();

			Bounds bounds = new(Vector3.zero, Vector3.one * 10000);
			Graphics.DrawMeshInstancedIndirect(_mesh, 0, _mat, bounds, _argsBuffer);
		}

		void UpdateSettings()
		{
			_mesh = SphereGenerator.GenerateSphereMesh(meshResolution);
			ComputeHelper.CreateArgsBuffer(ref _argsBuffer, _mesh, Sim.ParticleCount);

			_mat = new Material(_shaderShaded);

			_mat.SetBuffer(Positions, Sim.PositionBuffer);
			_mat.SetBuffer(Velocities, Sim.VelocityBuffer);

			if (!_mat) return;

			_mat.SetFloat(Scale, scale * 0.01f);
			_mat.SetFloat(VelocityMax, velocityDisplayMax);

			Vector3 s = transform.localScale;
			transform.localScale = Vector3.one;
			Matrix4x4 localToWorld = transform.localToWorldMatrix;
			transform.localScale = s;

			_mat.SetMatrix(LocalToWorld, localToWorld);
		}
		

		void OnDestroy() => _argsBuffer.Release();
	}
}