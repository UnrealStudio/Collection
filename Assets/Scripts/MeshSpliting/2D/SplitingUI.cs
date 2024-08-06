using UnityEngine;
using UnityEngine.UI;

namespace Spliting
{
    public class SplitingUI : MonoBehaviour
    {
        public static SplitingUI instance;

        public void Awake()
        {
            instance = this;
        }

        public void Start()
        {
            FindObjectOfType<MeshGen>().CreateMesh();
            transform.Find("RestartBtn").GetComponent<Button>().onClick.AddListener(() =>
            {
                var sliceObjects = FindObjectsOfType<MeshGen>();
                for (int i = 1; i < sliceObjects.Length; i++)
                {
                    Destroy(sliceObjects[i].gameObject);
                }
                sliceObjects[0].CreateMesh();
                sliceObjects[0].transform.position = new(0, 1, 0);
            });
        }
    }
}