using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Scenes;
using VisualProgramming.Capsules;

namespace VisualProgramming.Editor
{
    public static class SceneSetupHelper
    {
        [MenuItem("Tools/Setup Practical Task Scene")]
        public static void SetupPracticalScene()
        {
            Debug.Log("[SceneSetupHelper] Starting scene and subscene setup...");

            string scenesDir = "Assets/Scenes";
            if (!AssetDatabase.IsValidFolder(scenesDir))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            string materialsDir = "Assets/Materials";
            if (!AssetDatabase.IsValidFolder(materialsDir))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            // Создаем материалы для наглядности
            Material targetMat = CreateOrGetMaterial("Assets/Materials/TargetMat.mat", new Color(1f, 0.25f, 0.2f)); // красный/оранжевый
            Material capsuleMat = CreateOrGetMaterial("Assets/Materials/CapsuleMat.mat", new Color(0.2f, 0.7f, 1f)); // голубой
            Material groundMat = CreateOrGetMaterial("Assets/Materials/GroundMat.mat", new Color(0.25f, 0.28f, 0.32f)); // темно-серый

            // 1. Создаем SubScene: Assets/Scenes/CapsuleSubScene.unity
            string subScenePath = "Assets/Scenes/CapsuleSubScene.unity";
            Scene subScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Земля (Ground Plane)
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Ground";
            plane.transform.position = Vector3.zero;
            plane.transform.localScale = new Vector3(3f, 1f, 3f);
            if (groundMat != null) plane.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            // Цель (Target)
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            target.name = "Target";
            target.transform.position = new Vector3(0f, 0.5f, 0f);
            target.transform.localScale = Vector3.one;
            if (targetMat != null) target.GetComponent<MeshRenderer>().sharedMaterial = targetMat;
            TargetAuthoring targetAuth = target.AddComponent<TargetAuthoring>();
            targetAuth.EnableCircularMovement = true;
            targetAuth.Radius = 6f;
            targetAuth.Speed = 1.2f;

            // Несколько капсул с разными характеристиками стамины и скорости
            CreateCapsule("Capsule_Fast", new Vector3(-4f, 1f, -4f), 4.5f, 60f, 30f, 20f, capsuleMat);
            CreateCapsule("Capsule_Balanced", new Vector3(4f, 1f, -4f), 3.0f, 100f, 20f, 25f, capsuleMat);
            CreateCapsule("Capsule_Tank", new Vector3(0f, 1f, -6f), 2.0f, 150f, 15f, 30f, capsuleMat);
            CreateCapsule("Capsule_Sprinter", new Vector3(-6f, 1f, 2f), 5.0f, 50f, 35f, 15f, capsuleMat);

            EditorSceneManager.SaveScene(subScene, subScenePath);
            Debug.Log($"[SceneSetupHelper] SubScene saved to {subScenePath}");

            // 2. Открываем и настраиваем главную сцену: Assets/Scenes/SampleScene.unity
            string mainScenePath = "Assets/Scenes/SampleScene.unity";
            Scene mainScene = EditorSceneManager.OpenScene(mainScenePath, OpenSceneMode.Single);

            // Настройка камеры
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }
            mainCam.transform.position = new Vector3(0f, 12f, -15f);
            mainCam.transform.rotation = Quaternion.Euler(38f, 0f, 0f);

            // Настройка света
            Light dirLight = Object.FindFirstObjectByType<Light>();
            if (dirLight == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                dirLight = lightObj.AddComponent<Light>();
                dirLight.type = LightType.Directional;
            }
            dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Настройка SubScene GameObject в главной сцене
            SubScene existingSubSceneComponent = Object.FindFirstObjectByType<SubScene>();
            GameObject subSceneGO;
            if (existingSubSceneComponent != null)
            {
                subSceneGO = existingSubSceneComponent.gameObject;
            }
            else
            {
                subSceneGO = new GameObject("SubScene");
                existingSubSceneComponent = subSceneGO.AddComponent<SubScene>();
            }

            SceneAsset subSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(subScenePath);
            if (subSceneAsset != null)
            {
                existingSubSceneComponent.SceneAsset = subSceneAsset;
                existingSubSceneComponent.AutoLoadScene = true;
                EditorUtility.SetDirty(existingSubSceneComponent);
            }
            else
            {
                Debug.LogError($"[SceneSetupHelper] Failed to load SceneAsset at {subScenePath}");
            }

            EditorSceneManager.SaveScene(mainScene, mainScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[SceneSetupHelper] Setup successfully completed!");
        }

        [MenuItem("Tools/Validate Practical Task")]
        public static void ValidatePracticalTask()
        {
            Debug.Log("=== VALIDATING PRACTICAL TASK ===");

            string subScenePath = "Assets/Scenes/CapsuleSubScene.unity";
            var subSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(subScenePath);
            if (subSceneAsset == null)
            {
                Debug.LogError($"[Validation] CapsuleSubScene.unity NOT found at {subScenePath}!");
                return;
            }
            Debug.Log("[Validation] CapsuleSubScene.unity exists: OK");

            Scene subScene = EditorSceneManager.OpenScene(subScenePath, OpenSceneMode.Single);
            var targets = Object.FindObjectsByType<TargetAuthoring>(FindObjectsSortMode.None);
            var capsules = Object.FindObjectsByType<CapsuleAuthoring>(FindObjectsSortMode.None);
            Debug.Log($"[Validation] Found {targets.Length} Targets and {capsules.Length} Capsules in SubScene.");

            foreach (var cap in capsules)
            {
                Debug.Log($"[Validation] -> Capsule '{cap.name}': Speed={cap.Speed}, MaxStamina={cap.MaxStamina}, DrainRate={cap.StaminaDrainRate}, RecoveryRate={cap.StaminaRecoveryRate}");
            }

            string mainScenePath = "Assets/Scenes/SampleScene.unity";
            Scene mainScene = EditorSceneManager.OpenScene(mainScenePath, OpenSceneMode.Single);
            var subSceneComp = Object.FindFirstObjectByType<SubScene>();
            if (subSceneComp == null)
            {
                Debug.LogError("[Validation] SubScene component NOT found in SampleScene!");
                return;
            }
            if (subSceneComp.SceneAsset == null)
            {
                Debug.LogError("[Validation] SubScene.SceneAsset is null in SampleScene!");
                return;
            }
            Debug.Log($"[Validation] SubScene in SampleScene correctly references '{subSceneComp.SceneAsset.name}': OK");

            Debug.Log("=== ALL PRACTICAL TASK CHECKS PASSED SUCCESSFULLY ===");
        }

        private static GameObject CreateCapsule(string name, Vector3 pos, float speed, float maxStamina, float drainRate, float recoveryRate, Material mat)
        {
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = name;
            capsule.transform.position = pos;
            capsule.transform.localScale = Vector3.one;

            if (mat != null)
            {
                capsule.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }

            CapsuleAuthoring auth = capsule.AddComponent<CapsuleAuthoring>();
            auth.Speed = speed;
            auth.MaxStamina = maxStamina;
            auth.StaminaDrainRate = drainRate;
            auth.StaminaRecoveryRate = recoveryRate;
            auth.MaxHealth = 100f;

            return capsule;
        }

        private static Material CreateOrGetMaterial(string path, Color color)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                mat = new Material(shader);
                mat.color = color;
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", color);
                }

                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }
    }
}
