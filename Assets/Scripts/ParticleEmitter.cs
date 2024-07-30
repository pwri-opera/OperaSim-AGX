using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGXUnity;
using AGXUnity.Collide;
using AGXUnity.Model;

namespace PWRISimulator
{
    /// <summary>
    /// 指示した形状の中に書くフレームに少しずつ粒子を作成させるスクリプト。参考：agx.ParticleEmitter
    /// </summary>
    public class ParticleEmitter : ScriptComponent
    {
        public enum QuantityType { Count = 0, Volume = 1, Mass = 2 };

        [Tooltip("The unit of the Rate and Max Quantity properties. Can be particle count, volume (m3), or mass (kg).")]
        public QuantityType quantityType = QuantityType.Count;
        
        [Tooltip("The rate at which to spawn particles. Depending on the Quantity Type property, the unit is either " +
                 "particle count, volume (m3), or mass (kg) per second.")]
        public double rate = 10;

        [Tooltip("Max total quantity that the emitter spawn is allowed to spawn. Depending on the Quantity Type " +
                 "property, the unit is either particle count, volume (m3), or mass (kg)")]
        public double maxQuantity = 1000;

        [Tooltip("Automatically set Max Quantity to the volume of the shape. Should only be used if Quantity Type " +
                 "is set to Volume.")]
        public bool maxQuantityFromShapeVolume = false;
        
        [Header("Overrides (auto-assigned on Play)")]

        [Tooltip("The shape to spawn particlesin. If None, auto-assigned to the Shape on this game object.")]
        public Shape emitterShape;
        public DeformableTerrain terrain;

        agx.ParticleEmitter emitter;

        protected override bool Initialize()
        {
            if (terrain == null)
                terrain = FindObjectOfType<DeformableTerrain>();

            if (emitterShape == null)
                emitterShape = GetComponent<Shape>();

            bool success = base.Initialize();
            success = CreateNativeEmitter() && success;
            return success;
        }

        bool CreateNativeEmitter()
        {
            if (terrain.GetInitialized<DeformableTerrain>() == null)
                return false;

            if (emitterShape.GetInitialized<Shape>() == null)
                return false;

            agxSDK.Simulation sim = Simulation.Instance?.Native;
            if (sim == null)
                return false;

            agxTerrain.SoilSimulationInterface soilSim = terrain.Native?.getSoilSimulationInterface();
            agx.GranularBodySystem granularSystem = soilSim?.getGranularBodySystem();

            if (granularSystem == null)
                return false;

            emitter = new agx.ParticleEmitter(granularSystem, (agx.Emitter.Quantity)quantityType);

            granularSystem.setEnableCollisions(emitterShape.NativeGeometry, false);
            emitter.setGeometry(emitterShape.NativeGeometry);

            var distTable = new agx.ParticleEmitter.DistributionTable((agx.Emitter.Quantity)quantityType);
            distTable.addModel(
                new agx.ParticleEmitter.DistributionModel(
                    terrain.Native.getParticleNominalRadius(),
                    terrain.Native.getMaterial(agxTerrain.Terrain.MaterialType.PARTICLE), 1));
            emitter.setDistributionTable(distTable);

            sim.add(emitter);

            UpdateRateAndMaxQuantity();

            return true;
        }

        protected override void OnEnable()
        {
            if (emitter != null)
                emitter.setEnable(true);
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            if (emitter != null)
                emitter.setEnable(false);
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            agxSDK.Simulation sim = Simulation.Instance?.Native;

            if (emitter != null && sim != null)
                sim.remove(emitter);

            base.OnDestroy();
        }

        void UpdateRateAndMaxQuantity()
        {
            if (emitter == null)
                return;

            if (maxQuantityFromShapeVolume)
            {
                if (quantityType == QuantityType.Volume)
                    maxQuantity = emitterShape.NativeShape.getVolume();
                else
                    Debug.LogError("Cannot set maximum quantity from shape volume because Quanity Type is not Volume");
            }

            emitter.setRate(rate);
            emitter.setMaximumEmittedQuantity(maxQuantity);
        }

        void Update()
        {
            UpdateRateAndMaxQuantity();
        }
    }
}
