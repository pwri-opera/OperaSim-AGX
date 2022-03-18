using System.Collections.Generic;
using AGXUnity.Model;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PWRISimulator
{
    /// <summary>
    /// GeneratorPointCloudというメソッド経由で、指示したグリッドにUnityのTerrainの高さを取得し、ポイントごとの座標を配列として
    /// 返す機能を提供する。
    /// </summary>
    public class TerrainPointCloudGenerator : PointCloudGenerator
    {
        #region Inspector Properties
        [Header("Layout")]
        public float xSize = 10;
        public float zSize = 10;
        public float pointDistance = 0.1f;

        [MiniLabel("Position and Rotation are set by Origin Point Object transformation")]
        public GameObject originPointObject;

        [Header("Visuals")]
        public bool showRangeInSceneWindow = true;
        public bool showPointVisuals = false;
        public Mesh visualMesh;
        [Range(0.1f, 2.0f)]
        public float visualScale = 1.0f;
        public Color visualColor = new Color(0.3f, 0.9f, 1.0f);

        #endregion

        #region Private Variables

        float[] terrainPointsAsFloats = new float[0];

        bool visualsMatricesNeedUpdate = false;
        Material visualMaterial;
        readonly List<Matrix4x4[]> visualPointMatrices = new List<Matrix4x4[]>();

        #endregion

        #region C# Properties

        public bool flippedXAxis { get; private set; } = false;

        public Vector3 originPosition { get { return originPointObject != null ? originPointObject.transform.position : Vector3.zero; } }

        public float originAngle { get { return originPointObject != null ? originPointObject.transform.rotation.eulerAngles.y : 0; } }

        public Quaternion originRotation { get { return Quaternion.Euler(0, originAngle, 0); } }

        #endregion

        #region Public Methods
        
        /// <summary>
        /// xSize、zSize、pointDistance、angleで定義されるグリッドのようなポイント配列を作成し、グリッドの各ポイントごとに
        /// Terrainの高さを取得してポイントのy座標に設定し、各ポイントごとのxyz座標を順番的に返す。つまり、p0.x, p0.y, p0.z, 
        /// p1.x, p1.y, p1.zなどの順番となる。
        /// </summary>
        /// <param name="flipX">返す前にx座標の符号を逆にするか</param>
        /// <returns>各ポイントのxyz座標を含む配列。内部の配列データなので、編集したり長い時間保存したりしてはいけない。</returns>
        public override float[] GeneratePointCloud(bool flipX)
        {
            flippedXAxis = flipX;

            Terrain terrain = Terrain.activeTerrain;

            // いくつに区切るかの変数
            int xDivision = 1 + Mathf.RoundToInt(xSize / pointDistance);
            int zDivision = 1 + Mathf.RoundToInt(zSize / pointDistance);
            int numPoints = xDivision * zDivision;

            if (terrainPointsAsFloats == null || terrainPointsAsFloats.Length != numPoints * 3)
            {
                terrainPointsAsFloats = new float[numPoints * 3];
                //Debug.Log($"{name} : Resized floats array to {terrainPointsAsFloats.Length}.");
            }

            float invTerrainSizeX = 1.0f / terrain.terrainData.size.x;
            float invTerrainSizeZ = 1.0f / terrain.terrainData.size.z;
            Vector3 terrainPos = terrain.transform.position;
            Vector3 localOriginPoint = terrain.transform.InverseTransformPoint(originPosition);
            Vector3 localOffsetPerPointX = terrain.transform.InverseTransformVector(originRotation * Vector3.right * pointDistance);
            Vector3 localOffsetPerPointZ = terrain.transform.InverseTransformVector(originRotation * Vector3.forward * pointDistance);
            float localOriginOffsetX = localOriginPoint.x;
            float localOriginOffsetZ = localOriginPoint.z;

            int floatId = 0;
            for (int i = 0; i < xDivision; i++)
            {
                for (int j = 0; j < zDivision; j++)
                {
                    float localX = localOriginOffsetX + localOffsetPerPointX.x * i + localOffsetPerPointZ.x * j;
                    float localZ = localOriginOffsetZ + localOffsetPerPointX.z * i + localOffsetPerPointZ.z * j;

                    // xを書き込む
                    terrainPointsAsFloats[floatId++] = flipX ? -(terrainPos.x + localX) : terrainPos.x + localX;
                    // yを書き込む
                    terrainPointsAsFloats[floatId++] = terrainPos.y + terrain.terrainData.GetInterpolatedHeight(
                                                                            localX * invTerrainSizeX,
                                                                            localZ * invTerrainSizeZ);
                    // zを書き込む
                    terrainPointsAsFloats[floatId++] = terrainPos.z + localZ;
                }
            }

            visualsMatricesNeedUpdate = true;

            return terrainPointsAsFloats;
        }

        #endregion

        #region Private Methods

        void Reset()
        {
            // デフォルトでこのComponentのGameObjectを使用
            originPointObject = gameObject;
        }
        
        void Update()
        {
            if (showPointVisuals)
            {
                UpdateVisualMatrices();
            }
        }

        /// <summary>
        /// Sceneウィンドウ内にポイント取得するエリアを表示する。
        /// </summary>
        void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            // ペアレントが選択されたときに表示しないように
            if (!Selection.Contains(gameObject))
                return;
#endif

            if (showRangeInSceneWindow)
            {
                Matrix4x4 transform = Matrix4x4.TRS(originPosition, originRotation, Vector3.one);
                Gizmos.matrix = transform;

                Vector3 size = new Vector3(xSize, 2.0f, zSize);
                Vector3 localPos = 0.5f * size;
                Color color = visualColor;

                color.a = 0.3f;
                Gizmos.color = color;
                Gizmos.DrawCube(localPos, size);

                color.a = 1.0f;
                Gizmos.color = color;
                Gizmos.DrawWireCube(localPos, size);
            }
        }

        /// <summary>
        /// Scene及びGameウィンドウに生成されている各Pointをmeshとして表示する。
        /// </summary>
        void UpdateVisualMatrices()
        {
            if (!showPointVisuals)
                return;

            // Meshが設定されていない場合はデフォルトSphereを使用
            if (visualMesh == null)
                visualMesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");

            // インスタンシングを対応しているMaterialを作成
            if (visualMaterial == null)
            {
                visualMaterial = new Material(Shader.Find("Standard"));
                visualMaterial.enableInstancing = true;
                visualMaterial.color = visualColor;
            }

            // DrawMeshInstancedは最大に1023つのオブジェクトをレンダリングできるので、より多い場合は複数回呼び出さないといけない
            int numPoints = terrainPointsAsFloats.Length / 3;
            while (numPoints / 1023 + 1 > visualPointMatrices.Count)
            {
                visualPointMatrices.Add(new Matrix4x4[1023]);
                Debug.Log($"{name} : Added array to visualMatrixes. Num arrays = {visualPointMatrices.Count}");
            }

            // ビジュアルオブジェクトのトランスフォーム行列を更新
            if (visualsMatricesNeedUpdate)
            {
                Vector3 meshScale = Vector3.one * pointDistance * visualScale;
                int firstFloatId = 0;
                for (int arrayIndex = 0; arrayIndex < (numPoints / 1023 + 1); ++arrayIndex)
                {
                    Matrix4x4[] matrices = visualPointMatrices[arrayIndex];
                    int numPointsInArray = Mathf.Min(1023, numPoints - arrayIndex * 1023);
                    for (int i = 0; i < numPointsInArray; ++i)
                    {
                        matrices[i].SetTRS(new Vector3(
                            terrainPointsAsFloats[firstFloatId + 0] * (flippedXAxis ? -1 : 1),
                            terrainPointsAsFloats[firstFloatId + 1],
                            terrainPointsAsFloats[firstFloatId + 2]),
                            Quaternion.identity,
                            meshScale);

                        firstFloatId += 3;
                    }
                }
                visualsMatricesNeedUpdate = false;
            }

            // インスタンシングを利用してビジュアルオブジェクトをレンダリングする
            for (int i = 0; i < numPoints; i += 1023)
            {
                Graphics.DrawMeshInstanced(visualMesh,
                                           0,
                                           visualMaterial,
                                           visualPointMatrices[i / 1023],
                                           Mathf.Min(1023, numPoints - i),
                                           null,
                                           castShadows: UnityEngine.Rendering.ShadowCastingMode.Off,
                                           receiveShadows: false);
            }
        }

        #endregion
    }

    #region Editor

#if UNITY_EDITOR
    [CustomEditor(typeof(TerrainPointCloudGenerator))]
    public class TerrainHeightEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            GUILayout.Label("Commands", EditorStyles.boldLabel);
            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("Update Point Cloud", GUILayout.Width(200)))
            {
                var generator = target as TerrainPointCloudGenerator;
                generator.GeneratePointCloud(flipX: generator.flippedXAxis);
            }
        }
    }
#endif

    #endregion
}