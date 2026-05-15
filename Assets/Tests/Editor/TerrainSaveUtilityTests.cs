using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace PWRISimulator.Tests
{
    public class TerrainSaveUtilityTests
    {
        private Type utilityType;

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            utilityType = Type.GetType("PWRISimulator.TerrainSaveUtility, Assembly-CSharp");
        }

        [Test]
        public void CreateSerializableHeights_SubtractsRuntimeDepthOffset()
        {
            Assert.That(utilityType, Is.Not.Null);

            var method = utilityType.GetMethod(
                "CreateSerializableHeights",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);

            var source = new float[,]
            {
                { 0.80f, 0.90f },
                { 0.30f, 0.20f }
            };

            var result = (float[,])method.Invoke(null, new object[] { source, 20.0f, true, 6.0f });

            Assert.That(result[0, 0], Is.EqualTo(0.50f).Within(1e-6f));
            Assert.That(result[0, 1], Is.EqualTo(0.60f).Within(1e-6f));
            Assert.That(result[1, 0], Is.EqualTo(0.00f).Within(1e-6f));
            Assert.That(result[1, 1], Is.EqualTo(0.00f).Within(1e-6f));
            Assert.That(source[0, 0], Is.EqualTo(0.80f).Within(1e-6f));
        }

        [Test]
        public void CreateSerializableHeights_LeavesEditorHeightsUnchanged()
        {
            Assert.That(utilityType, Is.Not.Null);

            var method = utilityType.GetMethod(
                "CreateSerializableHeights",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);

            var source = new float[,]
            {
                { 0.80f, 0.90f },
                { 0.30f, 0.20f }
            };

            var result = (float[,])method.Invoke(null, new object[] { source, 20.0f, false, 6.0f });

            Assert.That(result[0, 0], Is.EqualTo(0.80f).Within(1e-6f));
            Assert.That(result[0, 1], Is.EqualTo(0.90f).Within(1e-6f));
            Assert.That(result[1, 0], Is.EqualTo(0.30f).Within(1e-6f));
            Assert.That(result[1, 1], Is.EqualTo(0.20f).Within(1e-6f));
        }

        [Test]
        public void CreateNativeHeightValues_AddsRuntimeDepthOffsetInAgxOrder()
        {
            Assert.That(utilityType, Is.Not.Null);

            var method = utilityType.GetMethod(
                "CreateNativeHeightValues",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);

            var saved = new float[,]
            {
                { 0.10f, 0.20f },
                { 0.30f, 0.40f }
            };

            var result = (float[])method.Invoke(null, new object[] { saved, 20.0f, 6.0f });

            Assert.That(result, Is.EqualTo(new[] { 14.0f, 12.0f, 10.0f, 8.0f }).Within(1e-6f));
        }

        [Test]
        public void CreateVisibleHeightValues_ConvertsSavedNormalizedHeightsToMeters()
        {
            Assert.That(utilityType, Is.Not.Null);

            var method = utilityType.GetMethod(
                "CreateVisibleHeightValues",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);

            var saved = new float[,]
            {
                { 0.10f, 0.20f },
                { 0.30f, 0.40f }
            };

            var result = (float[,])method.Invoke(null, new object[] { saved, 20.0f });

            Assert.That(result[0, 0], Is.EqualTo(2.0f).Within(1e-6f));
            Assert.That(result[0, 1], Is.EqualTo(4.0f).Within(1e-6f));
            Assert.That(result[1, 0], Is.EqualTo(6.0f).Within(1e-6f));
            Assert.That(result[1, 1], Is.EqualTo(8.0f).Within(1e-6f));
        }

        [Test]
        public void CreateRuntimeTerrainDataHeights_AddsRuntimeDepthOffsetForInitializedTerrain()
        {
            Assert.That(utilityType, Is.Not.Null);

            var method = utilityType.GetMethod(
                "CreateRuntimeTerrainDataHeights",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);

            var saved = new float[,]
            {
                { 0.10f, 0.20f },
                { 0.80f, 0.90f }
            };

            var result = (float[,])method.Invoke(null, new object[] { saved, 20.0f, true, 6.0f });

            Assert.That(result[0, 0], Is.EqualTo(0.40f).Within(1e-6f));
            Assert.That(result[0, 1], Is.EqualTo(0.50f).Within(1e-6f));
            Assert.That(result[1, 0], Is.EqualTo(1.00f).Within(1e-6f));
            Assert.That(result[1, 1], Is.EqualTo(1.00f).Within(1e-6f));
        }

        [Test]
        public void CreateRuntimeTerrainDataHeights_LeavesUninitializedTerrainUnchanged()
        {
            Assert.That(utilityType, Is.Not.Null);

            var method = utilityType.GetMethod(
                "CreateRuntimeTerrainDataHeights",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);

            var saved = new float[,]
            {
                { 0.10f, 0.20f },
                { 0.80f, 0.90f }
            };

            var result = (float[,])method.Invoke(null, new object[] { saved, 20.0f, false, 6.0f });

            Assert.That(result[0, 0], Is.EqualTo(0.10f).Within(1e-6f));
            Assert.That(result[0, 1], Is.EqualTo(0.20f).Within(1e-6f));
            Assert.That(result[1, 0], Is.EqualTo(0.80f).Within(1e-6f));
            Assert.That(result[1, 1], Is.EqualTo(0.90f).Within(1e-6f));
        }

        [Test]
        public void ApplySerializedHeights_UpdatesUnityAndNativeTerrainAtRuntime()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Scripts", "TerrainSaveUtility.cs");
            var source = File.ReadAllText(path);

            Assert.That(source, Does.Contain("terrainData.SetHeights(0, 0, runtimeHeights)"));
            Assert.That(source, Does.Contain("deformableTerrain.Native.setHeights"));
            Assert.That(source, Does.Not.Contain("Skipping runtime TerrainData.SetHeights"));
        }

        [Test]
        public void GetMachineRootPose_UsesRootTransformInsteadOfChildBodyTransform()
        {
            Assert.That(utilityType, Is.Not.Null);

            var positionMethod = utilityType.GetMethod(
                "GetMachineRootPosition",
                BindingFlags.Public | BindingFlags.Static);
            var rotationMethod = utilityType.GetMethod(
                "GetMachineRootRotation",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(positionMethod, Is.Not.Null);
            Assert.That(rotationMethod, Is.Not.Null);

            var root = new GameObject("ic120_0");
            root.transform.SetPositionAndRotation(
                new Vector3(1.0f, 2.0f, 3.0f),
                Quaternion.Euler(0.0f, 45.0f, 0.0f));
            var body = new GameObject("body_link");
            body.transform.SetParent(root.transform, false);
            body.transform.SetPositionAndRotation(
                new Vector3(10.0f, 11.0f, 12.0f),
                Quaternion.Euler(0.0f, 90.0f, 0.0f));

            var position = (Vector3)positionMethod.Invoke(null, new object[] { root });
            var rotation = (Quaternion)rotationMethod.Invoke(null, new object[] { root });

            Assert.That(position, Is.EqualTo(root.transform.position));
            Assert.That(rotation, Is.EqualTo(root.transform.rotation));
        }

        [Test]
        public void IsSavedDumpTruckRootName_AcceptsSavedDumpInstancesOnly()
        {
            Assert.That(utilityType, Is.Not.Null);

            var method = utilityType.GetMethod(
                "IsSavedDumpTruckRootName",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);

            Assert.That((bool)method.Invoke(null, new object[] { "ic120_0" }), Is.True);
            Assert.That((bool)method.Invoke(null, new object[] { "ic120_12" }), Is.True);
            Assert.That((bool)method.Invoke(null, new object[] { "ic120_prefVar" }), Is.True);
            Assert.That((bool)method.Invoke(null, new object[] { "ic120" }), Is.False);
            Assert.That((bool)method.Invoke(null, new object[] { "ic120_track" }), Is.False);
            Assert.That((bool)method.Invoke(null, new object[] { "foo_ic120_0" }), Is.False);
        }
    }
}
