using UnityEngine;

/// <summary>
/// 三つのオブジェクトの位置から角ABCの内角（Bの角度）を計算して Inspector に表示する
/// </summary>
 
[ExecuteAlways]
public class AngleCalculator : MonoBehaviour
{
    public string name = "";

    [Tooltip("角度を求める対象の A 点オブジェクト")]
    public GameObject objectA;

    [Tooltip("角度を求める対象の B 点オブジェクト（角の頂点）")]
    public GameObject objectB;

    [Tooltip("角度を求める対象の C 点オブジェクト")]
    public GameObject objectC;

    [Header("Result")]
    [SerializeField]
    private float angle;   // Inspector 上に表示用
    [SerializeField]
    private float angleDeg;   // Inspector 上に表示用

    void Update()
    {
        if (objectA != null && objectB != null && objectC != null)
        {
            Vector3 BA = objectA.transform.position - objectB.transform.position;
            BA.x = 0.0f;
            Vector3 BC = objectC.transform.position - objectB.transform.position;
            BC.x = 0.0f;

            angle = Mathf.Deg2Rad * Vector3.Angle(BA, BC);
            angleDeg = Mathf.Rad2Deg * angle;
        }
        else
        {
            angle = 0f;
            Debug.LogWarning("AngleCalculator: オブジェクトの指定が不十分です。objectA, objectB, objectC をすべて設定してください。");
        }
    }
}
