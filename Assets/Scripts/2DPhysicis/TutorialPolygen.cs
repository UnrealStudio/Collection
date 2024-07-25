using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NarrowPhase
{
    public enum NarrowPhaseType
    {
        SAT,
        GJK,
    }

    public class TutorialPolyen : MonoBehaviour
    {
        public NarrowPhaseType NarrowPhaseType;

        // 存储屏幕坐标的列表
        private List<Vector3> positions = new();
		public List<Vector2> TestPolyA;
		public List<Vector2> TestPolyB;

		private Vector3 mousePosition;
		private bool isOverlap = false;

        // 是否分离两个多边形
        public bool SeparatePoly;

        public void Update()
        {
            // 记录鼠标点击的屏幕位置，转换为世界坐标，为了可视化所以外移
            mousePosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.nearClipPlane));

            if (Input.GetMouseButtonDown(0))
            {
                if (positions.Count >= 8)
				{
					isOverlap = false;
					positions.Clear();
				}
                positions.Add(mousePosition);
                if (positions.Count == 8)
                {
                    List<Vector2> posA = positions.GetRange(0, 4).Select(x => (Vector2)x).ToList();
                    List<Vector2> posB = positions.GetRange(4, 4).Select(x => (Vector2)x).ToList();
                    if (NarrowPhaseType == NarrowPhaseType.SAT)
						isOverlap = SAT.OverlapPolyPoly2D(posA, posB, SeparatePoly);
                    else if (NarrowPhaseType == NarrowPhaseType.GJK)
						isOverlap = GJK.OverlapPolyPoly2D(posA, posB, SeparatePoly);
                    if (isOverlap)
                    {
                        Debug.Log("两个多边形判定区域有重叠");

                        // 如果需要分离，更新三角形顶点
                        if (SeparatePoly)
                        {
                            for (int i = 0; i < 4; ++i)
                            {
                                positions[i + 4] = posB[i];
                            }
                        }
                    }
                    else
                        Debug.Log("两个多边形判定区域没有重叠");
                }
            }
			else if (Input.GetKeyDown(KeyCode.P))
			{
				bool res = false;
				if (NarrowPhaseType == NarrowPhaseType.SAT)
					res = SAT.OverlapPolyPoly2D(TestPolyA, TestPolyB, SeparatePoly);
				else if (NarrowPhaseType == NarrowPhaseType.GJK)
					res = GJK.OverlapPolyPoly2D(TestPolyA, TestPolyB, SeparatePoly);
				if (res)
					Debug.Log("两个多边形判定区域有重叠");
				else
					Debug.Log("两个多边形判定区域没有重叠");
			}
        }

        public void OnDrawGizmos()
        {
			// 绘制三角形
			int n = positions.Count;
			if (n == 0)
				return;
			if (n <= 4)
            {
				for (int i = 0; i < n - 1; ++i)
					DrawLine(positions[i], positions[i + 1], Color.green);
				if (n != 4)
					DrawLine(mousePosition, positions[n - 1], Color.green);
				else
					DrawLine(positions[0], positions[3], Color.green);
			}
			else
			{
				for (int i = 0; i < 3; ++i)
					DrawLine(positions[i], positions[i + 1], Color.green);
				DrawLine(positions[0], positions[3], Color.green);
				for (int i = 4; i < n - 1; ++i)
					DrawLine(positions[i], positions[i + 1], Color.blue);
				if (n != 8)
					DrawLine(mousePosition, positions[n - 1], Color.blue);
				else
					DrawLine(positions[4], positions[7], Color.blue);
			}
        }

        public void DrawLine(Vector3 p1, Vector3 p2, Color c)
        {
            // 使用Gizmos绘制三角形的三条边
            Gizmos.color = isOverlap ? Color.red : c;
            Gizmos.DrawLine(p1, p2);
        }
    }
}