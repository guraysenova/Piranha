﻿using System.Runtime.CompilerServices;
using UnityEngine;

namespace Pirahna
{
	public class PirahnaTarget : MonoBehaviour
	{
		/// <summary>
		/// Abstracts access to a mesh.
		/// </summary>
		public class MeshTarget
		{
			public enum MeshTargetType { Missing, MeshFilter, SkinnedMeshRenderer }

			/// <summary>
			/// Stores the current type of target.
			/// </summary>
			public MeshTargetType TargetType { get; private set; } = MeshTargetType.Missing;

			private MeshFilter mf;
			private SkinnedMeshRenderer smr;

			public void Initialize (GameObject target)
			{
				// Try and get a MeshFilter.
				mf = target.GetComponent<MeshFilter> ();

				// If there isn't a MeshFilter, try and get a SkinnedMeshRenderer.
				if (mf == null)
					smr = target.GetComponent<SkinnedMeshRenderer> ();
				// Otherwise, we've found a MeshFilter and can update the TargetType.
				else
				{
					TargetType = MeshTargetType.MeshFilter;
					return;
				}

				// If there isn't a SkinnedMeshRenderer, the target GameObject isn't valid so we need to throw an error.
				if (smr == null)
				{
					TargetType = MeshTargetType.Missing;
					throw new MissingComponentException ("Target doesn't have Mesh Filter or Skinned Mesh Renderer");
				}
				// Otherwise, we've found a SkinnedMeshRenderer and can update the TargetType
				else
					TargetType = MeshTargetType.SkinnedMeshRenderer;
			}

			/// <summary>
			/// Stores the target's mesh in mesh passed to this method.
			/// </summary>
			/// <param name="mesh">The mesh to store the target's mesh in.</param>
			public void GetMesh (ref Mesh mesh)
			{
				switch (TargetType)
				{
					case MeshTargetType.MeshFilter:
						mesh = mf.mesh;
						break;
					case MeshTargetType.SkinnedMeshRenderer:
						smr.BakeMesh (mesh);
						break;
				}
			}

			/// <summary>
			/// Returns the target's vertex count. Return's zero if the target is missing.
			/// </summary>
			/// <returns></returns>
			public int GetVertexCount ()
			{
				switch (TargetType)
				{
					case MeshTargetType.MeshFilter:
						return mf.mesh.vertexCount;
					case MeshTargetType.SkinnedMeshRenderer:
						return smr.sharedMesh.vertexCount;
				}

				return 0;
			}
		}

		public float force = 5f;
		public Rigidbody[] pirahnas;

		private MeshTarget target;
		private Mesh mesh;
		private Vector3[] vertices;

		private void Awake ()
		{
			target = new MeshTarget ();
			mesh = new Mesh ();
		}

		private void OnEnable ()
		{
			target.Initialize (gameObject);
			target.GetMesh (ref mesh);

			vertices = mesh.vertices;
		}

		private void FixedUpdate ()
		{
			// If the target is a Skinned Mesh Renderer, it's vertices are being animated each frame, so we need to update our
			// vertices array.
			if (target.TargetType == MeshTarget.MeshTargetType.SkinnedMeshRenderer)
			{
				target.GetMesh (ref mesh);
				vertices = mesh.vertices;
			}

			for (var i = 0; i < pirahnas.Length; i++)
			{
				var pirahna = pirahnas[i];

				// Don't do anything to the rigidbody if it's game object is disabled.
				if (!pirahna.gameObject.activeInHierarchy)
					continue;

				// Get the target position in worlspace. This the position the rigidbody will be trying to reach.
				var targetPosition = GetVerticeInWorldspace (GetTargetVertexIndex (i));
				// Calculate the direction to move along.
				var direction = (targetPosition - pirahna.transform.position).normalized;

				// Add force towards the target position.
				pirahna.AddForce (direction * force * Time.fixedDeltaTime, ForceMode.Impulse);
			}
		}

		/// <summary>
		/// Get a vertex index from a pirahna index.
		/// </summary>
		[MethodImpl (MethodImplOptions.AggressiveInlining)]
		private int GetTargetVertexIndex (int pirahnaIndex)
		{
			// Get how far between the start and end of the array the current index is, in a 0 to 1 range.
			var normalizedIndex = (float)pirahnaIndex / pirahnas.Length;
			// Remap the normalized index to the length of the vertex array.
			return (int)(normalizedIndex * vertices.Length);
		}

		/// <summary>
		/// Converts a vertice into worldspace.
		/// </summary>
		[MethodImpl (MethodImplOptions.AggressiveInlining)]
		private Vector3 GetVerticeInWorldspace (int i)
		{
			return transform.localToWorldMatrix.MultiplyPoint3x4 (vertices[i]);
		}
	}
}