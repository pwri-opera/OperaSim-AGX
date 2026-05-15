using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace PWRISimulator.Tests
{
    public class Zx200ObjectUtilityTests
    {
        private readonly List<GameObject> createdObjects = new List<GameObject>();
        private Type helperType;

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            helperType = Type.GetType("PWRISimulator.Zx200ObjectUtility, Assembly-CSharp");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var createdObject in createdObjects)
            {
                if (createdObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObject);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void IsZx200Name_AcceptsSupportedNamesOnly()
        {
            Assert.That(helperType, Is.Not.Null);

            var method = helperType.GetMethod("IsZx200Name", BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);

            Assert.That((bool)method.Invoke(null, new object[] { "zx200_prefVar" }), Is.True);
            Assert.That((bool)method.Invoke(null, new object[] { "zx200" }), Is.True);
            Assert.That((bool)method.Invoke(null, new object[] { "zx200_3" }), Is.True);
            Assert.That((bool)method.Invoke(null, new object[] { "foo_zx200_3" }), Is.False);
            Assert.That((bool)method.Invoke(null, new object[] { "zx200_prefVar_extra" }), Is.False);
        }

        [Test]
        public void FindZx200Object_FindsPrefVarObject()
        {
            Assert.That(helperType, Is.Not.Null);

            var createdObject = new GameObject("zx200_prefVar");
            createdObjects.Add(createdObject);

            var method = helperType.GetMethod("FindZx200Object", BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);

            var result = method.Invoke(null, null) as GameObject;

            Assert.That(result, Is.SameAs(createdObject));
        }

        [Test]
        public void FindZx200Object_FindsIdVariantObject()
        {
            Assert.That(helperType, Is.Not.Null);

            var createdObject = new GameObject("zx200_7");
            createdObjects.Add(createdObject);

            var method = helperType.GetMethod("FindZx200Object", BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);

            var result = method.Invoke(null, null) as GameObject;

            Assert.That(result, Is.SameAs(createdObject));
        }
    }
}
