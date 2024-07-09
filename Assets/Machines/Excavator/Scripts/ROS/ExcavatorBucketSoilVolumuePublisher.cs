using UnityEngine;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// ショベルのバケット内土量を出力する
    /// </summary>
    public class ExcavatorBucketSoilVolumePublisher : SoilVolumePublisher
    {
        [SerializeField] uint frequency = 60;
        ExcavationData data;

        protected override void DoStart()
        {
            data = gameObject.GetComponent<ExcavationData>();
            if (data == null)
            {
                Debug.LogError("ExcavationData is not found");
            }
        }

        protected override void DoUpdate()
        {
            soilVolumeMsg.data = data.shovelSoilVolume;
        }

        protected override uint Frequency()
        {
            return frequency;
        }

        protected override string MachineName()
        {
            return gameObject.name;
        }

        protected override string TopicPhrase()
        {
            return "/soil_volume";
        }
    }
}
