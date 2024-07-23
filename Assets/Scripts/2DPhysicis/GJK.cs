using System.Collections.Generic;
using UnityEngine;

namespace NarrowPhase
{
    public static class GJK
    {
        /// <summary>
        /// 求两个多边形是否有相交部分
        /// </summary>
        public static bool OverlapPolyPoly2D(List<Vector2> polyA, List<Vector2> polyB, bool needSeparate)
        {
            // GJK 算法属于Narrow Phase，核心是通过闵可夫斯基差，得到一个单纯形，并且验证这个单纯形是否包括原点
            // 如果无论递归多少次（直到退出），都找不到包含原点的单纯形，代表没有碰撞
            // 如果存在包含原点的单纯形，代表发生碰撞
            // GJK的分离算法是另一种算法，名为EPA

            // 第一个起始向量取二者的质心差
            Vector2 CentroidA = Consts.GetCentroid(polyA);
            Vector2 CentroidB = Consts.GetCentroid(polyB);

            return false;
        }

        /// <summary>
        /// Support方法，GJK中的核心方法，用于找到多边形在向量上最远的投影点<br/>
        /// GJK 需要精确投影点坐标，因此不能返回模长
        /// </summary>
        /// <returns>最远的投影点坐标</returns>
        public static Vector2 Support(List<Vector2> poly, Vector2 dir)
        {
            Vector2 result = poly[0];
            float maxDot = Vector2.Dot(poly[0], dir);
            for (int i = 1; i < poly.Count; ++i)
            {
                float dot = Vector2.Dot(poly[i], dir);
                if (dot > maxDot)
                {
                    maxDot = dot;
                    result = poly[i];
                }
            }
            return result;
        }
    }
}

