using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.AI;

public static class SAT
{
    /// <summary>
    /// 求两个多边形是否有相交部分
    /// </summary>
    public static bool OverlapPolyPoly2D(List<Vector2> polyA, List<Vector2> polyB, bool needSeparate)
    {
        // SAT算法属于Narrow Phase，核心是遍历两个多边形的每一条边，取垂线
        // 算出每个多边形在每条垂线上的投影线，
        // 如果每条垂线上两条投影线都没有相交，则两个多边形无重叠
        // 如果有相交，则全部投影线最短的交线就是最短分离距离
        int n = polyA.Count;
        float smallestOverlap = float.MaxValue;

        // 记录是哪条线段是最短分离轴
        int endPt = 0;
        bool endAtPolyA = false;

        for(int i = 0, j = n - 1; i < n; j = i++)
        {
            Vector2 p = polyA[i];
            Vector2 q = polyA[j];
            var normal = Consts.PerpendicularLine(p, q);
            ProjectPoly(polyA, normal, out float aMin, out float aMax, needSeparate);
            ProjectPoly(polyB, normal, out float bMin, out float bMax, needSeparate);
            if (!Consts.OverlapRange(aMin, aMax, bMin, bMax))
                return false;
            
            // 分离思路：找出最短分离轴
            if (needSeparate)
            {
                float len = Consts.OverlapRangeLength(aMin, aMax, bMin, bMax);
                if (smallestOverlap > len)
                {
                    smallestOverlap = len;
                    endPt = j;
                    endAtPolyA = true;
                }
            }
        }

        int m = polyB.Count;
        for (int i = 0, j = m - 1; i < m; j = i++)
        {
            Vector2 p = polyB[i];
            Vector2 q = polyB[j];
            var normal = Consts.PerpendicularLine(p, q);
            ProjectPoly(polyA, normal, out float aMin, out float aMax, needSeparate);
            ProjectPoly(polyB, normal, out float bMin, out float bMax, needSeparate);
            if (!Consts.OverlapRange(aMin, aMax, bMin, bMax))
                return false;

            if (needSeparate)
            {
                float len = Consts.OverlapRangeLength(aMin, aMax, bMin, bMax);
                if (smallestOverlap > len)
                {
                    smallestOverlap = len;
                    endPt = j;
                    endAtPolyA = false;
                }
            }
        }

        // 如果需要分离，则更改返回值
        if (needSeparate)
        {
            // 定义分离轴和中介点
            Vector2 separationAxis, p, q;
            if (endAtPolyA)
            {
                p = endPt == n - 1 ? polyA[0] : polyA[endPt + 1];
                q = polyA[endPt];
            }
            else
            {
                p = endPt == m - 1 ? polyB[0] : polyB[endPt + 1];
                q = polyB[endPt];
            }
            separationAxis = Consts.PerpendicularLine(p, q);
            separationAxis.Normalize();

            // 计算分离向量
            Vector2 separationVector = Mathf.Sign(Vector2.Dot(Consts.GetCentroid(polyA) - Consts.GetCentroid(polyB), separationAxis)) * smallestOverlap * separationAxis;
            for (int i = 0; i < m; ++i)
            {
                polyB[i] = new Vector2(polyB[i].x - separationVector.x, polyB[i].y - separationVector.y);
            }
        }
        return true;
    }

    /// <summary>
    /// 计算一个多边形的所有点在垂线上的投影线
    /// </summary>
    /// <param name="divLength">是否要除以垂线的模长，除以模长才是真正的投影距离</param>
    private static void ProjectPoly(List<Vector2> poly, Vector2 normal, out float rmin, out float rmax, bool divLength)
    {
        float len = normal.magnitude;
        rmin = rmax = Vector2.Dot(poly[0], normal);
        if(divLength)
        {
            rmin /= len;
            rmax /= len;
        }
        for (int i = 1; i < poly.Count; i++)
        {
            float d = Vector2.Dot(poly[i], normal);
            if (divLength)
                d /= len;
            rmin = Mathf.Min(rmin, d);
            rmax = Mathf.Max(rmax, d);
        }
    }

    
}
