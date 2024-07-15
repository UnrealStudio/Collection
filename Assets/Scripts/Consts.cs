using UnityEngine;

public static class Consts
{
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
}
