using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.Image;

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

            List<Vector2> SimplexList = new(3);

            // 第一次迭代，得到第一个单纯形顶点
            Vector2 vec = CentroidB - CentroidA;
            SimplexList.Add(GetSimplexNode(vec, polyA, polyB));

            // 第二次迭代，选择第一个单纯形到原点的向量，同时进行一次过原点的判断
            SimplexList.Add(GetSimplexNode(-SimplexList[0], polyA, polyB));
            if (!isCrossingOrigin(SimplexList[0], SimplexList[1]))
                return false;

            // 第三次迭代，选择两个单纯形顶点的垂线，同时进行一次过原点的判断
            SimplexList.Add(GetSimplexNode(GetPerpendicularLineToOrigin(SimplexList[0], SimplexList[1]), polyA, polyB));
            if (!isCrossingOrigin(SimplexList[0], SimplexList[2]))
                return false;

            // 开始递归迭代
            while(true)
            {
                if (Consts.InnerGraphByAngle(Vector2.zero, SimplexList.ToArray()))
                    return true;

                // 通过距离原点最近的单纯形边，找到新的单纯形顶点
                Vector2 edge = GetCloestEdgeToOrigin(SimplexList, out var index1, out var index2);
                Vector2 newNode = GetSimplexNode(GetPerpendicularLineToOrigin(edge, Vector2.zero), polyA, polyB);

                // 如果存在重复的顶点，退出递归
                for(int i = 0; i < 3; ++i)
                {
                    if (newNode == SimplexList[i])
                        return false;
                }
                if (!isCrossingOrigin(SimplexList[0], newNode))
                    return false;

                // 更新单纯形
                for(int i = 0; i < 3; ++i)
                {
                    if(i != index1 && i != index2)
                    {
                        SimplexList[i] = newNode;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Support方法，GJK中的核心方法，用于找到多边形在向量上最远的投影点<br/>
        /// GJK 需要精确投影点坐标，因此不能返回模长
        /// </summary>
        /// <returns>最远的投影点坐标</returns>
        private static Vector2 Support(List<Vector2> poly, in Vector2 dir)
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

        /// <summary>
        /// 根据两个点的坐标，通过叉乘得到朝向原点的法向量
        /// </summary>
        private static Vector2 GetPerpendicularLineToOrigin(in Vector2 pA, in Vector2 pB)
        {
            Vector2 normal = Consts.PerpendicularLine(pA, pB);
            return Vector2.Dot(normal, -pA) > 0 ? normal : -normal;
        }

        /// <summary>
        /// 根据某个向量获取两个多边形得到的一个单纯形顶点
        /// </summary>
        private static Vector2 GetSimplexNode(in Vector2 vec, List<Vector2> polyA, List<Vector2> polyB)
        {
            Vector2 pA = Support(polyA, vec);
            Vector2 pB = Support(polyB, -vec);
            return pA - pB;
        }

        /// <summary>
        /// 检查两条点是否分别在原点的两侧<br/>
        /// 两侧的依据是，过任一点到原点的向量做垂线，得到的垂线将两点分割到左右两侧，简化后就是点乘结果大于0
        /// </summary>
        private static bool isCrossingOrigin(in Vector2 pA, in Vector2 pB) => Vector2.Dot(pA, -pB) >= 0;

        /// <summary>
        /// 找到多边形距离原点最近的边
        /// </summary>
        private static Vector2 GetCloestEdgeToOrigin(List<Vector2> poly, out float index1, out float index2)
        {
            int n = poly.Count;
            float minDist = float.MaxValue;
            int minIndex = -1;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float dist = Consts.GetDistFromPoint2Line(Vector2.zero, poly[i], poly[j]);
                if (dist < minDist)
                {
                    minDist = dist;
                    minIndex = i;
                }
            }
            if (minIndex == 0)
            {
                index1 = 0;
                index2 = n - 1;
                return poly[0] - poly[n - 1];
            }
            index1 = minIndex;
            index2 = minIndex - 1;
            return poly[minIndex] - poly[minIndex - 1];
        }
    }
}