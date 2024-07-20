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
}