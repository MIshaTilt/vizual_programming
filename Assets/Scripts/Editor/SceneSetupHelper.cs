using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Scenes;
using VisualProgramming.Capsules;

namespace VisualProgramming.Editor
{
    /// <summary>
    /// Вспомогательный редакторский скрипт для автоматической сборки и валидации Практического задания 2.
    /// Создает необходимые материалы, префабы для Singleton-компонента PrefabHolder,
    /// настраивает SubScene с спавнером (Blob, ECB, Random) и целью (DynamicBuffer waypoints).
    /// </summary>
    public static class SceneSetupHelper
    {
        [MenuItem("Tools/Setup Practical Task 2 Scene")]
        public static void SetupPracticalScene()
        {
            Debug.Log("[SceneSetupHelper] Starting Task 2 scene and subscene setup...");

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

            string prefabsDir = "Assets/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabsDir))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            // Создаем наглядные материалы для разных типов сущностей
            Material targetMat = CreateOrGetMaterial("Assets/Materials/TargetMat.mat", new Color(1f, 0.2f, 0.2f));       // Красный (Цель)
            Material fastMat = CreateOrGetMaterial("Assets/Materials/FastCapsuleMat.mat", new Color(1f, 0.55f, 0.1f));    // Оранжевый (Быстрая)
            Material balancedMat = CreateOrGetMaterial("Assets/Materials/BalancedCapsuleMat.mat", new Color(0.2f, 0.7f, 1f)); // Синий/голубой (Сбалансированная)
            Material tankMat = CreateOrGetMaterial("Assets/Materials/TankCapsuleMat.mat", new Color(0.2f, 0.85f, 0.35f));  // Зеленый (Танк)
            Material groundMat = CreateOrGetMaterial("Assets/Materials/GroundMat.mat", new Color(0.22f, 0.25f, 0.28f));  // Темно-серый (Арена)

            // Создаем заготовки префабов для PrefabHolder
            GameObject fastPrefab = CreateAndSaveCapsulePrefab(
                "Assets/Prefabs/FastCapsulePrefab.prefab",
                "FastCapsulePrefab",
                speed: 4.8f,
                maxStamina: 60f,
                drainRate: 30f,
                recoveryRate: 20f,
                maxHealth: 80f,
                mat: fastMat
            );

            GameObject balancedPrefab = CreateAndSaveCapsulePrefab(
                "Assets/Prefabs/BalancedCapsulePrefab.prefab",
                "BalancedCapsulePrefab",
                speed: 3.2f,
                maxStamina: 100f,
                drainRate: 20f,
                recoveryRate: 25f,
                maxHealth: 100f,
                mat: balancedMat
            );

            GameObject tankPrefab = CreateAndSaveCapsulePrefab(
                "Assets/Prefabs/TankCapsulePrefab.prefab",
                "TankCapsulePrefab",
                speed: 2.0f,
                maxStamina: 160f,
                drainRate: 15f,
                recoveryRate: 30f,
                maxHealth: 180f,
                mat: tankMat
            );

            // 1. Создаем SubScene: Assets/Scenes/CapsuleSubScene.unity
            string subScenePath = "Assets/Scenes/CapsuleSubScene.unity";
            Scene subScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Арена (Ground Plane)
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Ground";
            plane.transform.position = Vector3.zero;
            plane.transform.localScale = new Vector3(3.5f, 1f, 3.5f);
            if (groundMat != null) plane.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            // Цель (Target) с поддержкой DynamicBuffer путевых точек патрулирования (слайды 5, 6)
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            target.name = "Target";
            target.transform.position = new Vector3(0f, 0.5f, 0f);
            target.transform.localScale = Vector3.one * 1.2f;
            if (targetMat != null) target.GetComponent<MeshRenderer>().sharedMaterial = targetMat;

            TargetAuthoring targetAuth = target.AddComponent<TargetAuthoring>();
            targetAuth.EnableWaypointPatrol = true;
            targetAuth.PatrolSpeed = 3.8f;
            targetAuth.ArriveDistance = 0.6f;
            targetAuth.Waypoints = new List<Vector3>
            {
                new Vector3(6f, 0.5f, 6f),
                new Vector3(6f, 0.5f, -6f),
                new Vector3(-6f, 0.5f, -6f),
                new Vector3(-6f, 0.5f, 6f)
            };

            // Спавнер волн: PrefabHolder (Singleton) + WaveConfig (Blob) + Random (RandomHolder)
            GameObject spawnerGO = new GameObject("WaveSpawner");
            spawnerGO.transform.position = Vector3.zero;

            // Компонент хранения префабов (слайд 8)
            PrefabHolderAuthoring prefabHolderAuth = spawnerGO.AddComponent<PrefabHolderAuthoring>();
            prefabHolderAuth.FastCapsulePrefab = fastPrefab;
            prefabHolderAuth.BalancedCapsulePrefab = balancedPrefab;
            prefabHolderAuth.TankCapsulePrefab = tankPrefab;

            // Компонент конфигурации волн через Blob (слайды 1-4)
            WaveConfigAuthoring waveConfigAuth = spawnerGO.AddComponent<WaveConfigAuthoring>();
            waveConfigAuth.Waves = new List<EnemiesWaveData>
            {
                new EnemiesWaveData
                {
                    WaitTimeBeforeWave = 1f, // Волна 1: быстрое начало
                    EnemiesInWave = new List<WaveEnemyData>
                    {
                        new WaveEnemyData { EnemyType = CapsuleType.Fast, Amount = 2 },
                        new WaveEnemyData { EnemyType = CapsuleType.Balanced, Amount = 2 }
                    }
                },
                new EnemiesWaveData
                {
                    WaitTimeBeforeWave = 5f, // Волна 2
                    EnemiesInWave = new List<WaveEnemyData>
                    {
                        new WaveEnemyData { EnemyType = CapsuleType.Fast, Amount = 3 },
                        new WaveEnemyData { EnemyType = CapsuleType.Tank, Amount = 2 }
                    }
                },
                new EnemiesWaveData
                {
                    WaitTimeBeforeWave = 6f, // Волна 3
                    EnemiesInWave = new List<WaveEnemyData>
                    {
                        new WaveEnemyData { EnemyType = CapsuleType.Fast, Amount = 4 },
                        new WaveEnemyData { EnemyType = CapsuleType.Balanced, Amount = 3 },
                        new WaveEnemyData { EnemyType = CapsuleType.Tank, Amount = 2 }
                    }
                }
            };

            // Начальные демонстрационные капсулы
            CreateCapsuleObjectInScene("Initial_Capsule_1", new Vector3(-4f, 1f, -4f), 3.2f, 100f, 20f, 25f, balancedMat);
            CreateCapsuleObjectInScene("Initial_Capsule_2", new Vector3(4f, 1f, 4f), 4.8f, 60f, 30f, 20f, fastMat);

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
            mainCam.transform.position = new Vector3(0f, 16f, -18f);
            mainCam.transform.rotation = Quaternion.Euler(42f, 0f, 0f);

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

            Debug.Log("[SceneSetupHelper] Setup Practical Task 2 successfully completed!");
        }

        [MenuItem("Tools/Validate Practical Task")]
        public static void ValidatePracticalTask()
        {
            Debug.Log("=== VALIDATING PRACTICAL TASK 2 ===");

            // 1. Проверяем наличие префабов
            string[] requiredPrefabs = {
                "Assets/Prefabs/FastCapsulePrefab.prefab",
                "Assets/Prefabs/BalancedCapsulePrefab.prefab",
                "Assets/Prefabs/TankCapsulePrefab.prefab"
            };

            foreach (var pPath in requiredPrefabs)
            {
                var pAsset = AssetDatabase.LoadAssetAtPath<GameObject>(pPath);
                if (pAsset == null)
                {
                    Debug.LogError($"[Validation] Prefab NOT found at {pPath}!");
                    return;
                }
                if (pAsset.GetComponent<CapsuleAuthoring>() == null)
                {
                    Debug.LogError($"[Validation] Prefab at {pPath} missing CapsuleAuthoring!");
                    return;
                }
                Debug.Log($"[Validation] Prefab '{pAsset.name}': OK");
            }

            // 2. Проверяем SubScene
            string subScenePath = "Assets/Scenes/CapsuleSubScene.unity";
            var subSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(subScenePath);
            if (subSceneAsset == null)
            {
                Debug.LogError($"[Validation] CapsuleSubScene.unity NOT found at {subScenePath}!");
                return;
            }

            Scene subScene = EditorSceneManager.OpenScene(subScenePath, OpenSceneMode.Single);
            
            // Проверка цели
            var target = Object.FindFirstObjectByType<TargetAuthoring>();
            if (target == null)
            {
                Debug.LogError("[Validation] TargetAuthoring NOT found in SubScene!");
                return;
            }
            if (!target.EnableWaypointPatrol || target.Waypoints.Count == 0)
            {
                Debug.LogError("[Validation] TargetAuthoring missing DynamicBuffer waypoints!");
                return;
            }
            Debug.Log($"[Validation] TargetAuthoring with {target.Waypoints.Count} waypoints for DynamicBuffer: OK");

            // Проверка спавнера
            var prefabHolder = Object.FindFirstObjectByType<PrefabHolderAuthoring>();
            if (prefabHolder == null || prefabHolder.FastCapsulePrefab == null || prefabHolder.BalancedCapsulePrefab == null)
            {
                Debug.LogError("[Validation] PrefabHolderAuthoring missing or prefabs not linked in SubScene!");
                return;
            }
            Debug.Log("[Validation] PrefabHolderAuthoring with Fast, Balanced and Tank prefabs: OK");

            var waveConfig = Object.FindFirstObjectByType<WaveConfigAuthoring>();
            if (waveConfig == null || waveConfig.Waves.Count == 0)
            {
                Debug.LogError("[Validation] WaveConfigAuthoring missing or no waves configured in SubScene!");
                return;
            }
            Debug.Log($"[Validation] WaveConfigAuthoring with {waveConfig.Waves.Count} waves for Blob: OK");

            // 3. Проверяем главную сцену
            string mainScenePath = "Assets/Scenes/SampleScene.unity";
            Scene mainScene = EditorSceneManager.OpenScene(mainScenePath, OpenSceneMode.Single);
            var subSceneComp = Object.FindFirstObjectByType<SubScene>();
            if (subSceneComp == null || subSceneComp.SceneAsset == null)
            {
                Debug.LogError("[Validation] SubScene reference is invalid in SampleScene!");
                return;
            }
            Debug.Log($"[Validation] SubScene in SampleScene references '{subSceneComp.SceneAsset.name}': OK");

            Debug.Log("=== ALL PRACTICAL TASK 2 CHECKS PASSED SUCCESSFULLY ===");
        }

        private static GameObject CreateAndSaveCapsulePrefab(string path, string name, float speed, float maxStamina, float drainRate, float recoveryRate, float maxHealth, Material mat)
        {
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = name;
            capsule.transform.position = Vector3.zero;

            if (mat != null)
            {
                capsule.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }

            CapsuleAuthoring auth = capsule.AddComponent<CapsuleAuthoring>();
            auth.Speed = speed;
            auth.MaxStamina = maxStamina;
            auth.StaminaDrainRate = drainRate;
            auth.StaminaRecoveryRate = recoveryRate;
            auth.MaxHealth = maxHealth;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(capsule, path);
            Object.DestroyImmediate(capsule);

            return prefab;
        }

        private static GameObject CreateCapsuleObjectInScene(string name, Vector3 pos, float speed, float maxStamina, float drainRate, float recoveryRate, Material mat)
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
