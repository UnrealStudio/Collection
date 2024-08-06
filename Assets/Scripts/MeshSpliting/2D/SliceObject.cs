using System.Collections.Generic;
using UnityEngine;

namespace Spliting
{
    public class SliceObject : MonoBehaviour
    {
        private static int ObjectCount = 1;

        ///<summary>切割函数</summary>
        ///<param name="startPos">切割起始点</param>
        ///<param name="endPos">切割终止点</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0018:内联变量声明", Justification = "<挂起>")]
        public void Slice(Vector2 startPos, Vector2 endPos)
        {
            #region 获取Mesh信息

            MeshFilter MF = GetComponent<MeshFilter>();
            Mesh mesh = MF.mesh;
            //获取当前Mesh的顶点、三角面、UV信息
            List<Vector3> vertList = new(mesh.vertices);
            List<int> triList = new(mesh.triangles);
            List<Vector2> uvList = new(mesh.uv);

            int preVertCount = vertList.Count;

            #endregion 获取Mesh信息

            #region 判断是否为有效切割

            //如果完全没有交点且跑出循环了，代表整个切割线都在图形外侧，属于不合法切割
            if (!IsEffectiveCutting(triList, vertList, startPos, endPos))
            {
                Debug.Log($"{name}无法切割");
                return;
            }

            #endregion 判断是否为有效切割

            #region 切割

            List<Vector3> CrossVertList = new();
            List<Vector2> CrossUVList = new();
            //遍历三角面，每3个三角顶点为一个三角面进行遍历
            for (int i = 0; i < triList.Count; i += 3)
            {
                int triIndex0 = triList[i];
                int triIndex1 = triList[i + 1];
                int triIndex2 = triList[i + 2];

                Vector2 point0 = vertList[triIndex0];
                Vector2 point1 = vertList[triIndex1];
                Vector2 point2 = vertList[triIndex2];
                //分别代表与一个三角面的01边、12边、02边的交点
                Vector2 crossPoint0_1, crossPoint1_2, crossPoint0_2;
                //如果与01边和12边有交点
                if (Consts2D.HasCrossPoint(startPos, endPos, point0, point1, out crossPoint0_1) &&
                    Consts2D.HasCrossPoint(startPos, endPos, point1, point2, out crossPoint1_2))
                {
                    //为两个交点计算UV坐标
                    CrossUVList.Add(GetUVPoint(uvList[triIndex0], uvList[triIndex1], point0, point1, crossPoint0_1));
                    CrossUVList.Add(GetUVPoint(uvList[triIndex1], uvList[triIndex2], point1, point2, crossPoint1_2));

                    CrossVertList.Add(crossPoint0_1);
                    CrossVertList.Add(crossPoint1_2);
                }
                else if (Consts2D.HasCrossPoint(startPos, endPos, point1, point2, out crossPoint1_2) &&
                         Consts2D.HasCrossPoint(startPos, endPos, point2, point0, out crossPoint0_2))
                {
                    CrossUVList.Add(GetUVPoint(uvList[triIndex1], uvList[triIndex2], point1, point2, crossPoint1_2));
                    CrossUVList.Add(GetUVPoint(uvList[triIndex0], uvList[triIndex2], point0, point2, crossPoint0_2));

                    CrossVertList.Add(crossPoint1_2);
                    CrossVertList.Add(crossPoint0_2);
                }
                else if (Consts2D.HasCrossPoint(startPos, endPos, point0, point1, out crossPoint0_1) &&
                         Consts2D.HasCrossPoint(startPos, endPos, point2, point0, out crossPoint0_2))
                {
                    CrossUVList.Add(GetUVPoint(uvList[triIndex0], uvList[triIndex1], point0, point1, crossPoint0_1));
                    CrossUVList.Add(GetUVPoint(uvList[triIndex0], uvList[triIndex2], point0, point2, crossPoint0_2));

                    CrossVertList.Add(crossPoint0_1);
                    CrossVertList.Add(crossPoint0_2);
                }
            }

            //算出切割线上距离起始点最近的端点和最远的端点
            //再优化思路：通过InnerGraphByAngle方法，不计算距离而直接计算交点是否在凸多边形内部
            //最近和最远的端点相对而言一定在凸多边形的边上，起到完美分割的作用
            int nearestPointIndex = 0;
            int farestPointIndex = 0;
            if (CrossVertList.Count > 2)
            {
                float mx = 0, mn = 1;
                for (int i = 0; i < CrossVertList.Count; i++)
                {
                    Vector2 v = CrossVertList[i];
                    float relate = GetRelate(startPos, endPos, v);
                    if (relate > mx)
                    {
                        mx = relate;
                        farestPointIndex = i;
                    }
                    if (relate < mn)
                    {
                        mn = relate;
                        nearestPointIndex = i;
                    }
                }
            }
            else
            {
                if (GetRelate(startPos, endPos, CrossVertList[0]) > GetRelate(startPos, endPos, CrossVertList[1]))
                {
                    farestPointIndex = 0;
                    nearestPointIndex = 1;
                }
                else
                {
                    farestPointIndex = 1;
                    nearestPointIndex = 0;
                }
            }
            //最近和最远只是相对起始端点的，reMesh的思路是一定从第0个端点开始reMesh，所以具有方向性
            //方向性：正切和倒切结果不同
            Vector2 toZeroNear = vertList[0] - CrossVertList[nearestPointIndex];
            Vector2 toZeroFar = vertList[0] - CrossVertList[farestPointIndex];
            //反方向的需要修正，反方向的判定则根据叉乘结果得到（第0个顶点与两个交点一定以逆时针顺序进行）
            float crossProduct = (toZeroFar.x * toZeroNear.y) - (toZeroFar.y * toZeroNear.x);
            bool corrective = crossProduct < 0;
            if (corrective)
            {
                (nearestPointIndex, farestPointIndex) = (farestPointIndex, nearestPointIndex);
            }

            #endregion 切割

            #region 测试

            //定义左侧是什么
            OldMeshData LeftMesh = new();
            OldMeshData RightMesh = new();
            bool LeftPos = Consts2D.IsPointOnLeftSideOfLine(vertList[0], startPos, endPos);
            int ExceedLineTime = 0;
            LeftMesh.AddVert(vertList[0], uvList[0]);
            for (int i = 1; i < preVertCount; ++i)
            {
                bool Pos = Consts2D.IsPointOnLeftSideOfLine(vertList[i], startPos, endPos);
                //第一次见到
                if (Pos != LeftPos)
                {
                    //初次越界
                    if (ExceedLineTime == 0)
                    {
                        LeftMesh.AddVert(CrossVertList[nearestPointIndex], CrossUVList[nearestPointIndex]);
                        RightMesh.AddVert(CrossVertList[nearestPointIndex], CrossUVList[nearestPointIndex]);
                        ExceedLineTime++;
                    }
                    RightMesh.AddVert(vertList[i], uvList[i]);
                }
                else
                {
                    //越界回来
                    if (ExceedLineTime == 1)
                    {
                        LeftMesh.AddVert(CrossVertList[farestPointIndex], CrossUVList[farestPointIndex]);
                        RightMesh.AddVert(CrossVertList[farestPointIndex], CrossUVList[farestPointIndex]);
                        ExceedLineTime++;
                    }
                    LeftMesh.AddVert(vertList[i], uvList[i]);
                }
            }
            if (ExceedLineTime == 1)
            {
                LeftMesh.AddVert(CrossVertList[farestPointIndex], CrossUVList[farestPointIndex]);
                RightMesh.AddVert(CrossVertList[farestPointIndex], CrossUVList[farestPointIndex]);
                //ExceedLineTime++;
            }
            LeftMesh.GenTri();
            RightMesh.GenTri();

            #endregion 测试

            #region 分离成两个Mesh

            //生成切割图形，并设置Mesh
            GameObject rightGo = Instantiate(gameObject, transform.position, transform.rotation);
            rightGo.name = $"Mesh{++ObjectCount}";
            SliceObject so = rightGo.GetComponent<SliceObject>();
            so.SetMesh(RightMesh.VertList, RightMesh.TriList, RightMesh.UVList);
            SetMesh(LeftMesh.VertList, LeftMesh.TriList, LeftMesh.UVList);

            //分离切割后的图形，以切割线为标准，将分割后的两物体向相反方向位移
            //由于具有方向性，如果经过了corrective修正则需要反过来移动（不然是相向移动）
            Vector2 normalline = RotateVectorHalfPi(startPos - endPos).normalized;
            if (corrective) normalline *= -1;
            rightGo.transform.position -= (Vector3)normalline / 10;
            transform.position += (Vector3)normalline / 10;

            #endregion 分离成两个Mesh
        }

        private Vector2 RotateVectorHalfPi(Vector2 vector)
        {
            return new Vector2(-vector.y, vector.x);
        }

        /// <summary>
        /// 设置网格体并更新法向量
        /// </summary>
        /// <param name="vList">顶点列表</param>
        /// <param name="tList">三角面列表</param>
        /// <param name="uvList">UV列表</param>
        public void SetMesh(List<Vector3> vList, List<int> tList, List<Vector2> uvList)
        {
            MeshFilter MF = GetComponent<MeshFilter>();
            //一定要清空，否则永远不知道会发生什么错误
            MF.mesh.Clear();
            //重新赋值顶点，由于多了交点的顶点，三角面和顶点都与切割前不同
            MF.mesh.SetVertices(vList);

            //重新赋值三角面与UV顶点
            MF.mesh.SetTriangles(tList.ToArray(), 0);
            MF.mesh.SetUVs(0, uvList.ToArray());
            MF.mesh.RecalculateNormals();
        }

        /// <summary>
        /// 获取三角面的一边的中间点，相对边的两个顶点位置与UV坐标得到的UV点
        /// </summary>
        /// <param name="startUV">边的起点UV</param>
        /// <param name="endUV">边的终点</param>
        /// <param name="startPoint">边的起点</param>
        /// <param name="endPoint">边的终点</param>
        /// <param name="curPoint">待计算UV的中间点</param>
        /// <returns>中间点的UV</returns>
        public static Vector2 GetUVPoint(in Vector2 startUV, in Vector2 endUV, in Vector2 startPoint, in Vector2 endPoint, Vector2 curPoint)
        {
            //计算出中间点相对起点的距离
            float relate = GetRelate(startPoint, endPoint, curPoint);
            // Mathf.Lerp函数的返回值本质含义是，a到b执行了t的进度时，应该在哪里
            return new Vector2(Mathf.Lerp(startUV.x, endUV.x, relate),
                               Mathf.Lerp(startUV.y, endUV.y, relate));
        }

        public static float GetRelate(in Vector2 startPoint, in Vector2 endPoint, in Vector2 curPoint)
        {
            float relate = (startPoint - curPoint).magnitude / (startPoint - endPoint).magnitude;
            return relate;
        }

        /// <summary>
        /// 判断起始点组成线段的切割是否有效
        /// </summary>
        /// <param name="triList">三角面数组</param>
        /// <param name="vertList">顶点数组</param>
        /// <param name="startPos">起始点</param>
        /// <param name="endPos">终止点</param>
        /// <returns>是否有效</returns>
        private static bool IsEffectiveCutting(List<int> triList, List<Vector3> vertList, in Vector2 startPos, in Vector2 endPos)
        {
            bool isEffective = false;
            //遍历三角面，每3个三角顶点为一个三角面进行遍历
            for (int i = 0; i < triList.Count; i += 3)
            {
                int triIndex0 = triList[i];
                int triIndex1 = triList[i + 1];
                int triIndex2 = triList[i + 2];

                Vector2 point0 = vertList[triIndex0];
                Vector2 point1 = vertList[triIndex1];
                Vector2 point2 = vertList[triIndex2];
                //判断切割线与每个三角面的任意边是否有交点
                if (Consts2D.HasCrossPoint(startPos, endPos, point0, point1, out _) ||
                    Consts2D.HasCrossPoint(startPos, endPos, point1, point2, out _) ||
                    Consts2D.HasCrossPoint(startPos, endPos, point2, point0, out _))
                {
                    isEffective = true;
                }
                //判断是否有交点后判断是否是不合法的切割线，如果不合法直接return
                //原理：任何一点在任意三角面内，都代表整个物体没有被合法切割
                if (Consts2D.InnerGraphByAngle(startPos, point0, point1, point2) ||
                    Consts2D.InnerGraphByAngle(endPos, point0, point1, point2))
                {
                    // Debug.Log("切割不完整，有至少一个点在图片内");
                    return false;
                }
            }
            if (!isEffective)
            {
                // Debug.Log("无效切割！没有交点");
                return false;
            }
            return true;
        }
    }

    public class MeshData
    {
        public List<Vector3> VertList = new(64);
        public List<int> TriList = new(64 * 3);
        public List<Vector2> UVList = new(64);

        //Key代表原Mesh中某顶点下标，Value代表在现在的Mesh中的顶点下标
        private readonly Dictionary<int, int> vertMap = new();

        public void AddTriangles(List<Vector3> vList, List<int> tList, List<Vector2> uvList, int index)
        {
            for (int i = index; i < index + 3; ++i)
            {
                if (!vertMap.ContainsKey(tList[i]))
                {
                    //Count不等于Capacity!
                    vertMap.Add(tList[i], VertList.Count);
                    VertList.Add(vList[tList[i]]);
                    UVList.Add(uvList[tList[i]]);
                }
                TriList.Add(vertMap[tList[i]]);
            }
        }
    }

    public class OldMeshData
    {
        public List<Vector3> VertList = new(64);
        public List<Vector2> UVList = new(64);
        public List<int> TriList = new(64 * 3);

        public void AddVert(Vector3 v, Vector2 uv)
        {
            VertList.Add(v);
            UVList.Add(uv);
        }

        public void GenTri()
        {
            for (int i = 1; i < VertList.Count - 1; i++)
            {
                TriList.Add(0);
                TriList.Add(i);
                TriList.Add(i + 1);
            }
        }
    }
}