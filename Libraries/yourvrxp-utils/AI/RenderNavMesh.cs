using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace yourvrexperience.Utils
{
	public class RenderNavMesh : MonoBehaviour
	{
		public void ShowMesh()
		{
			NavMeshTriangulation meshData = NavMesh.CalculateTriangulation();

			Dictionary<int, List<int>> submeshIndices = new Dictionary<int, List<int>>();

			for (int i = 0; i < meshData.areas.Length; i++)
			{
				if (!submeshIndices.ContainsKey(meshData.areas[i]))
				{
					submeshIndices.Add(meshData.areas[i], new List<int>());
				}

				submeshIndices[meshData.areas[i]].Add(meshData.indices[3 * i]);
				submeshIndices[meshData.areas[i]].Add(meshData.indices[3 * i + 1]);
				submeshIndices[meshData.areas[i]].Add(meshData.indices[3 * i + 2]);
			}

			Mesh mesh = new Mesh();
			mesh.vertices = meshData.vertices;

			mesh.subMeshCount = submeshIndices.Count;
			int index = 0;
			foreach (KeyValuePair<int, List<int>> entry in submeshIndices)
			{
				mesh.SetTriangles(entry.Value.ToArray(), index++);
			}

			GetComponent<MeshFilter>().mesh = mesh;
		}
	}
}