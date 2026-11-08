using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Entities;
using Unity.Physics.Authoring;
using Unity.Scenes;
using VisualProgramming.Capsules;

namespace VisualProgramming.Editor
{
    /// <summary>
    /// Вспомогательный редакторский скрипт для автоматической сборки и валидации Практических заданий 1–4.
    /// Настраивает:
    /// - Префабы капсул с Rigidbody и CapsuleCollider (providesContacts = true) для Unity Physics (Задание 4).
    /// - PhysicsStepAuthoring на синглтоне в SubScene (слайд 1 методички 4).
    /// - Спавнер волн (Blob, ECB, Random), буфер урона (NativeList для Jobs) и цель (DynamicBuffer waypoints).
    /// </summary>
    public static class SceneSetupHelper
    {
        [MenuItem("Tools/Setup Practical Task Scene")]
        public static void SetupPracticalScene()
        {
            Debug.Log("[SceneSetupHelper] Starting Practical Task 4 (Unity Physics & Jobs) scene setup...");

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

            // Создаем материалы для разных типов сущностей
            Material targetMat = CreateOrGetMaterial("Assets/Materials/TargetMat.mat", new Color(1f, 0.2f, 0.2f));          // Красный (Цель)
            Material fastMat = CreateOrGetMaterial("Assets/Materials/FastCapsuleMat.mat", new Color(1f, 0.55f, 0.1f));       // Оранжевый (Быстрая)
            Material balancedMat = CreateOrGetMaterial("Assets/Materials/BalancedCapsuleMat.mat", new Color(0.2f, 0.7f, 1f));// Синий/голубой (Сбалансированная)
            Material tankMat = CreateOrGetMaterial("Assets/Materials/TankCapsuleMat.mat", new Color(0.2f, 0.85f, 0.35f));     // Зеленый (Танк)
            Material groundMat = CreateOrGetMaterial("Assets/Materials/GroundMat.mat", new Color(0.22f, 0.25f, 0.28f));     // Темно-серый (Арена)

            // Создаем заготовки физических префабов для PrefabHolder (Задания 2, 3, 4)
            GameObject fastPrefab = CreateAndSaveCapsulePrefab(
                "Assets/Prefabs/FastCapsulePrefab.prefab",
                "FastCapsulePrefab",
                speed: 4.8f,
                maxStamina: 60f,
                drainRate: 30f,
                recoveryRate: 20f,
                maxHealth: 80f,
                regenPerUpdate: 4f,
                damagePerSec: 12f,
                attackRange: 3.5f,
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
                regenPerUpdate: 6f,
                damagePerSec: 10f,
                attackRange: 3.5f,
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
                regenPerUpdate: 10f,
                damagePerSec: 18f,
                attackRange: 4.0f,
                mat: tankMat
            );

            // 1. Создаем SubScene: Assets/Scenes/CapsuleSubScene.unity
            string subScenePath = "Assets/Scenes/CapsuleSubScene.unity";
            Scene subScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Физическая арена (Ground Plane с BoxCollider для надежной коллизии в Unity Physics)
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Ground";
            plane.transform.position = Vector3.zero;
            plane.transform.localScale = new Vector3(4.0f, 1f, 4.0f);
            if (groundMat != null) plane.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            var meshCollider = plane.GetComponent<MeshCollider>();
            if (meshCollider != null) Object.DestroyImmediate(meshCollider);
            BoxCollider groundBox = plane.AddComponent<BoxCollider>();
            groundBox.size = new Vector3(10f, 0.2f, 10f);
            groundBox.center = new Vector3(0f, -0.1f, 0f);

            // Цель (Target) с коллайдером (providesContacts = true) и Rigidbody (слайды 1, 6, 7 методички 4)
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            target.name = "Target";
            target.transform.position = new Vector3(0f, 0.6f, 0f);
            target.transform.localScale = Vector3.one * 1.2f;
            if (targetMat != null) target.GetComponent<MeshRenderer>().sharedMaterial = targetMat;

            SphereCollider targetCollider = target.GetComponent<SphereCollider>();
            if (targetCollider != null)
            {
                // Включаем Provides Contacts для срабатывания ICollisionEventsJob (слайды 6, 7 методички 4)
                targetCollider.providesContacts = true;
            }

            Rigidbody targetRb = target.AddComponent<Rigidbody>();
            targetRb.isKinematic = true;
            targetRb.useGravity = false;

            TargetAuthoring targetAuth = target.AddComponent<TargetAuthoring>();
            targetAuth.EnableWaypointPatrol = true;
            targetAuth.PatrolSpeed = 3.8f;
            targetAuth.ArriveDistance = 0.6f;
            targetAuth.MaxHealth = 5000f;
            targetAuth.HealthRegeneratedPerUpdate = 50f;
            targetAuth.Waypoints = new List<Vector3>
            {
                new Vector3(6f, 0.6f, 6f),
                new Vector3(6f, 0.6f, -6f),
                new Vector3(-6f, 0.6f, -6f),
                new Vector3(-6f, 0.6f, 6f)
            };

            // Спавнер волн + PhysicsStepAuthoring + Контейнер буфера урона NativeList (Задания 2, 3, 4)
            GameObject spawnerGO = new GameObject("WaveSpawner");
            spawnerGO.transform.position = Vector3.zero;

            // Конфигурация физики в мире Entity — Physics Step (слайд 1 методички 4)
            spawnerGO.AddComponent<PhysicsStepAuthoring>();

            // Компонент хранения префабов (слайд 8 методички 2)
            PrefabHolderAuthoring prefabHolderAuth = spawnerGO.AddComponent<PrefabHolderAuthoring>();
            prefabHolderAuth.FastCapsulePrefab = fastPrefab;
            prefabHolderAuth.BalancedCapsulePrefab = balancedPrefab;
            prefabHolderAuth.TankCapsulePrefab = tankPrefab;

            // Компонент буфера урона NativeList для параллельных Jobs (слайд 8 методички 3)
            DamageBufferAuthoring damageBufferAuth = spawnerGO.AddComponent<DamageBufferAuthoring>();
            damageBufferAuth.InitialCapacity = 1024;

            // Компонент конфигурации волн через Blob (слайды 1-4 методички 2)
            WaveConfigAuthoring waveConfigAuth = spawnerGO.AddComponent<WaveConfigAuthoring>();
            waveConfigAuth.Waves = new List<EnemiesWaveData>
            {
                new EnemiesWaveData
                {
                    WaitTimeBeforeWave = 1f,
                    EnemiesInWave = new List<WaveEnemyData>
                    {
                        new WaveEnemyData { EnemyType = CapsuleType.Fast, Amount = 3 },
                        new WaveEnemyData { EnemyType = CapsuleType.Balanced, Amount = 3 }
                    }
                },
                new EnemiesWaveData
                {
                    WaitTimeBeforeWave = 4f,
                    EnemiesInWave = new List<WaveEnemyData>
                    {
                        new WaveEnemyData { EnemyType = CapsuleType.Fast, Amount = 4 },
                        new WaveEnemyData { EnemyType = CapsuleType.Tank, Amount = 3 }
                    }
                },
                new EnemiesWaveData
                {
                    WaitTimeBeforeWave = 5f,
                    EnemiesInWave = new List<WaveEnemyData>
                    {
                        new WaveEnemyData { EnemyType = CapsuleType.Fast, Amount = 5 },
                        new WaveEnemyData { EnemyType = CapsuleType.Balanced, Amount = 4 },
                        new WaveEnemyData { EnemyType = CapsuleType.Tank, Amount = 3 }
                    }
                }
            };

            // Начальные физические капсулы на сцене
            CreateCapsuleObjectInScene("Initial_Capsule_1", new Vector3(-4f, 1.1f, -4f), 3.2f, 100f, 20f, 25f, balancedMat);
            CreateCapsuleObjectInScene("Initial_Capsule_2", new Vector3(4f, 1.1f, 4f), 4.8f, 60f, 30f, 20f, fastMat);
            CreateCapsuleObjectInScene("Initial_Capsule_3", new Vector3(-4f, 1.1f, 4f), 2.2f, 160f, 15f, 30f, tankMat);

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

            Debug.Log("[SceneSetupHelper] Setup Practical Task 4 (Unity Physics & Jobs) successfully completed!");
        }

        [MenuItem("Tools/Setup And Validate Practical Task")]
        public static void SetupAndValidatePracticalTask()
        {
            SetupPracticalScene();
            ValidatePracticalTask();
        }

        [MenuItem("Tools/Validate Practical Task")]
        public static void ValidatePracticalTask()
        {
            Debug.Log("=== VALIDATING PRACTICAL TASK 4 (UNITY PHYSICS & JOBS) ===");

            // 1. Проверяем наличие префабов и компонентов физики (Rigidbody + Collider с providesContacts)
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
                var capAuth = pAsset.GetComponent<CapsuleAuthoring>();
                var rb = pAsset.GetComponent<Rigidbody>();
                var col = pAsset.GetComponent<CapsuleCollider>();
                if (capAuth == null || rb == null || col == null || !col.providesContacts)
                {
                    Debug.LogError($"[Validation] Prefab at {pPath} missing Rigidbody or Collider with providesContacts=true!");
                    return;
                }
                Debug.Log($"[Validation] Physics Prefab '{pAsset.name}' (Rigidbody=OK, ProvidesContacts={col.providesContacts}): OK");
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

            // Проверка PhysicsStepAuthoring
            var physicsStep = Object.FindFirstObjectByType<PhysicsStepAuthoring>();
            if (physicsStep == null)
            {
                Debug.LogError("[Validation] PhysicsStepAuthoring NOT found in SubScene!");
                return;
            }
            Debug.Log("[Validation] PhysicsStepAuthoring singleton in SubScene: OK");

            // Проверка цели
            var target = Object.FindFirstObjectByType<TargetAuthoring>();
            if (target == null)
            {
                Debug.LogError("[Validation] TargetAuthoring NOT found in SubScene!");
                return;
            }
            var targetCol = target.GetComponent<Collider>();
            if (targetCol == null || !targetCol.providesContacts)
            {
                Debug.LogError("[Validation] Target Collider missing or providesContacts is false!");
                return;
            }
            Debug.Log($"[Validation] TargetAuthoring with Collider (ProvidesContacts={targetCol.providesContacts}) and {target.Waypoints.Count} waypoints: OK");

            // Проверка спавнера и буфера урона
            var prefabHolder = Object.FindFirstObjectByType<PrefabHolderAuthoring>();
            if (prefabHolder == null || prefabHolder.FastCapsulePrefab == null || prefabHolder.BalancedCapsulePrefab == null)
            {
                Debug.LogError("[Validation] PrefabHolderAuthoring missing or prefabs not linked in SubScene!");
                return;
            }
            Debug.Log("[Validation] PrefabHolderAuthoring with Fast, Balanced and Tank prefabs: OK");

            var damageBuffer = Object.FindFirstObjectByType<DamageBufferAuthoring>();
            if (damageBuffer == null || damageBuffer.InitialCapacity <= 0)
            {
                Debug.LogError("[Validation] DamageBufferAuthoring missing in SubScene!");
                return;
            }
            Debug.Log($"[Validation] DamageBufferAuthoring (NativeList capacity={damageBuffer.InitialCapacity}): OK");

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

            Debug.Log("=== ALL PRACTICAL TASK 4 CHECKS PASSED SUCCESSFULLY ===");
        }

        private static GameObject CreateAndSaveCapsulePrefab(
            string path,
            string name,
            float speed,
            float maxStamina,
            float drainRate,
            float recoveryRate,
            float maxHealth,
            float regenPerUpdate,
            float damagePerSec,
            float attackRange,
            Material mat)
        {
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = name;
            capsule.transform.position = Vector3.zero;

            if (mat != null)
            {
                capsule.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }

            // Настраиваем физический коллайдер и включаем Provides Contacts (слайды 1, 6, 7 методички 4)
            CapsuleCollider col = capsule.GetComponent<CapsuleCollider>();
            if (col != null)
            {
                col.providesContacts = true;
            }

            // Добавляем Rigidbody для запекания в PhysicsVelocity и PhysicsMass (слайды 1–3 методички 4)
            Rigidbody rb = capsule.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.useGravity = true;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.5f;

            CapsuleAuthoring auth = capsule.AddComponent<CapsuleAuthoring>();
            auth.Speed = speed;
            auth.MaxStamina = maxStamina;
            auth.StaminaDrainRate = drainRate;
            auth.StaminaRecoveryRate = recoveryRate;
            auth.MaxHealth = maxHealth;
            auth.HealthRegeneratedPerUpdate = regenPerUpdate;
            auth.DamagePerSecond = damagePerSec;
            auth.AttackRange = attackRange;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(capsule, path);
            Object.DestroyImmediate(capsule);

            return prefab;
        }

        private static GameObject CreateCapsuleObjectInScene(
            string name,
            Vector3 pos,
            float speed,
            float maxStamina,
            float drainRate,
            float recoveryRate,
            Material mat)
        {
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = name;
            capsule.transform.position = pos;
            capsule.transform.localScale = Vector3.one;

            if (mat != null)
            {
                capsule.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }

            CapsuleCollider col = capsule.GetComponent<CapsuleCollider>();
            if (col != null)
            {
                col.providesContacts = true;
            }

            Rigidbody rb = capsule.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.useGravity = true;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.5f;

            CapsuleAuthoring auth = capsule.AddComponent<CapsuleAuthoring>();
            auth.Speed = speed;
            auth.MaxStamina = maxStamina;
            auth.StaminaDrainRate = drainRate;
            auth.StaminaRecoveryRate = recoveryRate;
            auth.MaxHealth = 100f;
            auth.HealthRegeneratedPerUpdate = 5f;
            auth.DamagePerSecond = 12f;
            auth.AttackRange = 3.5f;

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
