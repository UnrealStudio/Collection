using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TutorialPolyen : MonoBehaviour
{
    // 存储屏幕坐标的列表
    public List<Vector3> positions = new();

    public void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 记录鼠标点击的屏幕位置，转换为世界坐标
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.nearClipPlane));

            if (positions.Count >= 6)
                positions.Clear();
            positions.Add(mousePosition);
            if (positions.Count == 6)
            {
                List<Vector2> posA = positions.GetRange(0, 3).Select(x => (Vector2)x).ToList();
                List<Vector2> posB = positions.GetRange(3, 3).Select(x => (Vector2)x).ToList();
                bool res = SAT.OverlapPolyPoly2D(posA, posB);
                if (res)
                    Debug.Log("两个三角形有所相交");
                else
                    Debug.Log("两个三角形没有相交");
            }
        }
        else if (Input.GetKeyDown(KeyCode.M))
        {
            List<Vector2> posA = new()
            { new Vector2(1,1), new Vector2(2, 1), new Vector2(1, 2)};
            List<Vector2> posB = new()
            { new Vector2(2, 2), new Vector2(2, 3) , new Vector2(3, 2)};
            bool res = SAT.OverlapPolyPoly2D(posA, posB);
            if (res)
                Debug.Log("两个三角形有所相交");
            else
                Debug.Log("两个三角形没有相交");
        }
    }

    public void OnDrawGizmos()
    {
        // 绘制三角形
        if (positions.Count >= 3)
        {
            DrawTriangle(positions[0], positions[1], positions[2], Color.red);
            if (positions.Count == 6)
            {
                DrawTriangle(positions[3], positions[4], positions[5], Color.green);
            }
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
