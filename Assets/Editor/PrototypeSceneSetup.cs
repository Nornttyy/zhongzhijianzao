using System.IO;
using DoNotOpen.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DoNotOpen.EditorTools
{
    public static class PrototypeSceneSetup
    {
        private const string SceneDirectory = "Assets/Scenes";
        private const string ScenePath = SceneDirectory + "/Prototype.unity";

        [MenuItem("Zhong Zhi Jian Zao/Create Prototype Scene")]
        public static void CreatePrototypeScene()
        {
            Directory.CreateDirectory(SceneDirectory);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Prototype Bootstrap").AddComponent<PrototypeBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"Prototype scene created at {ScenePath}");
        }
    }
}
