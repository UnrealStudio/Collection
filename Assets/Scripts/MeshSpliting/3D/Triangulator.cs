// Shatter Toolkit
// Copyright 2012 Gustav Olsson
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Advanced triangulator capable of filling concave polygons with holes and recursive geometry.
/// An algorithm based on ear-clipping, developed by Gustav Olsson, see http://gustavolsson.squarespace.com/ for more information.
/// </summary>
public class Triangulator
{
	private Object key = new Object();

	// Geometry
	private List<Vector3> points;
	private List<int> edges;
	private List<int> triangles;
	private List<int> triangleEdges;

	private List<List<int>> loops;
	private List<List<bool>> concavities;
	private List<int> duplicateEdges;

	// Properties
	private Vector3 planeNormal;
	private int originalEdgeCount;

	public Triangulator(IList<Vector3> points, IList<int> edges, Vector3 planeNormal)
	{
		this.points = new List<Vector3>(points);
		this.edges = new List<int>(edges);
		triangles = new List<int>();
		triangleEdges = new List<int>();

		this.planeNormal = planeNormal;
		originalEdgeCount = this.edges.Count;
	}

	public void Fill(out int[] newEdges, out int[] newTriangles, out int[] newTriangleEdges)
	{
		lock (key)
		{
			// 准备三角化
			FindLoops();
			FindConcavities();
			PrepareDuplicateEdges();

			// TODO 凹包带来的bug修正
			//// 三角化循环
			//for (int i = 0; i < loops.Count; i++)
			//{
			//	List<int> loop = loops[i];
			//	List<bool> concavity = concavities[i];
			//	int index = 0;
			//	int unsuitableTriangles = 0;

			//	while (loop.Count >= 3)
			//	{
			//		// 评估三角形，zero就是三角化算法中始终固定构成三角面的第零个点
			//		int zero = index == 0 ? loop.Count - 1 : index - 1;
			//		int first = index;
			//		int second = (index + 1) % loop.Count;
			//		int third = (index + 2) % loop.Count;

			//		if (concavity[first] || IsTriangleOverlappingLoop(first, second, third, loop, concavity))
			//		{
			//			// This triangle is not an ear, examine the next one
			//			index++;
			//			unsuitableTriangles++;
			//		}
			//		else
			//		{
			//			// Evaluate loop merge

			//			if (MergeLoops(first, second, third, loop, concavity, out int swallowedLoopIndex))
			//			{
			//				// Merge occured, adjust loop index
			//				if (swallowedLoopIndex < i)
			//				{
			//					i--;
			//				}

			//				//ValidateConcavities();
			//			}
			//			else
			//			{
			//				// No merge occured, fill triangle
			//				FillTriangle(zero, first, second, third, loop, concavity);
			//			}

			//			// Suitable triangles may have appeared
			//			unsuitableTriangles = 0;
			//		}

			//		if (unsuitableTriangles >= loop.Count)
			//		{
			//			// No more suitable triangles in this loop, continue with the next one
			//			break;
			//		}

			//		// Wrap index
			//		if (index >= loop.Count)
			//		{
			//			index = 0;
			//			unsuitableTriangles = 0;
			//		}
			//	}

			//	// Is the loop filled?
			//	if (loop.Count <= 2)
			//	{
			//		// Remove the loop in order to avoid future merges
			//		loops.RemoveAt(i);
			//		concavities.RemoveAt(i);

			//		i--;
			//	}
			//}

			// 使用三角形面填充剩余的Loop
			for (int i = 0; i < loops.Count; i++)
			{
				List<int> loop = loops[i];
				List<bool> concavity = concavities[i];

				while (loop.Count >= 3)
				{
					FillTriangle(0, 1, 2, 3 % loop.Count, loop, concavity);
				}
			}

			// 即将完成，清理
			//RemoveDuplicateEdges();

			SetOutput(out newEdges, out newTriangles, out newTriangleEdges);
		}
	}

	/// <summary>
	/// 将切面的线段组分离成拓扑意义上的多边形边集合
	/// </summary>
	private void FindLoops()
	{
		// 由于可能传入的边集合可能是多个封闭多边形的组合
		// 所以需要将线段组分离成多个封闭的集合
		loops = new List<List<int>>();

		List<int> loop = new List<int>(edges.Count / 2);

		for (int i = 0; i < edges.Count / 2; i++)
		{
			int edge = i * 2;

			int startPoint = edges[edge + 0];
			int endPoint = edges[edge + 1];

			// 一次小保护检测
			if (loop.Count >= 1)
			{
				int previousEndPoint = edges[edge - 1];

				if (startPoint != previousEndPoint)
					Debug.LogError("The edges do not form an edge loop!");
			}

			// 在一次迭代完成前，将所有的顶点都加入到loop中
			loop.Add(edge);
			if (endPoint == edges[loop[0]])
			{
				loops.Add(loop);

				// 一次迭代完成，清空loop，不需要直接申请cap了，因为没人知道可能还有多少个loop
				loop = new List<int>();
			}
		}
	}

	/// <summary>
	/// 判断每个点是否具有凹性
	/// </summary>
	private void FindConcavities()
	{
		concavities = new List<List<bool>>();

		foreach (List<int> loop in loops)
		{
			List<bool> concavity = new List<bool>(loop.Count);

			for (int i = 0; i < loop.Count; i++)
			{
				int point0 = edges[loop[i]];
				int point1 = edges[loop[(i + 1) % loop.Count]];
				int point2 = edges[loop[(i + 2) % loop.Count]];

				Vector3 firstLine = points[point1] - points[point0];
				Vector3 secondLine = points[point2] - points[point1];

				concavity.Add(IsLinePairConcave(ref firstLine, ref secondLine));
			}

			concavities.Add(concavity);
		}
	}

	private void PrepareDuplicateEdges()
	{
		duplicateEdges = new List<int>();
	}

	private void ValidateConcavities()
	{
		for (int j = 0; j < loops.Count; j++)
		{
			IList<int> loop = loops[j];
			IList<bool> concavity = concavities[j];

			for (int i = 0; i < loop.Count; i++)
			{
				int point0 = edges[loop[i]];
				int point1 = edges[loop[(i + 1) % loop.Count]];
				int point2 = edges[loop[(i + 2) % loop.Count]];

				Vector3 firstLine = points[point1] - points[point0];
				Vector3 secondLine = points[point2] - points[point1];

				if (concavity[i] != IsLinePairConcave(ref firstLine, ref secondLine))
				{
					Debug.LogError("Concavity not valid!");
				}
			}
		}
	}

	private void UpdateConcavity(int index, List<int> loop, List<bool> concavity)
	{
		int firstEdge = loop[index];
		int secondEdge = loop[(index + 1) % loop.Count];

		Vector3 firstLine = points[edges[firstEdge + 1]] - points[edges[firstEdge]];
		Vector3 secondLine = points[edges[secondEdge + 1]] - points[edges[secondEdge]];

		concavity[index] = IsLinePairConcave(ref firstLine, ref secondLine);
	}

	/// <summary>
	/// 判断两个向量组成的顶点是否具有凹性
	/// </summary>
	private bool IsLinePairConcave(ref Vector3 line0, ref Vector3 line1)
	{
		Vector3 lineNormal0 = Vector3.Cross(line0, planeNormal);

		// 0向量组成的顶点视为无凹性
		return Vector3.Dot(line1, lineNormal0) > 0.0f;
	}

	private bool IsTriangleOverlappingLoop(int first, int second, int third, List<int> loop, List<bool> concavity)
	{
		int point0 = edges[loop[first]];
		int point1 = edges[loop[second]];
		int point2 = edges[loop[third]];

		Vector3 triangle0 = points[point0];
		Vector3 triangle1 = points[point1];
		Vector3 triangle2 = points[point2];

		for (int i = 0; i < loop.Count; i++)
		{
			if (concavity[i])
			{
				int reflexPoint = edges[loop[i] + 1];

				// 如果是三角形的顶点，跳过
				if (reflexPoint != point0 && reflexPoint != point1 && reflexPoint != point2)
				{
					Vector3 point = points[reflexPoint];

					// 监测点是否在三角形内
					if (Consts3D.IsPointInsideTriangle(ref point, ref triangle0, ref triangle1, ref triangle2, ref planeNormal))
					{
						return true;
					}
				}
			}
		}

		return false;
	}

	private bool MergeLoops(int first, int second, int third, List<int> loop, List<bool> concavity, out int swallowedLoopIndex)
	{

		if (FindClosestPointInTriangle(first, second, third, loop, out int otherLoopIndex, out int otherLoopLocation))
		{
			// Swallow the other loop
			InsertLoop(first, loop, concavity, otherLoopLocation, loops[otherLoopIndex], concavities[otherLoopIndex]);

			// Remove the now obsolete other loop
			loops.RemoveAt(otherLoopIndex);
			concavities.RemoveAt(otherLoopIndex);

			swallowedLoopIndex = otherLoopIndex;

			return true;
		}

		swallowedLoopIndex = -1;

		return false;
	}

	private bool FindClosestPointInTriangle(int first, int second, int third, List<int> loop, out int loopIndex, out int loopLocation)
	{
		Vector3 triangle0 = points[edges[loop[first]]];
		Vector3 triangle1 = points[edges[loop[second]]];
		Vector3 triangle2 = points[edges[loop[third]]];

		Vector3 firstNormal = Vector3.Cross(planeNormal, triangle1 - triangle0);

		int closestLoopIndex = -1;
		int closestLoopLocation = 0;
		float closestDistance = 0.0f;

		for (int i = 0; i < loops.Count; i++)
		{
			IList<int> otherLoop = loops[i];
			IList<bool> otherConcavity = concavities[i];

			if (otherLoop != loop)
			{
				for (int j = 0; j < otherLoop.Count; j++)
				{
					if (otherConcavity[j])
					{
						Vector3 point = points[edges[otherLoop[j] + 1]];

						if (Consts3D.IsPointInsideTriangle(ref point, ref triangle0, ref triangle1, ref triangle2, ref planeNormal))
						{
							// Calculate the distance from the bottom of the triangle to the point
							float distance = Vector3.Dot(point - triangle0, firstNormal);

							if (distance < closestDistance || closestLoopIndex == -1)
							{
								closestLoopIndex = i;
								closestLoopLocation = (j + 1) % otherLoop.Count;
								closestDistance = distance;
							}
						}
					}
				}
			}
		}

		loopIndex = closestLoopIndex;
		loopLocation = closestLoopLocation;

		return closestLoopIndex != -1;
	}

	private void InsertLoop(int insertLocation, List<int> loop, List<bool> concavity, int otherAnchorLocation, List<int> otherLoop, List<bool> otherConcavity)
	{
		int insertPoint = edges[loop[insertLocation]];
		int anchorPoint = edges[otherLoop[otherAnchorLocation]];

		// Create bridge edges
		int bridgeFromEdge = edges.Count;

		edges.Add(anchorPoint);
		edges.Add(insertPoint);

		int bridgeToEdge = edges.Count;

		edges.Add(insertPoint);
		edges.Add(anchorPoint);

		// Save the last added duplicate edge
		duplicateEdges.Add(bridgeToEdge);

		// Insert the other loop into this loop
		int[] insertLoop = new int[otherLoop.Count + 2];
		bool[] insertConcavity = new bool[otherConcavity.Count + 2];

		int index = 0;

		insertLoop[index] = bridgeToEdge;
		insertConcavity[index++] = false;

		for (int i = otherAnchorLocation; i < otherLoop.Count; i++)
		{
			insertLoop[index] = otherLoop[i];
			insertConcavity[index++] = otherConcavity[i];
		}

		for (int i = 0; i < otherAnchorLocation; i++)
		{
			insertLoop[index] = otherLoop[i];
			insertConcavity[index++] = otherConcavity[i];
		}

		insertLoop[index] = bridgeFromEdge;
		insertConcavity[index] = false;

		loop.InsertRange(insertLocation, insertLoop);
		concavity.InsertRange(insertLocation, insertConcavity);

		// Update concavity
		int previousLocation = insertLocation == 0 ? loop.Count - 1 : insertLocation - 1;

		UpdateConcavity(previousLocation, loop, concavity);

		UpdateConcavity(insertLocation, loop, concavity);

		UpdateConcavity(insertLocation + otherLoop.Count, loop, concavity);

		UpdateConcavity(insertLocation + otherLoop.Count + 1, loop, concavity);
	}

	private void FillTriangle(int zero, int first, int second, int third, List<int> loop, List<bool> concavity)
	{
		// 形成三角面
		int zeroEdge = loop[zero];
		int firstEdge = loop[first];
		int secondEdge = loop[second];
		int thirdEdge = loop[third];

		int firstPoint = edges[firstEdge];
		int secondPoint = edges[secondEdge];
		int thirdPoint = edges[thirdEdge];

		int crossEdge;

		if (loop.Count != 3)
		{
			// 创建额外的相交边
			crossEdge = edges.Count;

			edges.Add(firstPoint);
			edges.Add(thirdPoint);
		}
		else
		{
			// 如果是三角形，直接使用第三个边
			crossEdge = thirdEdge;
		}

		// 添加三角面
		triangles.Add(firstPoint);
		triangles.Add(secondPoint);
		triangles.Add(thirdPoint);

		triangleEdges.Add(firstEdge);
		triangleEdges.Add(secondEdge);
		triangleEdges.Add(crossEdge);

		// 更新loop
		loop[second] = crossEdge;
		loop.RemoveAt(first);

		// 更新凹点，始终更新以支持零长边
		Vector3 zeroLine = points[firstPoint] - points[edges[zeroEdge]];
		Vector3 crossLine = points[thirdPoint] - points[firstPoint];
		Vector3 thirdLine = points[edges[thirdEdge + 1]] - points[thirdPoint];

		concavity[zero] = IsLinePairConcave(ref zeroLine, ref crossLine);

		concavity[second] = IsLinePairConcave(ref crossLine, ref thirdLine);
		concavity.RemoveAt(first);
	}

	private void RemoveDuplicateEdges()
	{
		for (int i = 0; i < duplicateEdges.Count; i++)
		{
			int edge = duplicateEdges[i];

			// Remove the duplicate edge
			edges.RemoveRange(edge, 2);

			// Update triangles in triangle edges
			for (int j = 0; j < triangleEdges.Count; j++)
			{
				if (triangleEdges[j] >= edge)
				{
					// The corresponding edge lie in front of the duplicate edge
					triangleEdges[j] -= 2;
				}
			}

			// Update triangles in duplicate edges
			for (int j = i + 1; j < duplicateEdges.Count; j++)
			{
				if (duplicateEdges[j] >= edge)
				{
					// The corresponding edge lie in front of the duplicate edge
					duplicateEdges[j] -= 2;
				}
			}
		}
	}

	private void SetOutput(out int[] newEdges, out int[] newTriangles, out int[] newTriangleEdges)
	{
		// Set edges
		int newEdgeCount = edges.Count - originalEdgeCount;

		if (newEdgeCount > 0)
		{
			newEdges = new int[newEdgeCount];

			edges.CopyTo(originalEdgeCount, newEdges, 0, newEdgeCount);
		}
		else
		{
			newEdges = new int[0];
		}

		// Set triangles
		newTriangles = triangles.ToArray();

		// Set triangle edges
		newTriangleEdges = new int[triangleEdges.Count];

		for (int i = 0; i < triangleEdges.Count; i++)
		{
			newTriangleEdges[i] = triangleEdges[i] / 2;
		}
	}
}