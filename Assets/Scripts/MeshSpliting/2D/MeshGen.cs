using System.Collections.Generic;
using UnityEngine;

namespace Spliting
{
    public class MeshGen : MonoBehaviour
    {
        private Mesh m_Mesh;
        private MeshFilter m_MeshFilter;
        public float size = 0.5f;
        public List<Vector3> verts;
        public List<int> tris;
        public List<Vector2> uvs;

        public void CreateMesh()
        {
            if (!m_MeshFilter) m_MeshFilter = GetComponent<MeshFilter>();
            m_Mesh = new Mesh()
            {
                name = "Test Mesh"
            };
            //添加顶点
            verts = new()
        {
            new(-size, -size),
            new(-size, size),
            new(size, size),
            new(size, -size)
        };
            //添加三角面，三角面是能复用顶点的，并且注意不要弄反
            tris = new()
        {
            0, 1, 2,
            0, 2, 3,
        };
            //添加UV，UV和顶点是一一对应的，正方形容易实现
            uvs = new()
        {
            new(0, 0),
            new(0, 1),
            new(1, 1),
            new(1, 0)
        };
            //将数据作用到Mesh和MeshFilter里
            m_Mesh.SetVertices(verts);
            m_Mesh.triangles = tris.ToArray();
            m_Mesh.uv = uvs.ToArray();

            m_MeshFilter.mesh.Clear();
            m_MeshFilter.mesh = m_Mesh;
            m_Mesh.RecalculateNormals();
        }
    }
}