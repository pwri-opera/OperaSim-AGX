using UnityEngine;

namespace PWRISimulator
{
    /// <summary>
    /// 点群データGeneratorのベースクラス。具体的なGeneratorはこのクラスを継承してGeneratePointCloudメソッドをインプリメントする。
    /// </summary>
    public abstract class PointCloudGenerator : MonoBehaviour
    {
        public abstract float[] GeneratePointCloud(bool flipX);
    }
}
