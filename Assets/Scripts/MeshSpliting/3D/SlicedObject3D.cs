using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(UvMapper))]
public class SlicedObject3D : MonoBehaviour
{
	[SerializeField]
	private bool fillCut = true;

	private Hull hull;

	public void Start()
	{
		hull ??= new Hull(GetComponent<MeshFilter>().sharedMesh);
	}

	/// <summary>
	/// 根据传入的平面，将物体分割成多个部分，并创建新的物体
	/// </summary>
	public void Split(in Plane plane)
	{
		var uvMapper = GetComponent<UvMapper>();

		ConvertToLocal(plane, out Vector3 point, out Vector3 normal);
		GenarateNewHulls(uvMapper, point, normal, out List<Hull> newHulls);
		CreateNewGOByHull(newHulls, point, normal);
		Destroy(gameObject);
	}

	private void ConvertToLocal(Plane plane, out Vector3 point, out Vector3 normal)
	{
		Vector3 localPoint = transform.InverseTransformPoint(plane.normal * -plane.distance);
		Vector3 localNormal = transform.InverseTransformDirection(plane.normal);

		localNormal.Scale(transform.localScale);
		localNormal.Normalize();

		point = localPoint;
		normal = localNormal;
	}

	private void GenarateNewHulls(UvMapper uvMapper, in Vector3 point, in Vector3 normal, out List<Hull> newHulls)
	{
		newHulls = new List<Hull>();

		// 将物体分割成两个部分
		hull.Split(point, normal, fillCut, uvMapper, out Hull a, out Hull b);

		if (!a.IsEmpty)
			newHulls.Add(a);

		if (!b.IsEmpty)
			newHulls.Add(b);
	}

	private void CreateNewGOByHull(List<Hull> newHulls, in Vector3 point, in Vector3 normal)
	{
		// 获取Mesh
		Mesh[] newMeshes = new Mesh[newHulls.Count];

		// Mesh赋值
		for (int i = 0; i < newHulls.Count; i++)
			newMeshes[i] = newHulls[i].GetMesh();

		// 移除网格，加快实例化
		GetComponent<MeshFilter>().sharedMesh = null;
		
		if (TryGetComponent<MeshCollider>(out var meshCollider))
		{
			meshCollider.sharedMesh = null;
		}

		// 创建新物体，可以用于返回
		var newGameObjects = new GameObject[newHulls.Count];

		for (int i = 0; i < newHulls.Count; i++)
		{
			Hull newHull = newHulls[i];
			Mesh newMesh = newMeshes[i];

			GameObject newGameObject = Instantiate(gameObject);

			// 分离两个物体
			if(newMesh.vertices.Length > 0)
			{
				var vert = newMesh.vertices[0];
				if (Vector3.Dot(vert - point, normal) >= 0)
					newGameObject.transform.position += normal / 3;
				else
					newGameObject.transform.position -= normal / 3;
			}

			if (newGameObject.TryGetComponent<SlicedObject3D>(out var newShatterTool))
			{
				newShatterTool.hull = newHull;
			}

			if (newGameObject.TryGetComponent<MeshFilter>(out var newMeshFilter))
			{
				newMeshFilter.sharedMesh = newMesh;
			}

			if (newGameObject.TryGetComponent<MeshCollider>(out var newMeshCollider))
			{
				newMeshCollider.sharedMesh = newMesh;
			}

			newGameObjects[i] = newGameObject;
		}
	}
}