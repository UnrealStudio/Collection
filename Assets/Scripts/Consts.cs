using System.Collections.Generic;
using UnityEngine;

public static class Consts
{
    public const float eps = 1e-6f;

    /// <summary>
    /// 计算点在直线上的投影
    /// </summary>
    /// <param name="pointA">直线上一点A</param>
    /// <param name="pointB">直线上另一点B</param>
    /// <param name="pointToProject">计算点</param>
    /// <returns>投影点</returns>
    public static Vector2 ProjectPointOnLine(in Vector2 pointA, in Vector2 pointB, in Vector2 pointToProject)
    {
        // 计算向量AB和向量AP
        Vector2 vectorAB = pointB - pointA;
        Vector2 vectorAP = pointToProject - pointA;

        // 计算向量AB和向量AP的点积
        float dotProduct = Vector2.Dot(vectorAP, vectorAB);

        // 计算向量AB的模长平方、投影点的比例因子
        float magnitudeABSquared = vectorAB.sqrMagnitude;
        float projectionFactor = dotProduct / magnitudeABSquared;

        // 计算投影点
        return pointA + (vectorAB * projectionFactor);
    }

    /// <summary>
    /// 计算一个密度均匀的二维多边形的质心
    /// </summary>
    public static Vector2 GetCentroid(List<Vector2> points)
    {
        Vector2 centroid = new(0, 0);
        int n = points.Count;
        for (int i = 0; i < n; i++)
            centroid += points[i];
        return centroid / n;
    }

    /// <summary>
    /// 计算直线的垂线
    /// </summary>
    public static Vector2 PerpendicularLine(in Vector2 pointA, in Vector2 pointB) => new(pointB.y - pointA.y, -(pointB.x - pointA.x));

    /// <summary>
    /// 计算两条投影线段（长度上）是否相交
    /// </summary>
    /// <returns></returns>
    public static bool OverlapRange(float aMin, float aMax, float bMin, float bMax) => (aMin + eps) <= bMax && (aMax - eps) >= bMin;

    /// <summary>
    /// 计算两条相交投影线段的相交长度
    /// </summary>
    public static float OverlapRangeLength(float aMin, float aMax, float bMin, float bMax) => Mathf.Min(aMax, bMax) - Mathf.Max(aMin, bMin);

    ///<summary>计算一个点是否在一个多边形内</summary>
    ///<param name="point">点的坐标</param>
    ///<param name="polygon">多边形的各个顶点坐标</param>
    ///<returns>如果在多边形内返回True，否则返回False</returns>
    public static bool InnerGraphByAngle(Vector2 point, params Vector2[] polygon)
    {
        // 思路：从给定点绘制一条射线，然后计算该射线与多边形边界的交点数量
        int intersectCount = 0;
        int vertexCount = polygon.Length;

        for (int i = 0, j = vertexCount - 1; i < vertexCount; j = i++)
        {
            if ((polygon[i].y > point.y) != (polygon[j].y > point.y) &&
                point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
            {
                intersectCount++;
            }
        }
        return intersectCount % 2 == 1;
    }

    ///<summary>计算坐标系中的任意两条线段是否相交</summary>
    /// <param name="a">第一条线段的一个端点坐标</param>
    /// <param name="b">第一条线段的另一个端点坐标</param>
    /// <param name="c">第二条线段的一个端点坐标</param>
    /// <param name="d">第二条线段的另一个端点坐标</param>
    /// <param name="crossPoint">输出交点的坐标，如果没有交点输出(0,0)</param>
    /// <returns>如果有交点返回True，否则返回False</returns>
    public static bool GetCrossPoint(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 crossPoint)
    {
        crossPoint = Vector2.zero;
        double denominator = (b.y - a.y) * (d.x - c.x) - (a.x - b.x) * (c.y - d.y);
        if (denominator == 0) return false;
        double x = ((b.x - a.x) * (d.x - c.x) * (c.y - a.y)
                    + (b.y - a.y) * (d.x - c.x) * a.x
                    - (d.y - c.y) * (b.x - a.x) * c.x) / denominator;
        double y = -((b.y - a.y) * (d.y - c.y) * (c.x - a.x)
                    + (b.x - a.x) * (d.y - c.y) * a.y
                    - (d.x - c.x) * (b.y - a.y) * c.y) / denominator;
        if ((x - a.x) * (x - b.x) <= 0 && (y - a.y) * (y - b.y) <= 0
             && (x - c.x) * (x - d.x) <= 0 && (y - c.y) * (y - d.y) <= 0)
        {
            crossPoint = new Vector2((float)x, (float)y);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 判断一个点是否在某条线段（直线）的左侧
    /// </summary>
    /// <param name="point">点的坐标</param>
    /// <param name="startPoint">线段起点</param>
    /// <param name="endPoint">线段终点</param>
    public static bool IsPointOnLeftSideOfLine(Vector2 point, Vector2 startPoint, Vector2 endPoint)
    {
        // 计算直线上的向量
        Vector2 lineVector = endPoint - startPoint;
        // 计算起始点到点的向量
        Vector2 pointVector = point - startPoint;
        // 使用叉乘判断点是否在直线的左侧
        float crossProduct = (lineVector.x * pointVector.y) - (lineVector.y * pointVector.x);
        // 如果叉乘结果大于0，则点在直线的左侧
        return crossProduct > 0;
    }
}