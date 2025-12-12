using UnityEngine;
/// <summary>
/// 二つのオブジェクト間の距離（x方向, y方向, z方向及びそれらのノルム）を取得する
/// </summary>
 
[ExecuteAlways]
public class DistanceCalculator : MonoBehaviour
{
    public string name = "";

    [Tooltip("距離を計算する対象の一つ目のオブジェクト")]
    public GameObject object1;

    [Tooltip("距離を計算する対象の二つ目のオブジェクト")]
    public GameObject object2;

    [Header("Result")]
    [SerializeField] private float distance;   // Inspector 上に表示用
    [SerializeField] private float dist_x;   // Inspector 上に表示用
    [SerializeField] private float dist_y;   // Inspector 上に表示用
    [SerializeField] private float dist_z;   // Inspector 上に表示用
    void Update()
    {
        if (object1 != null && object2 != null)
        {
            float dist = Vector3.Distance(object1.transform.position, object2.transform.position);
            dist_x = object1.transform.position.x - object2.transform.position.x;
            dist_y = object1.transform.position.y - object2.transform.position.y;
            dist_z = object1.transform.position.z - object2.transform.position.z;

            // Debug.Log("Distance between object1 and object2: " + dist);
            // Debug.Log("Distance x: " + dist_x + ",\t y: " + dist_y + ",\t z: " + dist_z);
            distance = dist;
        }
    }
}
