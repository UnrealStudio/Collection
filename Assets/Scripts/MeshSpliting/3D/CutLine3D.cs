using System.Collections.Generic;
using UnityEngine;

public class CutLine3D : MonoBehaviour
{
	public int SlicePrecision = 6;

	[SerializeField] private LineRenderer LR;
	[SerializeField] private Vector3 StartPos;    //切割线的开始点
	[SerializeField] private Vector3 EndPos;      //切割线的终止点
	public bool isDrawing;      //是否正在切割
	private Camera mainCamera;

	public void Start()
	{
		LR = GetComponent<LineRenderer>();
		LR.positionCount = 2;
		mainCamera = Camera.main;
	}

	public void Update()
	{
		float near = mainCamera.nearClipPlane;
		if (Input.GetMouseButtonDown(0) && !isDrawing)
		{
			isDrawing = true;

			StartPos = Input.mousePosition;
			LR.SetPosition(0, mainCamera.ScreenToWorldPoint(new Vector3(StartPos.x, StartPos.y, near)));
		}
		if (Input.GetMouseButton(0) && isDrawing)
		{
			EndPos = Input.mousePosition;
			LR.SetPosition(1, mainCamera.ScreenToWorldPoint(new Vector3(EndPos.x, EndPos.y, near)));
		}
		if (Input.GetMouseButtonUp(0) && isDrawing)
		{
			//抬起鼠标时，进行一次切割判断
			isDrawing = false;
			Slice();
		}
	}

	private void Slice()
	{
		float near = mainCamera.nearClipPlane;

		Vector3 line = mainCamera.ScreenToWorldPoint(new Vector3(EndPos.x, EndPos.y, near)) - mainCamera.ScreenToWorldPoint(new Vector3(StartPos.x, StartPos.y, near));

		// 为了防止在切割的时候生成的新物体再次在循环中被检测到，就先在射线检测阶段记录待切割物体，然后再进行切割
		List<(SlicedObject3D, Plane)> list = new();
		HashSet<SlicedObject3D> set = new();

		// SlicePrecision 代表了分辨精度，如果直接采用平面与模型的碰撞检测，性能会不佳
		for (int i = 0; i < SlicePrecision; i++)
		{
			Ray ray = mainCamera.ScreenPointToRay(Vector3.Lerp(StartPos, EndPos, (float)i / SlicePrecision));

			if (Physics.Raycast(ray, out RaycastHit hit))
			{
				Plane splitPlane = new(Vector3.Cross(line, ray.direction).normalized, hit.point);
				var shatter = hit.collider.GetComponent<SlicedObject3D>();
				if (!set.Contains(shatter))
				{
					list.Add((shatter, splitPlane));
					set.Add(shatter);
				}
			}
		}

		foreach (var pair in list)
		{
			pair.Item1.Split(pair.Item2);
		}
	}
}