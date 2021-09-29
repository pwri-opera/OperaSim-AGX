using System;
using UnityEngine;
using AGXUnity;
using AGXUnity.Collide;
using AGXUnity.Model;
using AGXUnity.Utils;
using Math = System.Math;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
        public double soilMass { get; private set; } = 0.0;

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

        // MergeとSpawnの更新が必要かどうか。
        bool needsUpdate = true;

        // Spawnが最新に更新されたGameTimeの時刻。
        double lastSpawnUpdateTime = 0.0;
        
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

            return base.Initialize();
        }
        
        bool CreateSoilMassBody()
        {
            if (!addSoilMassRigidBody)
                return true;

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
        /// </summary>
        void Update()
        {
            if (needsUpdate)
            {
                needsUpdate = false;

                UpdateMerge();
                UpdateSpawn();
                UpdateSoilMassBody();
            }
            UpdateVisualMaterial(Time.deltaTime);
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
        /// </summary>
        void OnPostStepForward()
        {
            needsUpdate = true;

            UpdateDoorForce();
        }

        /// <summary>
        /// このスクリプトがEnableになるときUnityが呼び出すメソッド。
        /// </summary>
        protected override void OnEnable()
        {
            if (Simulation.HasInstance)
                Simulation.Instance.StepCallbacks.PostStepForward += OnPostStepForward;
            base.OnEnable();
        }

        /// <summary>
        /// このスクリプトがDisableになるときUnityが呼び出すメソッド。
        /// </summary>
        protected override void OnDisable()
        {
            if (Simulation.HasInstance)
                Simulation.Instance.StepCallbacks.PostStepForward -= OnPostStepForward;
            base.OnDisable();
        }

        /// <summary>
        /// MergeZoneに入っている粒子を検知して、適当な行動を行う：
        /// * 放土を行っていない場合は、粒子を消して質量を荷台土量に追加する。つまり、荷台土砂とマージする。
        /// * 放土を行っている場合は、マージしなくて、荷台の後ろ方向に粒子に力をかける。
        /// </summary>
        void UpdateMerge()
        {
            agx.AffineMatrix4x4 inverseShapeTransform = new agx.AffineMatrix4x4(
                transform.rotation.ToHandedQuat(),
                transform.position.ToHandedVec3()).inverse();

            // ワールドバウンディングボックスを計算（現在の土砂高さを無視する）
            agx.Vec3 aabbMin, aabbMax;
            AgxUtil.ToAgxMinMax(mergeZoneOriginalBoundsWorld, out aabbMin, out aabbMax);
            aabbMin -= new agx.Vec3(nominalParticleData.radius);
            aabbMax += new agx.Vec3(nominalParticleData.radius);

            // ローカルバウンディングボックスを計算（現在の土砂高さでY軸のサイズを設定）
            agx.Vec3 localAABBMin = new agx.Vec3(-mergeZoneCurrentSize.x * 0.5, 0, 0);
            agx.Vec3 localAABBMax = new agx.Vec3(mergeZoneCurrentSize.x * 0.5, soilHeight, mergeZoneCurrentSize.z);

            bool canMerge = (soilSpeed == 0.0 && tiltAngle < mininumDumpAngle) || !spawnParticlesEnabled; // 放土していないときだけにマージさせる
            double maxPotentialSoilSpeedSqrd = maxPotentialSoilSpeed * maxPotentialSoilSpeed; // 放土の土砂の最大速度
            agx.Vec3 pushForce = forwardDir.ToHandedVec3() * -CalcPushForce() * particlesPushForceScale;
            numUnmergedParticlesInMergeZone = 0; // Merge Zoneに入っているけどマージしない粒子の数

            // 全ての粒子を取得
            var soilSimulation = terrainNative.getSoilSimulationInterface();
            var granulars = soilSimulation.getSoilParticles();
            int granularsCount = (int)granulars.size();

            // 各粒子を反復
            for (int i = 0; i < granularsCount; ++i)
            {
                var granule = granulars.at((uint)i);

                // Check 1: Check if center is inside axis-aligned world space bounding box (expanded by particle radius)
                agx.Vec3 pos = granule.position();
                if (pos.x > aabbMax.x || pos.x < aabbMin.x ||
                    pos.z > aabbMax.z || pos.z < aabbMin.z ||
                    pos.y > aabbMax.y || pos.y < aabbMin.y)
                {
                    granule.ReturnToPool();
                    continue;
                }

                // Check 2: Convert pos to local shape coordinates, and check if inside local axis-aligned bounding box
                agx.Vec3 localPos = inverseShapeTransform.transformPoint(pos);
                double radius = granule.getRadius();
                if (localPos.x - radius > localAABBMax.x || localPos.x + radius < localAABBMin.x ||
                    localPos.z - radius > localAABBMax.z || localPos.z + radius < localAABBMin.z ||
                    localPos.y - radius > localAABBMax.y || localPos.y + radius < localAABBMin.y)
                {
                    granule.ReturnToPool();
                    continue;
                }

                if (canMerge) // 放土していない時
                {
                    // 粒子を荷台土砂にマージ、つまり粒子を消し荷台土砂量を更新
                    soilMass += granule.getMass();
                    soilSimulation.removeSoilParticle(granule);
                }
                else  // 放土時
                {
                    // そらに、詰まりを検知するために粒子を数える。
                    numUnmergedParticlesInMergeZone += 1;

                    // 放土時にMergeZoneに入ってしまう粒子に後ろ方向に力をかける。
                    if (particlesPushForceEnabled &&
                        granule.getVelocity().length2() <= maxPotentialSoilSpeedSqrd)
                    {
                            granule.setForce(granule.getForce() + pushForce);
                    }
                }

                // Return the proxy class to the pool to avoid garbage.
                granule.ReturnToPool();
            }
        }
        
        /// <summary>
        /// 荷台の後ろの粒子Emitterの生成率、速度を更新。それに、生成した粒子量に合わせて荷台土砂の量を更新。
        /// </summary>
        void UpdateSpawn()
        {
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
