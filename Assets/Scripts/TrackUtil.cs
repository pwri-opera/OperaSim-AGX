using System;
using System.Linq;
using UnityEngine;
using AGXUnity;
using AGXUnity.Model;

namespace PWRISimulator
{
    public static class TrackUtil
    {
        /// <summary>
        /// 指示した２つのTrackのそれぞれのsprocketホイールを取得し、separationという出力をsprocketホイール同士の距離に設定し、
        /// radiusという出力をsprocket半径＋Track厚さに設定し、Trueを返す。失敗の場合は、separation、radiusをゼロに設定しFalse
        /// を返す。
        /// </summary>
        public static bool GetSeparationAndTractionRadius(Track trackLeft, Track trackRight, out double separation,
                                                                                             out double radius)
        {
            TrackWheel sprocketLeft = trackLeft?.Wheels.First(x => x.Model == TrackWheelModel.Sprocket);
            TrackWheel sprocketRight = trackRight?.Wheels.First(x => x.Model == TrackWheelModel.Sprocket);
            if (sprocketLeft != null && sprocketRight != null)
            {
                separation = Vector3.Distance(sprocketLeft.Frame.Position, sprocketRight.Frame.Position);
                radius = sprocketLeft.Radius + trackLeft.Thickness;
                return true;
            }
            else
            {
                separation = 0.0;
                radius = 0.0;
                return false;
            }
        }

        /// <summary>
        /// ConstraintのReferenceObjectまたはConnectedObjectに直接に挿入したTrackWheelコンポネントを探し、返す。
        /// 見つけない場合は、searchInChildren=Trueだったら、ReferenceObjectそしてConnectedObjectの階層に
        /// TrackWheelコンポネントをまた探し、返す。
        /// </summary>
        public static TrackWheel GetTrackWheel(Constraint wheelConstraint, TrackWheelModel? model, bool searchChildren)
        {
            AttachmentPair pair = wheelConstraint?.AttachmentPair;
            if (pair == null)
                return null;

            // ２回探してみる：１回目は２つのGameObjectの直接のコンポネントだけ探すが、２回目は２つのGameObjectの子階層にも探す。
            for (int i = 0; i < (searchChildren ? 2 : 1); ++i)
            {
                // Constraintが繋ぐ２つのGameObjectにTrackWheelを探す
                foreach (var obj in new GameObject[]{ pair.ReferenceObject, pair.ConnectedObject })
                {
                    if (obj == null)
                        continue;

                    Func<TrackWheel, bool> condition = w =>
                        w != null && w.RigidBody.gameObject == obj && (!model.HasValue || w.Model == model);
                    
                    // gameObjectのコンポネントだけ探す
                    if (i == 0)
                    {
                        TrackWheel wheel = obj.GetComponent<TrackWheel>();
                        if (condition(wheel))
                            return wheel;
                    }
                    // gameObjectの子供のコンポネントも探す
                    else
                    {
                        TrackWheel wheel = obj.GetComponentsInChildren<TrackWheel>().First(condition);
                        if (wheel != null)
                            return wheel;
                    }
                }
            }
            return null;
        }
    }
}
