using System;
using System.Collections.Generic;
using Bibcam.Decoder;
using UnityEditor;
using UnityEngine;

namespace Bibcam
{
	[ExecuteInEditMode]
	public class BibcamPointCloud : MonoBehaviour, IDisposable
	{
		[SerializeField] private BibcamTextureDemuxer demuxer;
		[SerializeField] private ComputeShader shader;
		[SerializeField, Range(0, 1)] private float sampleQuality = 1;

		[Header("Rendering")] [SerializeField] private Mesh particleMesh;
		[SerializeField] private Material particleMaterial;
		[SerializeField] private bool renderBufferedPoints;

		[Header("Debugging")] [SerializeField] private bool renderGizmos = false;
		[SerializeField] private int gizmosOffset = 0;

		private ComputeBuffer points, args, colors;
		private Vector3[] pointsDebug;
		private uint[] argsData;
		private new Camera camera;
		private static readonly int s_Points = Shader.PropertyToID("_Points");
		private static readonly int s_Colors = Shader.PropertyToID("_Colors");


		private readonly List<Vector3> bufferedPoints = new List<Vector3>();
		private readonly List<Vector3> bufferedColors = new List<Vector3>();
		private ComputeBuffer bufferedPointsBuffer, bufferedColorsBuffer;
		private Vector3[] tempContainer;
		
		public void BeginSample()
		{
			renderBufferedPoints = false;
			ClearCache();
		}

		public void SampleFrame()
		{
			Sample();
			if (tempContainer == null || tempContainer.Length != points.count)
				tempContainer = new Vector3[points.count];
			points.GetData(tempContainer);
			bufferedPoints.AddRange(tempContainer);
			colors.GetData(tempContainer);
			bufferedColors.AddRange(tempContainer);
		}

		public void EndSample()
		{
			Debug.Log("Sampled " + bufferedPoints.Count.ToString("N0") + " points");
			tempContainer = null;
			renderBufferedPoints = true;
			bufferedPointsBuffer = new ComputeBuffer(bufferedPoints.Count, sizeof(float) * 3, ComputeBufferType.Structured);
			bufferedColorsBuffer = new ComputeBuffer(bufferedColors.Count, sizeof(float) * 3, ComputeBufferType.Structured);
			bufferedPointsBuffer.SetData(bufferedPoints);
			bufferedColorsBuffer.SetData(bufferedColors);
			Update();
		}

		[ContextMenu(nameof(ClearCache))]
		private void ClearCache()
		{
			bufferedPoints.Clear();
			bufferedColors.Clear();
			bufferedPointsBuffer?.Dispose();
			bufferedColorsBuffer?.Dispose();
		}

		private void OnDisable()
		{
			Dispose();
		}

		private void Update()
		{
			if (renderBufferedPoints)
			{
				if (bufferedPointsBuffer != null &&
				    bufferedPointsBuffer.IsValid() &&
				    bufferedColorsBuffer != null &&
				    bufferedColorsBuffer.IsValid())
				{
					Render(bufferedPointsBuffer, bufferedColorsBuffer);
				}
			}
			else
			{
				Sample();
				Render(points, colors);
			}
		}

		private void Sample()
		{
			if (!demuxer || !shader) return;
			var depthTex = demuxer.DepthTexture;
			var colTex = demuxer.ColorTexture;
			if (!depthTex || !colTex) return;
			if (sampleQuality <= 0) return;

			if (!camera) camera = Camera.main;
			if (!camera) return;

			var expectedPoints = Mathf.CeilToInt(sampleQuality * depthTex.width * depthTex.height);

			if (points == null || !points.IsValid() || points.count != expectedPoints)
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
			shader.SetFloat("colorHeightFactor", colTex.height / (float)depthTex.height);
			shader.Dispatch(0,
				Mathf.CeilToInt(tx),
				Mathf.CeilToInt(ty),
				1);
			
			if (!renderGizmos)
			{
				if (pointsDebug == null || pointsDebug.Length != points.count)
					pointsDebug = new Vector3[points.count];
				points.GetData(pointsDebug);
			}
		}

		private void Render(ComputeBuffer pointsBuffer, ComputeBuffer colorBuffer)
		{
			if (!particleMesh || !particleMaterial) return;
			argsData ??= new uint[5];
			argsData[0] = particleMesh.GetIndexCount(0);
			argsData[1] = (uint)pointsBuffer.count;
			argsData[2] = particleMesh.GetIndexStart(0);
			argsData[3] = particleMesh.GetBaseVertex(0);
			if (args == null || !args.IsValid())
				args = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
			args.SetData(argsData);
			particleMaterial.SetBuffer(s_Points, pointsBuffer);
			particleMaterial.SetBuffer(s_Colors, colorBuffer);
			Graphics.DrawMeshInstancedIndirect(particleMesh, 0, particleMaterial, new Bounds(Vector3.zero, Vector3.one * 1000), args);
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