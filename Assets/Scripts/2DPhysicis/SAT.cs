using System.Collections.Generic;
using UnityEngine;

public static class SAT
{
    /// <summary>
    /// 求两个多边形是否有相交部分
    /// </summary>
    public static bool OverlapPolyPoly2D(List<Vector2> polyA, List<Vector2> polyB)
    {
        // SAT算法属于Narrow Phase，核心是遍历两个多边形的每一条边，取垂线
        // 算出每个多边形在每条垂线上的投影线，
        // 如果每条垂线上两条投影线都没有相交，则两个多边形无重叠
        // 如果有相交，则全部投影线最短的交线就是最短分离距离
        int n = polyA.Count;
        for(int i = 0, j = n - 1; i < n; j = i++)
        {
            Vector2 p = polyA[i];
            Vector2 q = polyA[j];
            var normal = Consts.PerpendicularLine(p, q);
            float aMin, aMax;
            (aMin, aMax) = ProjectPoly(polyA, normal);
            float bMin, bMax;
            (bMin, bMax) = ProjectPoly(polyB, normal);
            if (!Consts.OverlapRange(aMin, aMax, bMin, bMax))
                return false;
        }
        int m = polyB.Count;
        for (int i = 0, j = m - 1; i < m; j = i++)
        {
            Vector2 p = polyB[i];
            Vector2 q = polyB[j];
            var normal = Consts.PerpendicularLine(p, q);
            float aMin, aMax;
            (aMin, aMax) = ProjectPoly(polyA, normal);
            float bMin, bMax;
            (bMin, bMax) = ProjectPoly(polyB, normal);
            if (!Consts.OverlapRange(aMin, aMax, bMin, bMax))
                return false;
        }
        return true;
    }

    /// <summary>
    /// 计算一个多边形的所有点在垂线上的投影线
    /// </summary>
    private static (float, float) ProjectPoly(List<Vector2> poly, Vector2 normal)
    {
        float rmin, rmax;
        rmin = rmax = Vector2.Dot(poly[0], normal);
        for (int i = 1; i < poly.Count; i++)
        {
            float d = Vector2.Dot(poly[i], normal);
            rmin = Mathf.Min(rmin, d);
            rmax = Mathf.Max(rmax, d);
        }
        return (rmin, rmax);
    }

    
}
