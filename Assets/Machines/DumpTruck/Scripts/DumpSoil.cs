using System;
using UnityEngine;
using AGXUnity;
using AGXUnity.Collide;
using AGXUnity.Model;
using AGXUnity.Utils;
using Math = System.Math;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

using Debug = UnityEngine.Debug;

namespace PWRISimulator
{
    /// <summary>
    /// パフォーマンスのために、このクラスが記載するMerge Zoneというボックスに粒子を入れると、粒子を一時的に消して全て入れた粒子
    /// の総量を一つ表面で可視化する。入れた粒子の総量によって表面の高さが変わる。Merge Zoneが付いている荷台剛体が斜めとなるよう
    /// に昇降されると、消した粒子の総量に対して後ろの出口から粒子が再生成されて出る。
    /// </summary>
    /// <remarks>
    /// 土砂表面を可視化するために、このComponentと同じGameObjectに以降の２つComponentが挿入されている必要がある：
    /// 1. DumpSoil.objというMeshが設定されているMesh Filter
    /// 2. DumpSoilMatというMaterial、またはDumpSoilShaderを使う他のMaterial、が設定されているMesh Renderer
    /// </remarks>
    [RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer))]
    public class DumpSoil : ScriptComponent
    {
        #region Inspector Properties

        [Header("Loading")]

        [Tooltip("荷台にマージされた土砂の総量が建機を影響するかどうか（荷台RigidBodyとジョイントで繋がるRigidBodyとして扱う）")]
        public bool addSoilMassRigidBody = true;


        [Header("Unloading")]

        [Tooltip("後ろから粒子が生成できるかどうか。Play時にドアのロック状態によってTrue/Falseに調整するはずだ")]
        public bool spawnParticlesEnabled = true;

        [Range(0.1f, 1)]
        [Tooltip("SpawnZoneの幅のスケール、1.0はMergeZone幅と同一となる")]
        public float spawnZoneWidthScale = 0.9f;

        [Range(0, 90)]
        [Tooltip("最低の放土角度。荷台昇降がこの角度を超えたら放土機能が有効になるが、摩擦などによってより大きい角度の必要のことがある。")]
        public float mininumDumpAngle = 10.0f;

        [Range(0.01f, 10.0f)]
        [Tooltip("放土の速度の上限（m/s）")]
        public float maximumSoilSpeed = 2.0f;

        [Range(0, 1)]
        [Tooltip("荷台の周囲と中に入っている土砂の間の摩擦係数。放土速度を影響する。")]
        public float frictionCoefficient = 0.4f;

        [Range(0, 100)]
        [Tooltip("SpawnZoneの出口に詰まった粒子が多すぎたら、frictionCoefficientをかける数値（放土速度が下がるために）")]
        public float fullSpawnZoneFrictionScale = 3.0f;

        [Range(0, 1)]
        public float fullSpawnZoneMarginFactor = 0.2f;

        [Header("Unloading Push Force")]

        [Range(0, 10000)]
        public float pushForceMinSoilMass = 200.0f;

        [Range(0, 10000)]
        public float pushForceMaxSoilMass = 1000.0f;

        [Tooltip("放土時に出口に生成した粒子に後ろ方向の力をかけるかどうか")]
        public bool particlesPushForceEnabled = true;

        [Range(0.1f, 10f)]
        public float particlesPushForceScale = 1.0f;

        [Tooltip("放土時にドアの剛体に後ろ方向の力をかけるかどうか")]
        public bool doorPushForceEnabled = true;

        [Range(0.1f, 10f)]
        public float doorPushForceScale = 1.0f;

        [Tooltip("放土時にドアの剛体に後ろ方向の力をかけるかどうか")]
        [ConditionalHide(nameof(doorPushForceEnabled), hideCompletely = true)]
        public RigidBody doorBody;
        

        [Header("Visuals")]

        [Tooltip("SceneウィンドウにMergeZoneを表示するか")]
        public bool showMergeZone = true;

        [Tooltip("SceneウィンドウにSpawnZoneを表示するか（Play時のみ）")]
        public bool showSpawnZone = true;

        [Range(0, 2)]
        public float soilVisualSpeedScale = 1.0f;


        [Header("Overrides (auto-assigned on Play)")]

        public DeformableTerrain terrain;
        public RigidBody containerBody;

        [Header("Output")]

        [InspectorLabel("Enabled")]
        public bool showOutputInInspector = false;

        #endregion

        #region Properties

        // 荷台とマージした粒子の総量。
        //public double soilMass { get; private set; } = 0.0;
        public double soilMass { get; set; } = 0.0;

        // 現在の放土速度。
        public double soilSpeed { get; private set; } = 0.0;

        public double soilHeight { get { return mergeZoneHorizontalArea != 0.0 ? soilVolume / mergeZoneHorizontalArea : 0.0; } }

        public double soilVolume { get { return nominalParticleData.density != 0.0 ? soilMass / nominalParticleData.density : 0.0; } }
        
        public float tiltAngle { get { return Mathf.Abs(Mathf.Asin(forwardDir.y)) * Mathf.Rad2Deg; } }

        Vector3 localForwardDir { get { return Vector3.forward; } }

        Vector3 forwardDir { get { return transform.TransformDirection(localForwardDir); } }

        double maxNumParticlesInSpawnZone { get { return nominalParticleData.area != 0.0 ? spawnZoneVerticalArea / spawnParticleData.area : 0.0; } }

        Vector3 mergeZoneOriginalSize { get { return transform.localScale; } }

        Vector3 mergeZoneCurrentSize { get { return new Vector3(mergeZoneOriginalSize.x, (float)soilHeight, mergeZoneOriginalSize.z); } }

        Vector3 mergeZoneOriginalLocalCenterUnscaled { get { return new Vector3(0, 0.5f, 0.5f); } }

        Vector3 mergeZoneCurrentLocalCenterUnscaled { get { return new Vector3(0, 0.5f * (float)soilHeight / transform.localScale.y, 0.5f); } }

        double mergeZoneHorizontalArea { get { return mergeZoneOriginalSize.x * mergeZoneOriginalSize.z; } }
        
        Bounds mergeZoneOriginalBoundsWorld { get { return MathUtil.TransformBounds(transform, mergeZoneOriginalBoundsLocal); } }

        double spawnZoneWidth { get { return mergeZoneOriginalSize.x * spawnZoneWidthScale; } }

        double spawnZoneHeight { get { return Math.Max(soilHeight, spawnParticleData.diameter); } }

        double spawnZoneVerticalArea { get { return spawnZoneWidth * spawnZoneHeight; } }

        #endregion

        #region Capture Classification Utility

        /// <summary>
        /// Pure static utility for local-space capture-zone overlap classification.
        /// Free of AGX runtime dependencies — tested directly in EditMode.
        /// </summary>
        public static class CaptureUtil
        {
            public struct LocalCaptureBounds
            {
                public Vector3 min;
                public Vector3 max;
            }

            /// <summary>
            /// Calculate the local-space capture bounds for the given vessel size and soil height.
            /// X is centered around 0; Z follows the merge-zone convention: min.z = 0, max.z = originalSize.z
            /// (rear-half of the vessel from the original center at local z = 0.5).
            /// </summary>
            public static LocalCaptureBounds CalculateLocalCaptureBounds(Vector3 originalSize, double soilHeight)
            {
                float halfWidth = originalSize.x * 0.5f;
                float y = (float)soilHeight;
                return new LocalCaptureBounds
                {
                    min = new Vector3(-halfWidth, y, 0f),
                    max = new Vector3(halfWidth, y, originalSize.z)
                };
            }

            /// <summary>
            /// Calculate the broad-phase world-space AABB expanded by <paramref name="expansion"/>
            /// on all axes.  Makes the broad-phase bounds expansion rules explicit and testable.
            /// </summary>
            public static LocalCaptureBounds CalculateWorldBroadPhaseBounds(Vector3 worldMin, Vector3 worldMax, double expansion)
            {
                float e = (float)expansion;
                return new LocalCaptureBounds
                {
                    min = new Vector3(worldMin.x - e, worldMin.y - e, worldMin.z - e),
                    max = new Vector3(worldMax.x + e, worldMax.y + e, worldMax.z + e)
                };
            }

            /// <summary>
            /// Returns true if a sphere at <paramref name="localPos"/> with <paramref name="radius"/>
            /// overlaps the axis-aligned capture bounds.
            /// </summary>
            public static bool IsSphereOverlappingBounds(Vector3 localPos, double radius, LocalCaptureBounds bounds)
            {
                float r = (float)radius;
                return !(localPos.x - r > bounds.max.x || localPos.x + r < bounds.min.x ||
                         localPos.z - r > bounds.max.z || localPos.z + r < bounds.min.z ||
                         localPos.y - r > bounds.max.y || localPos.y + r < bounds.min.y);
            }
        }

        #endregion

        #region Private Fields

        // AgxDynamicsの内蔵のTerrainオブジェクト。
        agxTerrain.Terrain terrainNative;

        // Terrainの最大半径のある粒子に対して粒子データ（TerrainのgetParticleNominalRadius()、getMaterial()から計算された）。
        ParticleData nominalParticleData;

        // 荷台から放土する粒子のデータ。
        ParticleData spawnParticleData;

        // 荷台とマージした粒子の総計質量をまねする剛体。荷台剛体とLockコンストレイントで繋がっている。
        RigidBody soilMassBody;

        // MergeZoneの元々の寸法（Editorで設定したの）。
        Bounds mergeZoneOriginalBoundsLocal = new Bounds();
        
        // このGameObjectのペアレント荷台剛体に対して元々の相対的な位置、回転。
        agx.AffineMatrix4x4 transformRelativeToContainerBody;

#if UNITY_ASSERTIONS
        // ProcessCaptureStep()が呼び出された累計回数。主にテスト検証用。
        int captureStepExecutionCount = 0;
#endif

        // Spawnが最新に更新されたGameTimeの時刻。
        double lastSpawnUpdateTime = 0.0;

        bool isRuntimeReady = false;

        // マージされていない（canMerge=falseのせいで）MergeZoneに入っている。
        int numUnmergedParticlesInMergeZone = 0;

        // 現在の昇降角度次第の放土最大速度。
        double maxPotentialSoilSpeed = 0.0;
        
        // ParticleEmitterが粒子を生成するゾーンを定義するボックス。
        agxCollide.Box emitterBox;

        // 内蔵のAgxDynamicsのParticleEmitter。
        agx.ParticleEmitter emitter;

        // ParticleEmitterが開始から今まで生成した粒子の数。
        double emittedQuantity = 0.0;

        // Per-step vessel-local candidate set.  Populated in the broad-phase pass,
        // consumed by the exact overlap and decision passes.  Indices into the
        // current granulars collection, valid only within UpdateCaptureCandidatesPerStep.
        List<int> captureCandidates = new List<int>();

        // Indices of particles confirmed for merge removal.  Batch-processed in
        // descending order to keep indices stable during removal.
        List<int> particlesToMerge = new List<int>();

        #endregion

        #region Public Methods
       
        public void EnableSpawnParticles()
        {
            spawnParticlesEnabled = true;
        }

        public void DisableSpawnParticles()
        {
            spawnParticlesEnabled = false;
        }

        #endregion

        #region Private Methods

        protected override bool Initialize()
        {
            // 自動的にComponentを取得：

            if (terrain == null)
                terrain = FindObjectOfType<DeformableTerrain>();

            if (containerBody == null)
                containerBody = GetComponentInParent<RigidBody>();

            // エラーチェック：

            if (terrain?.GetInitialized<DeformableTerrain>() == null)
                return false;

            if (containerBody?.GetInitialized<RigidBody>() == null)
                return false;

            if (Simulation.Instance?.GetInitialized<Simulation>() == null)
                return false;

            // データの初期：

            mergeZoneOriginalBoundsLocal = new Bounds(mergeZoneOriginalLocalCenterUnscaled, Vector3.one);
            transformRelativeToContainerBody = AgxUtil.GetRelativeAgxTransform(containerBody.transform, transform);
            terrainNative = terrain?.GetInitialized<DeformableTerrain>()?.Native;
            nominalParticleData = ParticleData.CreateFromTerrainProperties(terrain);
            spawnParticleData = nominalParticleData;
            
            if (!CreateSoilMassBody())
                return false;

            if (!CreateEmitter())
                return false;

            StartCoroutine(UpdateParticleDataCoroutine(4.0f));

            isRuntimeReady = true;

            // Register the PostStepForward callback here (not in OnEnable) because
            // OnEnable runs before Start/Initialize, so isRuntimeReady is still false
            // on first activation. OnEnable handles re-registration on re-enable.
            if (Simulation.HasInstance)
                Simulation.Instance.StepCallbacks.PostStepForward += OnPostStepForward;

            return base.Initialize();
        }
        
        bool CreateSoilMassBody()
        {
            if (!addSoilMassRigidBody)
                return true;

            String name;
            GameObject obj = this.transform.root.gameObject;
            if (obj != null)
            {
                name = obj.name;
            }
            else
            {
                Debug.Log("DumpSoil Error Get Parent GameObject");
                name = "DumpSoil";
            }


                // ダンプ土砂の質量を扱うRigidBodyを作成（衝突不可能）
                GameObject bodyObject = new GameObject(name + "_SoilMassBody", typeof(RigidBody));
            bool asChild = GetComponentInParent<ArticulatedRoot>() == null; // ArticulatedRootの子にすると問題が発生するから
            if (asChild)
            {
                bodyObject.transform.parent = gameObject.transform;
                bodyObject.transform.localPosition = new Vector3(0, 0, 0.5f);
                bodyObject.transform.localRotation = Quaternion.identity;
                bodyObject.transform.localScale = Vector3.one;
            }
            else
            {
                bodyObject.transform.position = transform.TransformPoint(
                    mergeZoneOriginalLocalCenterUnscaled.x, 
                    0, 
                    mergeZoneOriginalLocalCenterUnscaled.z);
                bodyObject.transform.rotation = transform.rotation;
            }

            // 質量設定の初期化
            soilMassBody = bodyObject.GetComponent<RigidBody>().GetInitialized<RigidBody>();
            MassProperties massProps = soilMassBody.MassProperties;
            massProps.Mass.UseDefault = false;
            massProps.CenterOfMassOffset.UseDefault = false;
            massProps.InertiaDiagonal.UseDefault = false;
            UpdateSoilMassBody(); // これから、各Updateに呼び出して質量設定を更新させる
            
            // soilMassBodyと荷台のRigidBodyを繋ぐConstraintを作成
            GameObject constraintObject = Factory.Create(ConstraintType.LockJoint, Vector3.zero, Quaternion.identity,
                                                         soilMassBody, containerBody);
            constraintObject.name = name + "_SoilMassJoint";
            constraintObject.transform.parent = bodyObject.transform.parent;
            constraintObject.GetComponent<Constraint>().GetInitialized<Constraint>(); // 初期化させるため
            return true;
        }

        /// <summary>
        /// 現在のダンプ土砂の質量に合わせて、ダンプ土砂を扱うRigidBodyの質量設定を調整する。
        /// </summary>
        void UpdateSoilMassBody()
        {
            if (soilMassBody == null)
                return;

            Vector3 size = mergeZoneCurrentSize;
            float mass = Mathf.Max((float)soilMass, 1f); // 物理エンジンに問題が発生しないため、質量がゼロにならないように
            float inertiaMassFactor = mass / 12f;

            MassProperties massProps = soilMassBody.MassProperties;
            massProps.Mass.Value = mass;
            massProps.CenterOfMassOffset.Value = new Vector3(0, (float)(size.y * 0.5), 0);
            massProps.InertiaDiagonal.Value = new Vector3(
                inertiaMassFactor * (size.y * size.y + size.z * size.z),
                inertiaMassFactor * (size.x * size.x + size.z * size.z),
                inertiaMassFactor * (size.x * size.x + size.y * size.y));
        }

        /// <summary>
        /// AgxDynamicsのParticleEmitterを生成する。それに、ParticleEmitterが粒子を生成するゾーンを定義するBoxも作成。この
        /// Boxの高は、後で荷台の土砂総量が変わると合わせて調整される(UpdateEmitterPositionAndSizeというメソッドから)。
        /// </summary>
        /// <returns></returns>
        bool CreateEmitter()
        {
            // 粒子Emitterを作成
            var granularBodySystem = terrainNative.getSoilSimulationInterface().getGranularBodySystem();
            emitter = new agx.ParticleEmitter(granularBodySystem, agx.Emitter.Quantity.QUANTITY_COUNT);
            emitter.setRate(0);
            emitter.setMaximumEmittedQuantity(0);
            var distTable = new agx.ParticleEmitter.DistributionTable(agx.Emitter.Quantity.QUANTITY_COUNT);
            distTable.addModel(
                new agx.ParticleEmitter.DistributionModel(
                    spawnParticleData.radius,
                    terrain.Native.getMaterial(agxTerrain.Terrain.MaterialType.PARTICLE), 1));
            emitter.setDistributionTable(distTable);
            Simulation.Instance.GetInitialized<Simulation>().Native.add(emitter);

            // 粒子Emitterのエリアを記載するBox Shape（衝突不可能）を作成し、荷台のRigidBodyに追加。マージエリアの後ろに置く。
            emitterBox = new agxCollide.Box(0.1, 0.1, 0.1);
            agxCollide.Geometry geometry = new agxCollide.Geometry(emitterBox);
            geometry.setEnableCollisions(false);
            Simulation.Instance.Native.add(geometry);
            containerBody.Native.add(geometry);
            UpdateEmitterPositionAndSize();

            // BoxをEmitterに追加
            granularBodySystem.setEnableCollisions(geometry, false);
            emitter.setGeometry(geometry);

            return true;
        }

        /// <summary>
        /// 放土時に、生成した粒子またはドアにかける力を計算。荷台の土砂総量および荷台の昇降角度によって変わる。
        /// </summary>
        /// <returns></returns>
        double CalcPushForce()
        {
            double potentialMaxForce =
                9.81 * Mathf.Clamp((float)soilMass, pushForceMinSoilMass, pushForceMaxSoilMass) /
                maxNumParticlesInSpawnZone;
            return Math.Sin(tiltAngle * Mathf.Deg2Rad) * potentialMaxForce;
        }
        
        /// <summary>
        /// Unityが各Frameに一回呼び出すメソッド。
        /// ステップクリティカルな処理（マージ、スポーン、質量体更新）は
        /// OnPostStepForward()で物理ステップごとに直接実行されるため、
        /// ここではビジュアル更新のみ行う。
        /// </summary>
        void Update()
        {
            if (!isRuntimeReady)
                return;

            UpdateVisualMaterial(Time.deltaTime);
        }

        /// <summary>
        /// ステップクリティカルな処理（マージ、スポーン、質量体更新）を行う。
        /// 各シミュレーションステップごとに一度実行される。
        /// </summary>
        void ProcessCaptureStep()
        {
#if UNITY_ASSERTIONS
            captureStepExecutionCount++;
#endif
            UpdateCaptureCandidatesPerStep();
            UpdateSpawn();
            UpdateSoilMassBody();
        }

        /// <summary>
        /// 放土のため、ドアに力をかける。
        /// </summary>
        void UpdateDoorForce()
        {
            if (!doorPushForceEnabled || doorBody?.GetInitialized<RigidBody>() == null)
                return;

            if (soilSpeed == 0.0 || numUnmergedParticlesInMergeZone == 0 || soilMass == 0)
                return;

            Vector3 forcePos = transform.position;
            Vector3 forceVec = doorPushForceScale * (float)-CalcPushForce() * 
                Vector3.ProjectOnPlane(forwardDir, Vector3.up).normalized;

            doorBody.Native.addForceAtPosition(forceVec.ToHandedVec3(), forcePos.ToHandedVec3());
        }

        /// <summary>
        /// AgxUnityが各シミュレーションステップの後に呼び出すメソッド。このクラスのOnEnable()からコールバックとして登録される。
        /// 物理ステップ直後に捕捉処理を実行することで、粒子が薄い荷台壁面をすり抜ける前に回収する（issue #75）。
        /// また、Update()でのバッチ処理を避けることでデススパイラルを防止する（issue #79）。
        /// </summary>
        void OnPostStepForward()
        {
            ProcessCaptureStep();
            UpdateDoorForce();
        }

        /// <summary>
        /// このスクリプトがEnableになるときUnityが呼び出すメソッド。
        /// 初回有効化時はisRuntimeReadyがfalseのためコールバック登録をスキップし、
        /// Initialize()内で登録される。再有効化時（OnDisableで解除された後）は
        /// isRuntimeReadyがtrueなのでここで再登録する。
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            if (isRuntimeReady && Simulation.HasInstance)
                Simulation.Instance.StepCallbacks.PostStepForward += OnPostStepForward;
        }

        /// <summary>
        /// このスクリプトがDisableになるときUnityが呼び出すメソッド。
        /// </summary>
        protected override void OnDisable()
        {
            if (isRuntimeReady && Simulation.HasInstance)
                Simulation.Instance.StepCallbacks.PostStepForward -= OnPostStepForward;
            base.OnDisable();
        }

        /// <summary>
        /// Per-step capture pipeline: broad-phase candidate selection, exact overlap
        /// classification, decision-making (merge vs force), and batch removal from AGX.
        ///
        /// Three-pass design avoids mutating the AGX granular collection while iterating:
        ///   1. Broad-phase AABB check → narrows to vessel-local <see cref="captureCandidates"/>
        ///   2. Exact local bounds check on candidates → populates <see cref="particlesToMerge"/>
        ///      or applies forces; never removes particles from AGX.
        ///   3. Batch removal from AGX in descending-index order to keep indices stable.
        /// </summary>
        void UpdateCaptureCandidatesPerStep()
        {
            numUnmergedParticlesInMergeZone = 0;

            if (terrainNative == null)
                return;

            agx.AffineMatrix4x4 inverseShapeTransform = new agx.AffineMatrix4x4(
                transform.rotation.ToHandedQuat(),
                transform.position.ToHandedVec3()).inverse();

            // Broad-phase world-space AABB, expanded by particle radius.
            agx.Vec3 aabbMinAgx, aabbMaxAgx;
            AgxUtil.ToAgxMinMax(mergeZoneOriginalBoundsWorld, out aabbMinAgx, out aabbMaxAgx);
            var broadWorldBounds = CaptureUtil.CalculateWorldBroadPhaseBounds(
                new Vector3((float)aabbMinAgx.x, (float)aabbMinAgx.y, (float)aabbMinAgx.z),
                new Vector3((float)aabbMaxAgx.x, (float)aabbMaxAgx.y, (float)aabbMaxAgx.z),
                nominalParticleData.radius);
            agx.Vec3 aabbMin = new agx.Vec3(broadWorldBounds.min.x, broadWorldBounds.min.y, broadWorldBounds.min.z);
            agx.Vec3 aabbMax = new agx.Vec3(broadWorldBounds.max.x, broadWorldBounds.max.y, broadWorldBounds.max.z);

            // Local-space capture bounds (current soil height shapes Y).
            var captureBounds = CaptureUtil.CalculateLocalCaptureBounds(mergeZoneOriginalSize, soilHeight);

            bool canMerge = (soilSpeed <= 0.1 && tiltAngle < mininumDumpAngle) || !spawnParticlesEnabled;
            double maxPotentialSoilSpeedSqrd = maxPotentialSoilSpeed * maxPotentialSoilSpeed;
            agx.Vec3 pushForce = forwardDir.ToHandedVec3() * -CalcPushForce() * particlesPushForceScale;

            var soilSimulation = terrainNative.getSoilSimulationInterface();
            var granulars = soilSimulation.getSoilParticles();
            int count = (int)granulars.size();

            // ---- Pass 1: Broad-phase candidate selection ----
            captureCandidates.Clear();
            for (int i = 0; i < count; ++i)
            {
                var granule = granulars.at((uint)i);
                agx.Vec3 pos = granule.position();

                // Broad-phase: world-space AABB check (expanded by particle radius).
                if (pos.x > aabbMax.x || pos.x < aabbMin.x ||
                    pos.z > aabbMax.z || pos.z < aabbMin.z ||
                    pos.y > aabbMax.y || pos.y < aabbMin.y)
                {
                    granule.ReturnToPool();
                    continue;
                }

                captureCandidates.Add(i);
                // Proxy remains alive — will be processed in Pass 2.
            }

            // ---- Pass 2: Exact overlap + decide merge/force (no AGX removals) ----
            particlesToMerge.Clear();
            foreach (int idx in captureCandidates)
            {
                var granule = granulars.at((uint)idx);
                agx.Vec3 localPos = inverseShapeTransform.transformPoint(granule.position());
                double radius = granule.getRadius();
                Vector3 localPosVec = new Vector3((float)localPos.x, (float)localPos.y, (float)localPos.z);

                if (!CaptureUtil.IsSphereOverlappingBounds(localPosVec, radius, captureBounds))
                {
                    granule.ReturnToPool();
                    continue;
                }

                if (canMerge)
                {
                    // Defer AGX removal to Pass 3.
                    particlesToMerge.Add(idx);
                }
                else
                {
                    numUnmergedParticlesInMergeZone++;
                    if (particlesPushForceEnabled &&
                        granule.getVelocity().length2() <= maxPotentialSoilSpeedSqrd)
                    {
                        granule.setForce(granule.getForce() + pushForce);
                    }
                    // Particle stays in simulation — may be captured on a future step.
                    granule.ReturnToPool();
                }
            }

            // ---- Pass 3: Batch removal from AGX (descending order for index stability) ----
            if (particlesToMerge.Count > 0)
            {
                particlesToMerge.Sort((a, b) => b.CompareTo(a));
                foreach (int idx in particlesToMerge)
                {
                    var granule = granulars.at((uint)idx);
                    soilMass += granule.getMass();
                    soilSimulation.removeSoilParticle(granule);
                    granule.ReturnToPool();
                }
                particlesToMerge.Clear();
            }
        }
        
        /// <summary>
        /// 荷台の後ろの粒子Emitterの生成率、速度を更新。それに、生成した粒子量に合わせて荷台土砂の量を更新。
        /// </summary>
        void UpdateSpawn()
        {
            if (emitter == null || emitterBox == null)
                return;

            float timeSinceLastUpdate = (float)(Time.timeAsDouble - lastSpawnUpdateTime);
            lastSpawnUpdateTime = Time.timeAsDouble;

            // 前回のUpdateから生成された粒子の質量を荷台質量から引く
            double emittedQuantityPrev = emittedQuantity;
            emittedQuantity = emitter.getEmittedQuantity();
            double deltaEmittedQuantity = emittedQuantity - emittedQuantityPrev;
            soilMass -= deltaEmittedQuantity * spawnParticleData.mass;

            // 今から生成する粒子の質量などを覚える
            spawnParticleData = nominalParticleData;

            // 荷台土砂量がゼロ、または荷台角度が下限より小さい場合は粒子生成をとめる
            bool canSpawn = spawnParticlesEnabled && 
                            soilMass > 0.0 && 
                            tiltAngle >= mininumDumpAngle;
            if (canSpawn)
            {
                // 角度次第の加速度を計算
                float gravityAcc = 9.81f * Mathf.Sin(tiltAngle * Mathf.Deg2Rad);
                float frictionAcc = 9.81f * Mathf.Cos(tiltAngle * Mathf.Deg2Rad) * frictionCoefficient;

                // Spawn Zoneが粒子で詰まっている場合は、gravityを小さくし、frictionを大きくする
                float particlesInSpawnZoneRatio = numUnmergedParticlesInMergeZone / (float)maxNumParticlesInSpawnZone;
                if (particlesInSpawnZoneRatio > 1.0f)
                {
                    float effect = fullSpawnZoneMarginFactor > 0 ?
                        Mathf.Clamp01((particlesInSpawnZoneRatio - 1f) / fullSpawnZoneMarginFactor) : 1f;
                    frictionAcc *= fullSpawnZoneFrictionScale * (1f + effect); // effectが1.0になるとfrictionAccが完了にスケール
                    gravityAcc *= 1f - effect; // effectが1.0になるとgravityAccが0.0になる
                }

                // 加速度で速度を更新。 ネガティブにならないように確認
                soilSpeed += (gravityAcc - frictionAcc) * timeSinceLastUpdate;
                soilSpeed = Math.Max(soilSpeed, 0);

                // 角度によって最大速度に制限
                maxPotentialSoilSpeed = Mathf.Sin(tiltAngle * Mathf.Deg2Rad) * maximumSoilSpeed;
                soilSpeed = Math.Min(soilSpeed, maxPotentialSoilSpeed);
            }
            else
            {
                maxPotentialSoilSpeed = 0.0;
                soilSpeed = 0.0;
            }

            // 粒子Emitterの生成率、粒子初期速度、粒子数上限を調整
            double flowVolume = soilSpeed * spawnZoneVerticalArea;
            double flowParticles = spawnParticleData.volume != 0.0 ? flowVolume / spawnParticleData.volume : 0.0;
            agx.Vec3 initParticleVelocity = soilSpeed * emitterBox.getGeometry().getFrame().transformVectorToLocal(-forwardDir.ToHandedVec3());
            double numSpawnableParticles = spawnParticleData.mass != 0.0 ? soilMass / spawnParticleData.mass : 0.0;
            double maximimuEmittedQuantity = emitter.getEmittedQuantity() + numSpawnableParticles;

            // AGX Emitterが切り上げるようなため
            maximimuEmittedQuantity = Math.Max(0.0, Math.Floor(maximimuEmittedQuantity));

            emitter.setRate(flowParticles);
            emitter.setVelocity(initParticleVelocity);
            emitter.setMaximumEmittedQuantity(maximimuEmittedQuantity);

            // 粒子Emitterの高さを荷台に入っている土砂の量に合わせて更新
            UpdateEmitterPositionAndSize();
        }

        /// <summary>
        /// 荷台に入っている土砂の量に合わせて粒子Emitterの高さを調整。
        /// </summary>
        void UpdateEmitterPositionAndSize()
        {
            if (emitterBox == null)
                return;

            emitterBox.setHalfExtents(new agx.Vec3(
                0.5 * spawnZoneWidth,
                0.5 * spawnZoneHeight,
                0.5 * spawnParticleData.diameter));

            agx.AffineMatrix4x4 relativeToMergeZone = agx.AffineMatrix4x4.translate(
                0,  
                emitterBox.getHalfExtents().y,
                emitterBox.getHalfExtents().z);

            emitterBox.getGeometry().setLocalTransform(
                relativeToMergeZone * transformRelativeToContainerBody);
        }
        
        /// <summary>
        /// AGXUnityのTerrainのParticleMaterialが変更されたのか検知し、変更された場合は関係のある粒子データを合わせて更新する。
        /// </summary>
        /// <param name="updateInterval">ParticleMaterialをチェックする周期(秒)</param>
        System.Collections.IEnumerator UpdateParticleDataCoroutine(float updateInterval)
        {
            if (terrain?.GetInitialized<DeformableTerrain>()?.Native == null)
                yield break;

            double? previousDensity = null;
            while (true)
            {
                yield return new WaitForSeconds(updateInterval);

                double density = terrain.Native.getMaterial(
                    agxTerrain.Terrain.MaterialType.PARTICLE).getBulkMaterial().getDensity();

                if (previousDensity != null && previousDensity != density)
                {
                    nominalParticleData = ParticleData.CreateFromTerrainProperties(terrain);
                    Debug.Log($"{name} : Detected a change in Terrain particle material density parameter. " +
                              $"Updating internal particle data cache. {nominalParticleData}.");
                    
                }
                previousDensity = density;
            }
        }  

        #endregion

        #region Visuals

        // ビジュアル用のコンポーネント、Materialプロパティ
        MeshRenderer meshRenderer;
        MaterialPropertyBlock materialPropertyBlock;
        double soilVisualMovedDistance = 0.0;

        /// <summary>
        /// 土量、放土速度などに合わせて、土砂表面メッシュのレンダリングマテリアルのパラメータを更新する。
        /// </summary>
        /// <param name="deltaTime">前回に呼び出したときからかかったGame時間</param>
        void UpdateVisualMaterial(double deltaTime)
        {
            if (!isRuntimeReady)
                return;

            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();

            if (materialPropertyBlock == null)
                materialPropertyBlock = new MaterialPropertyBlock();

            soilVisualMovedDistance += soilSpeed * deltaTime * soilVisualSpeedScale;

            float visualSoilHeight = (float)soilHeight;

            bool zeroHeightWhenOneParticleOrLess = true;
            if (zeroHeightWhenOneParticleOrLess)
            {
                float oneParticleSoilHeight = (float)(nominalParticleData.mass / (nominalParticleData.density * mergeZoneHorizontalArea));
                float invLerp = Mathf.InverseLerp(oneParticleSoilHeight, 10.0f, visualSoilHeight);
                visualSoilHeight = Mathf.LerpUnclamped(0, 10.0f, invLerp);
            }

            materialPropertyBlock.SetFloat("_SoilSlideOffset", (float)soilVisualMovedDistance / transform.localScale.z);
            materialPropertyBlock.SetFloat("_SoilBaseHeight", visualSoilHeight / transform.localScale.y);
            materialPropertyBlock.SetFloat("_SoilHeightMapMaxHeight", Mathf.Lerp(0.0f, 1.0f, Mathf.Sqrt(visualSoilHeight * 2.0f)));
            materialPropertyBlock.SetFloat("_TiltAngle", tiltAngle);

            meshRenderer.SetPropertyBlock(materialPropertyBlock);
        }

        /// <summary>
        /// デバッギングするために、Merge Zone、Spawn ZoneをSceneウィンドウ内に表示する。
        /// </summary>
        void OnDrawGizmos()
        {
            Matrix4x4 prevMatrix = Gizmos.matrix;
            try
            {
                if (showMergeZone)
                {
                    // Boxの位置、回転、スケールを設定
                    Gizmos.matrix = transform.localToWorldMatrix;

                    // Play時の場合は、Boxの高さを現在の土砂高さに合わせて調整
                    Vector3 localScale = Application.isPlaying ?
                        new Vector3(1, Mathf.Max(0.001f, (float)soilHeight / transform.localScale.y), 1) :
                        Vector3.one;

                    Vector3 localPos = Application.isPlaying ?
                        mergeZoneCurrentLocalCenterUnscaled :
                        mergeZoneOriginalLocalCenterUnscaled;

                    // Boxの表面を表示
                    Gizmos.color = new Color(0.1f, 1.0f, 0.1f, 0.2f);
                    Gizmos.DrawCube(localPos, localScale);

                    // Boxのエッジを表示
                    Gizmos.color = Gizmos.color * 2.0f;
                    Gizmos.DrawWireCube(localPos, localScale);
                }

                if(showSpawnZone)
                {
                    if (Application.isPlaying && emitterBox != null)
                    {
                        // Boxの位置、回転、スケールを設定
                        Gizmos.matrix = Matrix4x4.TRS(emitterBox.getGeometry().getPosition().ToHandedVector3(),
                                                      emitterBox.getGeometry().getRotation().ToHandedQuaternion(),
                                                      Vector3.one);
                        
                        Vector3 size = emitterBox.getHalfExtents().ToVector3() * 2.0f;

                        // Boxの表面を表示
                        Gizmos.color = new Color(1.0f, 0.1f, 0.1f, 0.2f);
                        Gizmos.DrawCube(Vector3.zero, size);

                        // Boxのエッジを表示
                        Gizmos.color = Gizmos.color * 2.0f;
                        Gizmos.DrawWireCube(Vector3.zero, size);
                    }
                }
            }
            finally { Gizmos.matrix = prevMatrix; }
        }

#endregion
    }

    /// <summary>
    /// Terrain粒子のプロパティを保存するストラクチャー。
    /// </summary>
    struct ParticleData
    {
        public double radius { get; private set; }
        public double diameter { get; private set; }
        public double area { get; private set; }
        public double volume { get; private set; }
        public double mass { get; private set; }
        public double density { get; private set; }

        public ParticleData(double radius, double density)
        {
            this.radius = radius;
            this.density = density;
            diameter = 2.0 * radius;
            area = radius * radius * Math.PI;
            volume = Math.Pow(radius, 3.0) * Math.PI * 4.0 / 3.0;
            mass = density * volume;
        }

        public static ParticleData CreateFromTerrainProperties(DeformableTerrain terrain)
        {
            return new ParticleData(
                terrain.Native.getParticleNominalRadius(),
                terrain.Native.getMaterial(agxTerrain.Terrain.MaterialType.PARTICLE).getBulkMaterial().getDensity());
        }

        static public double CalcMass(double radius, double density)
        {
            return density * Math.Pow(radius, 3.0) * Math.PI * 4.0 / 3.0;
        }

        static public double CalcRadius(double mass, double density)
        {
            return Math.Pow(mass * 3.0 / (Math.PI * 4.0 * density), 1.0 / 3.0);
        }

        public override string ToString()
        {
            return $"radius = {radius: 0.####}, diameter = {diameter: 0.####}, area = {area: 0.####}, " +
                   $"volume = {volume: 0.####}, mass = {mass: 0.####}, density = {density : 0.####}";
        }
    };

#if UNITY_EDITOR
    [CustomEditor(typeof(DumpSoil))]
    class DumpSoilEditor : Editor
    {
        public override bool RequiresConstantRepaint()
        {
            return RequiresConstantRepaint((DumpSoil)target);
        }

        static public bool RequiresConstantRepaint(DumpSoil dump)
        {
            return dump.showOutputInInspector && (dump.soilMass > 0.0 || dump.soilSpeed > 0.0);
        }

        public override void OnInspectorGUI()
        {
            // 標準のGUIを表示
            base.OnInspectorGUI();

            var data = (DumpSoil)target;

            if (data.showOutputInInspector)
                OnSoilDataGUI(data);
        }

        static public void OnSoilDataGUI(DumpSoil data)
        {
            EditorGUILayout.LabelField("Soil mass:", $"{data.soilMass: 0.###} kg");
            EditorGUILayout.LabelField("Soil height:", $"{data.soilHeight: 0.###} m");
            EditorGUILayout.LabelField("Soil volume:", $"{data.soilVolume: 0.###} m3");
        }
    }
#endif
}
