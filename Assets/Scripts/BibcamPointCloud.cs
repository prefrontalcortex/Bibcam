using System;
using Bibcam.Decoder;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;

namespace Bibcam
{
	[ExecuteInEditMode]
	public class BibcamPointCloud : MonoBehaviour, IDisposable
	{
		[SerializeField] private BibcamTextureDemuxer demuxer;
		[SerializeField] private ComputeShader shader;
		[SerializeField, Range(0,1)] private float sampleQuality = 1;

		[Header("Rendering")] [SerializeField] private Mesh particleMesh;
		[SerializeField] private Material particleMaterial;

		[Header("Debugging")] 
		[SerializeField] private bool renderGizmos = false;
		[SerializeField] private int gizmosOffset = 0;

		private ComputeBuffer points, args, colors;
		private Vector3[] pointsDebug;
		private uint[] argsData;
		private new Camera camera;
		private static readonly int s_Points = Shader.PropertyToID("_Points");
		private static readonly int s_Colors = Shader.PropertyToID("_Colors");

		private void OnDisable()
		{
			Dispose();
		}

		private void Update()
		{
			if (!demuxer || !shader) return;
			var depthTex = demuxer.DepthTexture;
			var colTex = demuxer.ColorTexture;
			if (!depthTex || !colTex) return;
			if (sampleQuality <= 0) return;

			if (!camera) camera = Camera.main;
			if (!camera) return;

			var expectedPoints = Mathf.CeilToInt(sampleQuality * depthTex.width * depthTex.height);

			if (points == null || points.count != expectedPoints)
			{
				Dispose();
				points = new ComputeBuffer(expectedPoints, sizeof(float) * 3, ComputeBufferType.Structured);
				colors = new ComputeBuffer(points.count, sizeof(float) * 3, ComputeBufferType.Structured);
			}

			var rayParams = BibcamRenderUtils.RayParams(camera);
			var inverseView = BibcamRenderUtils.InverseView(camera);

			shader.SetTexture(0, "_DepthTexture", depthTex);
			shader.SetTexture(0, "_ColorTexture", colTex);
			shader.SetBuffer(0, "_Points", points);
			shader.SetBuffer(0, "_Colors", colors);
			shader.SetFloat("quality", sampleQuality);
			shader.SetVector("rayParams", rayParams);
			shader.SetMatrix("inverseView", inverseView);
			var tx = depthTex.width / 32f * sampleQuality;
			var ty = depthTex.height / 32f * sampleQuality;
			shader.Dispatch(0,
				Mathf.CeilToInt(tx),
				Mathf.CeilToInt(ty),
				1);

			if (!renderGizmos)
			{
				if(pointsDebug == null || pointsDebug.Length != points.count)
					pointsDebug = new Vector3[points.count];
				points.GetData(pointsDebug);
			}

			if (particleMesh && particleMaterial)
			{
				argsData ??= new uint[5];
				argsData[0] = particleMesh.GetIndexCount(0);
				argsData[1] = (uint)expectedPoints;
				argsData[2] = particleMesh.GetIndexStart(0);
				argsData[3] = particleMesh.GetBaseVertex(0);

				if(args == null || !args.IsValid())
					args = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
				args.SetData(argsData);
				particleMaterial.SetBuffer(s_Points, points);
				particleMaterial.SetBuffer(s_Colors, colors);
				Graphics.DrawMeshInstancedIndirect(particleMesh, 0, particleMaterial, new Bounds(Vector3.zero, Vector3.one * 1000), args);
			}
		}

		private void OnDrawGizmos()
		{
			if (pointsDebug != null && renderGizmos)
			{
				for (var index = 0; index < pointsDebug.Length; index++)
				{
					var pt = pointsDebug[(index + gizmosOffset) % pointsDebug.Length];
					Gizmos.color = Color.gray;
					// Gizmos.DrawLine(Vector3.zero, pt);
					Gizmos.color = Color.red;
					Gizmos.DrawSphere(pt, 0.01f);
					if (index > 1000) break;
				}
			}
		}

		public void Dispose()
		{
			points?.Dispose();
			args?.Dispose();
			colors?.Dispose();
		}
	}
}