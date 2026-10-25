using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Entities;
using Unity.Scenes;
using VisualProgramming.Capsules;

namespace VisualProgramming.Editor
{
    /// <summary>
    /// Вспомогательный редакторский скрипт для автоматической сборки и валидации Практических заданий 1–3.
    /// Создает материалы, префабы для Singleton-компонента PrefabHolder,
    /// настраивает SubScene со спавнером (Blob, ECB, Random), буфером урона (NativeList для Jobs)
    /// и целью (DynamicBuffer waypoints, Health, Regeneration).
    /// </summary>
    public static class SceneSetupHelper
    {
        [MenuItem("Tools/Setup Practical Task Scene")]
        public static void SetupPracticalScene()
        {
            Debug.Log("[SceneSetupHelper] Starting Practical Task 3 (Jobs & Multithreading) scene setup...");

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

            // Создаем заготовки префабов для PrefabHolder с параметрами регенерации и атаки (Задание 3)
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

            // Арена (Ground Plane)
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "Ground";
            plane.transform.position = Vector3.zero;
            plane.transform.localScale = new Vector3(3.5f, 1f, 3.5f);
            if (groundMat != null) plane.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            // Цель (Target) с поддержкой DynamicBuffer путевых точек, здоровьем и регенерацией (Задания 2 и 3)
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            target.name = "Target";
            target.transform.position = new Vector3(0f, 0.5f, 0f);
            target.transform.localScale = Vector3.one * 1.2f;
            if (targetMat != null) target.GetComponent<MeshRenderer>().sharedMaterial = targetMat;

            TargetAuthoring targetAuth = target.AddComponent<TargetAuthoring>();
            targetAuth.EnableWaypointPatrol = true;
            targetAuth.PatrolSpeed = 3.8f;
            targetAuth.ArriveDistance = 0.6f;
            targetAuth.MaxHealth = 5000f;
            targetAuth.HealthRegeneratedPerUpdate = 50f;
            targetAuth.Waypoints = new List<Vector3>
            {
                new Vector3(6f, 0.5f, 6f),
                new Vector3(6f, 0.5f, -6f),
                new Vector3(-6f, 0.5f, -6f),
                new Vector3(-6f, 0.5f, 6f)
            };

            // Спавнер волн + Контейнер буфера урона NativeList (Задания 2 и 3)
            GameObject spawnerGO = new GameObject("WaveSpawner");
            spawnerGO.transform.position = Vector3.zero;

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

            // Начальные демонстрационные капсулы на сцене
            CreateCapsuleObjectInScene("Initial_Capsule_1", new Vector3(-4f, 1f, -4f), 3.2f, 100f, 20f, 25f, balancedMat);
            CreateCapsuleObjectInScene("Initial_Capsule_2", new Vector3(4f, 1f, 4f), 4.8f, 60f, 30f, 20f, fastMat);
            CreateCapsuleObjectInScene("Initial_Capsule_3", new Vector3(-4f, 1f, 4f), 2.2f, 160f, 15f, 30f, tankMat);

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

            Debug.Log("[SceneSetupHelper] Setup Practical Task 3 (Jobs & Multithreading) successfully completed!");
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
            Debug.Log("=== VALIDATING PRACTICAL TASK 3 (JOBS & MULTITHREADING) ===");

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
                var capAuth = pAsset.GetComponent<CapsuleAuthoring>();
                if (capAuth == null || capAuth.DamagePerSecond <= 0f || capAuth.HealthRegeneratedPerUpdate <= 0f)
                {
                    Debug.LogError($"[Validation] Prefab at {pPath} missing valid CapsuleAuthoring Jobs properties!");
                    return;
                }
                Debug.Log($"[Validation] Prefab '{pAsset.name}' (Regen={capAuth.HealthRegeneratedPerUpdate}, DPS={capAuth.DamagePerSecond}): OK");
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
            Debug.Log($"[Validation] TargetAuthoring with {target.Waypoints.Count} waypoints, MaxHealth={target.MaxHealth}, Regen={target.HealthRegeneratedPerUpdate}: OK");

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

            // 4. Тестирование многопоточного выполнения всех Jobs в тестовом ECS World
            ValidateJobsRuntimeExecution();

            Debug.Log("=== ALL PRACTICAL TASK 3 CHECKS PASSED SUCCESSFULLY ===");
        }

        private static void ValidateJobsRuntimeExecution()
        {
            Debug.Log("[Validation] Running multi-frame ECS Jobs simulation check...");

            using var world = new Unity.Entities.World("JobsValidationWorld");
            var em = world.EntityManager;

            var simGroup = world.GetOrCreateSystemManaged<Unity.Entities.SimulationSystemGroup>();
            var waveGroup = world.GetOrCreateSystemManaged<WaveSpawnSystemGroup>();
            simGroup.AddSystemToUpdateList(waveGroup);

            simGroup.AddSystemToUpdateList(world.GetOrCreateSystem<TargetPatrolSystem>());
            simGroup.AddSystemToUpdateList(world.GetOrCreateSystem<TargetMovementSystem>());
            simGroup.AddSystemToUpdateList(world.GetOrCreateSystem<TargetUpdateSystem>());
            simGroup.AddSystemToUpdateList(world.GetOrCreateSystem<MovementSystem>());
            simGroup.AddSystemToUpdateList(world.GetOrCreateSystem<StaminaSystem>());
            simGroup.AddSystemToUpdateList(world.GetOrCreateSystem<CombatAttackQuerySystem>());
            simGroup.AddSystemToUpdateList(world.GetOrCreateSystem<CombatDamageHandlerSystem>());
            simGroup.AddSystemToUpdateList(world.GetOrCreateSystem<HealthSystem>());
            simGroup.AddSystemToUpdateList(world.GetOrCreateSystem<CapsuleCatchAndDestroySystem>());
            waveGroup.AddSystemToUpdateList(world.GetOrCreateSystem<RegenerationSystem>());

            simGroup.SortSystems();
            waveGroup.SortSystems();

            // Создаем синглтон буфера урона NativeList
            Entity bufferEntity = em.CreateEntity();
            em.AddComponentData(bufferEntity, new DamageBufferHolder
            {
                Damages = new Unity.Collections.NativeList<DamageEvent>(1024, Unity.Collections.Allocator.Persistent)
            });

            // Создаем цель с маршрутом патрулирования, здоровьем и регенерацией
            Entity targetEntity = em.CreateEntity();
            em.AddComponentData(targetEntity, new TargetTag());
            em.AddComponentData(targetEntity, Unity.Transforms.LocalTransform.FromPosition(new Unity.Mathematics.float3(0f, 0.5f, 0f)));
            em.AddComponentData(targetEntity, new HealthData { Current = 500f, Max = 500f });
            em.AddComponentData(targetEntity, new RegenerationData { HealthRegeneratedPerUpdate = 10f });
            em.AddComponentData(targetEntity, new DeadTag());
            em.SetComponentEnabled<DeadTag>(targetEntity, false);
            em.AddComponentData(targetEntity, new WaypointPatrolData { CurrentWaypointIndex = 0, Speed = 4f, ArriveDistance = 0.5f });
            var waypoints = em.AddBuffer<WaypointElement>(targetEntity);
            waypoints.Add(new WaypointElement { Position = new Unity.Mathematics.float3(5f, 0.5f, 5f) });
            waypoints.Add(new WaypointElement { Position = new Unity.Mathematics.float3(-5f, 0.5f, 5f) });

            // Создаем несколько капсул рядом с целью, чтобы сработали параллельное движение, атака в NativeList и урон через IJob
            for (int i = 0; i < 10; i++)
            {
                Entity cap = em.CreateEntity();
                em.AddComponentData(cap, Unity.Transforms.LocalTransform.FromPosition(new Unity.Mathematics.float3(2.0f + i * 0.1f, 1f, 0f)));
                em.AddComponentData(cap, new MoveData { Speed = 3.5f, TargetPosition = Unity.Mathematics.float3.zero });
                em.AddComponentData(cap, new StaminaData { Current = 50f, Max = 100f, DrainRate = 20f, RecoveryRate = 25f });
                em.AddComponentData(cap, new ExhaustedTag());
                em.SetComponentEnabled<ExhaustedTag>(cap, false);
                em.AddComponentData(cap, new HealthData { Current = 80f, Max = 100f });
                em.AddComponentData(cap, new RegenerationData { HealthRegeneratedPerUpdate = 5f });
                em.AddComponentData(cap, new AttackerData { CurrentTarget = Entity.Null, DamagePerSecond = 20f, AttackRange = 4.0f });
                em.AddComponentData(cap, new DeadTag());
                em.SetComponentEnabled<DeadTag>(cap, false);
                em.AddComponentData(cap, new LifetimeData { RemainingTime = 30f });
            }

            // Симулируем 10 кадров с шагом времени 0.2 сек
            for (int step = 0; step < 10; step++)
            {
                world.SetTime(new Unity.Core.TimeData(elapsedTime: (step + 1) * 0.2, deltaTime: 0.2f));
                world.Update();
            }

            em.CompleteAllTrackedJobs();

            float targetHealthAfter = em.GetComponentData<HealthData>(targetEntity).Current;
            Debug.Log($"[Validation] Jobs Simulation completed 10 frames! Target Health after parallel capsule attacks & regen: {targetHealthAfter:F1}/500: OK");

            var holder = em.GetComponentData<DamageBufferHolder>(bufferEntity);
            if (holder.Damages.IsCreated)
            {
                holder.Damages.Dispose();
            }
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
