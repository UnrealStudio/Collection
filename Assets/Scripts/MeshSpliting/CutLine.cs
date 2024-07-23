using UnityEngine;

namespace Spliting
{
    public class CutLine : MonoBehaviour
    {
        [SerializeField] private LineRenderer LR;
        [SerializeField] private Vector3 StartPos;    //切割线的开始点
        [SerializeField] private Vector3 EndPos;      //切割线的终止点
        public bool isDrawing;      //是否正在切割

        public SliceObject[] sliceObjects;      //场景中的全部SliceObject

        public void Start()
        {
            LR = GetComponent<LineRenderer>();
            LR.positionCount = 2;
        }

        public void Update()
        {
            if (Input.GetMouseButtonDown(0) && !isDrawing)
            {
                //按下鼠标时，取得开始点
                isDrawing = true;

                //获取物体的的世界坐标，转换为屏幕坐标，得到物体所在位置的深度；
                Vector3 screenPosition = Camera.main.WorldToScreenPoint(transform.position);
                //获取鼠标的屏幕坐标
                Vector3 mousePositionOnScreen = Input.mousePosition;
                //为鼠标的屏幕坐标赋值深度
                mousePositionOnScreen.z = screenPosition.z;
                //将鼠标的屏幕坐标转换为世界坐标
                StartPos = Camera.main.ScreenToWorldPoint(mousePositionOnScreen);
                //设置起始点
                LR.SetPosition(0, StartPos);
            }
            if (Input.GetMouseButton(0))
            {
                //按住鼠标时，实时更新终止点
                Vector3 screenPosition = Camera.main.WorldToScreenPoint(transform.position);
                Vector3 mousePositionOnScreen = Input.mousePosition;
                mousePositionOnScreen.z = screenPosition.z;
                EndPos = Camera.main.ScreenToWorldPoint(mousePositionOnScreen);
                LR.SetPosition(1, EndPos);
            }
            if (Input.GetMouseButtonUp(0) && isDrawing)
            {
                //抬起鼠标时，进行一次切割判断
                isDrawing = false;
                Slice();
            }
        }

        private void Slice()
        {
            //获取场景中的全部SliceObject
            sliceObjects = FindObjectsOfType<SliceObject>();
            if (sliceObjects.Length == 0)
            {
                Debug.Log("未找到切割物体");
                return;
            }
            for (int i = 0; i < sliceObjects.Length; i++)
            {
                //将开始点和终止点由世界坐标系转为物体子坐标系
                Vector2 startPosLocal = sliceObjects[i].transform.InverseTransformPoint(StartPos);
                Vector2 endPosLocal = sliceObjects[i].transform.InverseTransformPoint(EndPos);
                //将切割的开始结束点传给物体并进行Slice计算
                sliceObjects[i].Slice(startPosLocal, endPosLocal);
            }
        }
    }
}
