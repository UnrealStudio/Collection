using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CopyRight Gustav Olsson，FastHull改造而来
/// </summary>
public class Hull
{
	private const float smallestValidLength = 0.01f;
	private const float smallestValidRatio = 0.05f;

	private bool isValid = true;

	private List<Vector3> vertices;
	private List<Vector3> normals;
	private List<Vector4> tangents;
	private List<Vector2> uvs;
	private List<int> triangles;

	public Hull(Mesh mesh)
	{
		vertices = new(mesh.vertices);
		triangles = new(mesh.triangles);

		if (mesh.normals.Length > 0)
			normals = new(mesh.normals);

		if (mesh.tangents.Length > 0)
			tangents = new(mesh.tangents);

		if (mesh.uv.Length > 0)
			uvs = new(mesh.uv);
	}

	public Hull(Hull reference)
	{
		vertices = new(reference.vertices.Count);
		triangles = new(reference.triangles.Count);

		if (reference.normals != null)
			normals = new(reference.normals.Count);

		if (reference.tangents != null)
			tangents = new(reference.tangents.Count);

		if (reference.uvs != null)
			uvs = new(reference.uvs.Count);
	}

	public bool IsEmpty
	{
		get { return !isValid || vertices.Count < 3 || triangles.Count < 3; }
	}

	public Mesh GetMesh()
	{
		if (isValid)
		{
			Mesh mesh = new Mesh
			{
				// Required properties
				vertices = vertices.ToArray(),
				triangles = triangles.ToArray()
			};

			// Optional properties
			if (normals != null)
			{
				mesh.normals = normals.ToArray();
			}

			if (tangents != null)
			{
				mesh.tangents = tangents.ToArray();
			}

			if (uvs != null)
			{
				mesh.uv = uvs.ToArray();
			}

			return mesh;
		}

		return null;
	}

	/// <summary>
	/// 根据传入的信息，将Hull切割成两个Hull
	/// </summary>
	/// <param name="localPlaneNormal">切割平面的正向法线</param>
	/// <param name="localPointOnPlane">切割平面上的一点</param>
	public void Split(Vector3 localPointOnPlane, Vector3 localPlaneNormal, bool fillCut, UvMapper uvMapper, out Hull resultA, out Hull resultB)
	{
		// 防御性保护
		if (localPlaneNormal == Vector3.zero)
			localPlaneNormal.y = 1;

		// 实例化切割后的两个Hull
		resultA = new Hull(this);
		resultB = new Hull(this);

		// 将顶点分配到两个Hull中，分别记录在布尔和int数组
		AssignVertices(resultA, resultB, localPointOnPlane, localPlaneNormal, out bool[] vertexAbovePlane, out int[] oldToNewVertexMap);

		// 根据分配的顶点信息，生成新的三角面
		AssignTriangles(resultA, resultB, vertexAbovePlane, oldToNewVertexMap, localPointOnPlane, localPlaneNormal, out List<Vector3> cutEdges);

		if (fillCut)
			FillCutEdges(resultA, resultB, cutEdges, localPlaneNormal, uvMapper);

		CheckValid(resultA, resultB, localPlaneNormal);

		isValid = false;
	}

	/// <summary>
	/// 根据顶点在半平面的正侧还是负侧可以把顶点分离到两侧
	/// </summary>
	private void AssignVertices(Hull a, Hull b, in Vector3 pointOnPlane, in Vector3 planeNormal, out bool[] vertexAbovePlane, out int[] oldToNewVertexMap)
	{
		vertexAbovePlane = new bool[vertices.Count];
		oldToNewVertexMap = new int[vertices.Count];

		for (int i = 0; i < vertices.Count; i++)
		{
			Vector3 vertex = vertices[i];

			// 根据是否在平面的上半部分，将顶点分配到两个Hull中
			bool abovePlane = Vector3.Dot(pointOnPlane - vertex, planeNormal) >= 0;
			vertexAbovePlane[i] = abovePlane;
			Hull tmp = abovePlane ? a : b;
			oldToNewVertexMap[i] = tmp.vertices.Count;
			tmp.vertices.Add(vertex);
			if (normals != null)
				tmp.normals.Add(normals[i]);
			if (uvs != null)
				tmp.uvs.Add(uvs[i]);
			if (tangents != null)
				tmp.tangents.Add(tangents[i]);
		}
	}

	/// <summary>
	/// 把三角面还原到两个 Hull 中，并且插补被切割到的三角面
	/// </summary>
	private void AssignTriangles(Hull a, Hull b, bool[] vertexAbovePlane, int[] oldToNewVertexMap, Vector3 pointOnPlane, Vector3 planeNormal, out List<Vector3> cutEdges)
	{
		cutEdges = new();

		int triangleCount = triangles.Count / 3;

		for (int i = 0; i < triangleCount; i++)
		{
			int index0 = triangles[i * 3 + 0];
			int index1 = triangles[i * 3 + 1];
			int index2 = triangles[i * 3 + 2];

			bool above0 = vertexAbovePlane[index0];
			bool above1 = vertexAbovePlane[index1];
			bool above2 = vertexAbovePlane[index2];

			// 如果原先的三角面的三个顶点在同一半侧，可以直接添加到新的Hull中
			if (above0 && above1 && above2)
			{
				a.triangles.Add(oldToNewVertexMap[index0]);
				a.triangles.Add(oldToNewVertexMap[index1]);
				a.triangles.Add(oldToNewVertexMap[index2]);
			}
			else if (!above0 && !above1 && !above2)
			{
				b.triangles.Add(oldToNewVertexMap[index0]);
				b.triangles.Add(oldToNewVertexMap[index1]);
				b.triangles.Add(oldToNewVertexMap[index2]);
			}
			else
			{
				// 插补三角面
				int top, cw, ccw;

				if (above1 == above2 && above0 != above1)
				{
					top = index0;
					cw = index1;
					ccw = index2;
				}
				else if (above2 == above0 && above1 != above2)
				{
					top = index1;
					cw = index2;
					ccw = index0;
				}
				else
				{
					top = index2;
					cw = index0;
					ccw = index1;
				}

				Vector3 cutVertex0, cutVertex1;

				if (vertexAbovePlane[top])
				{
					SplitTriangle(a, b, oldToNewVertexMap, pointOnPlane, planeNormal, top, cw, ccw, out cutVertex0, out cutVertex1);
				}
				else
				{
					SplitTriangle(b, a, oldToNewVertexMap, pointOnPlane, planeNormal, top, cw, ccw, out cutVertex1, out cutVertex0);
				}

				// Add cut edge
				if (cutVertex0 != cutVertex1)
				{
					cutEdges.Add(cutVertex0);
					cutEdges.Add(cutVertex1);
				}

			}
		}
	}

	/// <summary>
	/// 2D分离算法，切割三角面分离到两个Hull
	/// </summary>
	private void SplitTriangle(Hull topHull, Hull bottomHull, int[] oldToNewVertexMap, Vector3 pointOnPlane, Vector3 planeNormal, int top, int cw, int ccw, out Vector3 cwIntersection, out Vector3 ccwIntersection)
	{
		Vector3 v0 = vertices[top];
		Vector3 v1 = vertices[cw];
		Vector3 v2 = vertices[ccw];

		// 计算出到半平面的比例
		float cwDenominator = Vector3.Dot(v1 - v0, planeNormal);
		float cwScalar = Mathf.Clamp01(Vector3.Dot(pointOnPlane - v0, planeNormal) / cwDenominator);

		float ccwDenominator = Vector3.Dot(v2 - v0, planeNormal);
		float ccwScalar = Mathf.Clamp01(Vector3.Dot(pointOnPlane - v0, planeNormal) / ccwDenominator);

		// 计算出两个交点
		Vector3 cwVertex = new Vector3
		{
			x = v0.x + (v1.x - v0.x) * cwScalar,
			y = v0.y + (v1.y - v0.y) * cwScalar,
			z = v0.z + (v1.z - v0.z) * cwScalar
		};

		Vector3 ccwVertex = new Vector3
		{
			x = v0.x + (v2.x - v0.x) * ccwScalar,
			y = v0.y + (v2.y - v0.y) * ccwScalar,
			z = v0.z + (v2.z - v0.z) * ccwScalar
		};

		// 为Top添加三角面
		int cwA = topHull.vertices.Count;
		topHull.vertices.Add(cwVertex);

		int ccwA = topHull.vertices.Count;
		topHull.vertices.Add(ccwVertex);

		topHull.triangles.Add(oldToNewVertexMap[top]);
		topHull.triangles.Add(cwA);
		topHull.triangles.Add(ccwA);

		// 为Buttom添加三角面
		int cwB = bottomHull.vertices.Count;
		bottomHull.vertices.Add(cwVertex);

		int ccwB = bottomHull.vertices.Count;
		bottomHull.vertices.Add(ccwVertex);

		bottomHull.triangles.Add(oldToNewVertexMap[cw]);
		bottomHull.triangles.Add(oldToNewVertexMap[ccw]);
		bottomHull.triangles.Add(ccwB);

		bottomHull.triangles.Add(oldToNewVertexMap[cw]);
		bottomHull.triangles.Add(ccwB);
		bottomHull.triangles.Add(cwB);

		// 插值计算法线
		if (normals != null)
		{
			Vector3 n0 = normals[top];
			Vector3 n1 = normals[cw];
			Vector3 n2 = normals[ccw];

			Vector3 cwNormal = new Vector3
			{
				x = n0.x + (n1.x - n0.x) * cwScalar,
				y = n0.y + (n1.y - n0.y) * cwScalar,
				z = n0.z + (n1.z - n0.z) * cwScalar
			};

			cwNormal.Normalize();

			Vector3 ccwNormal = new Vector3
			{
				x = n0.x + (n2.x - n0.x) * ccwScalar,
				y = n0.y + (n2.y - n0.y) * ccwScalar,
				z = n0.z + (n2.z - n0.z) * ccwScalar
			};

			ccwNormal.Normalize();

			// 添加信息
			topHull.normals.Add(cwNormal);
			topHull.normals.Add(ccwNormal);

			bottomHull.normals.Add(cwNormal);
			bottomHull.normals.Add(ccwNormal);
		}

		// 插值计算切线
		if (tangents != null)
		{
			Vector4 t0 = tangents[top];
			Vector4 t1 = tangents[cw];
			Vector4 t2 = tangents[ccw];

			Vector4 cwTangent = new Vector4
			{
				x = t0.x + (t1.x - t0.x) * cwScalar,
				y = t0.y + (t1.y - t0.y) * cwScalar,
				z = t0.z + (t1.z - t0.z) * cwScalar
			};

			cwTangent.Normalize();
			cwTangent.w = t1.w;

			Vector4 ccwTangent = new Vector4
			{
				x = t0.x + (t2.x - t0.x) * ccwScalar,
				y = t0.y + (t2.y - t0.y) * ccwScalar,
				z = t0.z + (t2.z - t0.z) * ccwScalar
			};

			ccwTangent.Normalize();
			ccwTangent.w = t2.w;

			topHull.tangents.Add(cwTangent);
			topHull.tangents.Add(ccwTangent);

			bottomHull.tangents.Add(cwTangent);
			bottomHull.tangents.Add(ccwTangent);
		}

		// 插值计算UV
		if (uvs != null)
		{
			Vector2 u0 = uvs[top];
			Vector2 u1 = uvs[cw];
			Vector2 u2 = uvs[ccw];

			Vector2 cwUv = new Vector2
			{
				x = u0.x + (u1.x - u0.x) * cwScalar,
				y = u0.y + (u1.y - u0.y) * cwScalar
			};

			Vector2 ccwUv = new Vector2
			{
				x = u0.x + (u2.x - u0.x) * ccwScalar,
				y = u0.y + (u2.y - u0.y) * ccwScalar
			};

			topHull.uvs.Add(cwUv);
			topHull.uvs.Add(ccwUv);

			bottomHull.uvs.Add(cwUv);
			bottomHull.uvs.Add(ccwUv);
		}

		// 设置输出
		cwIntersection = cwVertex;
		ccwIntersection = ccwVertex;
	}

	// TODO 下篇文章再写..
	private void FillCutEdges(Hull a, Hull b, List<Vector3> edges, Vector3 planeNormal, UvMapper uvMapper)
	{
		int edgeCount = edges.Count / 2;

		List<Vector3> points = new(edgeCount);
		List<int> outline = new(edgeCount * 2);

		int start = 0;

		for (int current = 0; current < edgeCount; current++)
		{
			int next = current + 1;

			// Find the next edge
			int nearest = start;
			float nearestDistance = (edges[current * 2 + 1] - edges[start * 2 + 0]).sqrMagnitude;

			for (int other = next; other < edgeCount; other++)
			{
				float distance = (edges[current * 2 + 1] - edges[other * 2 + 0]).sqrMagnitude;

				if (distance < nearestDistance)
				{
					nearest = other;
					nearestDistance = distance;
				}
			}

			// Is the current edge the last edge in this edge loop?
			if (nearest == start && current > start)
			{
				int pointStart = points.Count;
				int pointCounter = pointStart;

				// Add this edge loop to the triangulation lists
				for (int edge = start; edge < current; edge++)
				{
					points.Add(edges[edge * 2 + 0]);
					outline.Add(pointCounter++);
					outline.Add(pointCounter);
				}

				points.Add(edges[current * 2 + 0]);
				outline.Add(pointCounter);
				outline.Add(pointStart);

				// Start a new edge loop
				start = next;
			}
			else if (next < edgeCount)
			{
				// Move the nearest edge sh that it follows the current edge
				Vector3 n0 = edges[next * 2 + 0];
				Vector3 n1 = edges[next * 2 + 1];

				edges[next * 2 + 0] = edges[nearest * 2 + 0];
				edges[next * 2 + 1] = edges[nearest * 2 + 1];

				edges[nearest * 2 + 0] = n0;
				edges[nearest * 2 + 1] = n1;
			}
		}

		if (points.Count > 0)
		{
			// Triangulate the outline

			var triangulator = new Triangulator(points, outline, planeNormal);

			triangulator.Fill(out int[] newEdges, out int[] newTriangles, out int[] newTriangleEdges);

			// Calculate the vertex properties
			Vector3 normalA = -planeNormal;
			Vector3 normalB = planeNormal;

			uvMapper.Map(points, planeNormal, out Vector4[] tangentsA, out Vector4[] tangentsB, out Vector2[] uvsA, out Vector2[] uvsB);

			// Add the new vertices
			int offsetA = a.vertices.Count;
			int offsetB = b.vertices.Count;

			for (int i = 0; i < points.Count; i++)
			{
				a.vertices.Add(points[i]);
				b.vertices.Add(points[i]);
			}

			if (normals != null)
			{
				for (int i = 0; i < points.Count; i++)
				{
					a.normals.Add(normalA);
					b.normals.Add(normalB);
				}
			}

			if (tangents != null)
			{
				for (int i = 0; i < points.Count; i++)
				{
					a.tangents.Add(tangentsA[i]);
					b.tangents.Add(tangentsB[i]);
				}
			}

			if (uvs != null)
			{
				for (int i = 0; i < points.Count; i++)
				{
					a.uvs.Add(uvsA[i]);
					b.uvs.Add(uvsB[i]);
				}
			}

			// Add the new triangles
			int newTriangleCount = newTriangles.Length / 3;

			for (int i = 0; i < newTriangleCount; i++)
			{
				a.triangles.Add(offsetA + newTriangles[i * 3 + 0]);
				a.triangles.Add(offsetA + newTriangles[i * 3 + 2]);
				a.triangles.Add(offsetA + newTriangles[i * 3 + 1]);

				b.triangles.Add(offsetB + newTriangles[i * 3 + 0]);
				b.triangles.Add(offsetB + newTriangles[i * 3 + 1]);
				b.triangles.Add(offsetB + newTriangles[i * 3 + 2]);
			}
		}
	}

	private void CheckValid(Hull a, Hull b, Vector3 planeNormal)
	{
		// 如果切出的物体太小，或者切出的物体比例太小，就认为是无效的
		float lengthA = a.LengthAlongAxis(planeNormal);
		float lengthB = b.LengthAlongAxis(planeNormal);

		float sum = lengthA + lengthB;

		if (sum < smallestValidLength)
		{
			a.isValid = false;
			b.isValid = false;
		}
		else if (lengthA / sum < smallestValidRatio)
		{
			a.isValid = false;
		}
		else if (lengthB / sum < smallestValidRatio)
		{
			b.isValid = false;
		}
	}

	private float LengthAlongAxis(Vector3 axis)
	{
		if (vertices.Count > 0)
		{
			float min = Vector3.Dot(vertices[0], axis);
			float max = min;

			foreach (Vector3 vertex in vertices)
			{
				float distance = Vector3.Dot(vertex, axis);

				min = Mathf.Min(distance, min);
				max = Mathf.Max(distance, max);
			}

			return max - min;
		}

		return 0.0f;
	}
}