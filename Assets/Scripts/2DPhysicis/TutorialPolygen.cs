using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TutorialPolyen : MonoBehaviour
{
    // 存储屏幕坐标的列表
    private List<Vector3> positions = new();
    private Vector3 mousePosition;

    // 是否分离两个多边形
    public bool SeparatePoly;

    public void Update()
    {
        // 记录鼠标点击的屏幕位置，转换为世界坐标，为了可视化所以外移
        mousePosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.nearClipPlane));

        if (Input.GetMouseButtonDown(0))
        {
            if (positions.Count >= 6)
                positions.Clear();
            positions.Add(mousePosition);
            if (positions.Count == 6)
            {
                List<Vector2> posA = positions.GetRange(0, 3).Select(x => (Vector2)x).ToList();
                List<Vector2> posB = positions.GetRange(3, 3).Select(x => (Vector2)x).ToList();
                bool res = SAT.OverlapPolyPoly2D(posA, posB, SeparatePoly);
                if (res)
                {
                    Debug.Log("两个三角形判定区域有重叠");

                    // 如果需要分离，更新三角形顶点
                    if (SeparatePoly)
                    {
                        for(int i = 0; i < 3; ++i)
                        {
                            positions[i + 3] = posB[i];
                        }
                    }
                }
                else
                    Debug.Log("两个三角形判定区域没有重叠");
            }
        }
    }

    public void OnDrawGizmos()
    {
        // 绘制三角形
        if (positions.Count >= 2)
        {
            if(positions.Count == 2)
                DrawTriangle(positions[0], positions[1], mousePosition, Color.red);
            else
                DrawTriangle(positions[0], positions[1], positions[2], Color.red);
            if(positions.Count == 5)
                DrawTriangle(positions[3], positions[4], mousePosition, Color.green);
            else if (positions.Count == 6)
                DrawTriangle(positions[3], positions[4], positions[5], Color.green);
        }
    }

    public void DrawTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Color c)
    {
        // 使用Gizmos绘制三角形的三条边
        Gizmos.color = c;
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p1);
    }
}
