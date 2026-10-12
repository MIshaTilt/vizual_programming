using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Система спавна волн капсул (слайд 10 методички).
    /// Находится в пользовательской группе WaveSpawnSystemGroup.
    /// Использует:
    /// 1. EntityCommandBuffer для инстанцирования сущностей (слайд 7).
    /// 2. Singleton-компонент PrefabHolder для доступа к префабам (слайд 8).
    /// 3. BlobAssetReference через WavesHolder для чтения структуры волн (слайды 4, 5).
    /// 4. Unity.Mathematics.Random для процедурного позиционирования и рандомизации характеристик (слайд 7).
    /// </summary>
    [UpdateInGroup(typeof(WaveSpawnSystemGroup))]
    [BurstCompile]
    public partial struct WaveSpawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WavesHolder>();
            state.RequireForUpdate<PrefabHolder>();
            state.RequireForUpdate<WaveSpawnerState>();
            state.RequireForUpdate<RandomHolder>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ref var spawnerState = ref SystemAPI.GetSingletonRW<WaveSpawnerState>().ValueRW;
            if (spawnerState.AllWavesSpawned)
            {
                return;
            }

            // Обращение к данным Blob через GetSingleton (слайд 5)
            ref var wavesBlob = ref SystemAPI.GetSingleton<WavesHolder>().WavesReference.Value;
            if (spawnerState.CurrentWaveIndex >= wavesBlob.Waves.Length)
            {
                spawnerState.AllWavesSpawned = true;
                return;
            }

            ref var currentWave = ref wavesBlob.Waves[spawnerState.CurrentWaveIndex];
            
            // Поскольку WaveSpawnSystemGroup тикает 1 раз в секунду, прибавляем шаг 1 сек
            spawnerState.WaveTimer += 1.0f;

            if (spawnerState.WaveTimer < currentWave.WaitTimeBeforeWave)
            {
                return;
            }

            // Время волны наступило — сбрасываем таймер
            spawnerState.WaveTimer = 0f;

            // Обращение к держателю префабов через GetSingleton (слайд 8)
            var prefabHolder = SystemAPI.GetSingleton<PrefabHolder>();

            // Получаем генератор случайных чисел RandomHolder (слайд 7)
            ref var randomHolder = ref SystemAPI.GetSingletonRW<RandomHolder>().ValueRW;

            // Создание EntityCommandBuffer с аллокатором Temp (слайд 7)
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            ref var enemiesArray = ref currentWave.Enemies;
            for (int i = 0; i < enemiesArray.Length; i++)
            {
                ref var enemyGroup = ref enemiesArray[i];
                Entity prefabToSpawn = Entity.Null;
                float baseSpeed = 3.0f;
                float baseStamina = 100f;

                switch (enemyGroup.EnemyType)
                {
                    case CapsuleType.Fast:
                        prefabToSpawn = prefabHolder.FastCapsulePrefab;
                        baseSpeed = 4.5f;
                        baseStamina = 60f;
                        break;

                    case CapsuleType.Balanced:
                        prefabToSpawn = prefabHolder.BalancedCapsulePrefab;
                        baseSpeed = 3.0f;
                        baseStamina = 100f;
                        break;

                    case CapsuleType.Tank:
                        prefabToSpawn = prefabHolder.TankCapsulePrefab;
                        baseSpeed = 2.0f;
                        baseStamina = 150f;
                        break;
                }

                // Запасной вариант, если конкретный префаб не был назначен
                if (prefabToSpawn == Entity.Null)
                {
                    if (prefabHolder.BalancedCapsulePrefab != Entity.Null)
                        prefabToSpawn = prefabHolder.BalancedCapsulePrefab;
                    else if (prefabHolder.FastCapsulePrefab != Entity.Null)
                        prefabToSpawn = prefabHolder.FastCapsulePrefab;
                    else if (prefabHolder.TankCapsulePrefab != Entity.Null)
                        prefabToSpawn = prefabHolder.TankCapsulePrefab;
                    else
                        continue;
                }

                for (int count = 0; count < enemyGroup.Amount; count++)
                {
                    // Запись в EntityCommandBuffer команды создания Entity (слайд 7)
                    Entity spawned = ecb.Instantiate(prefabToSpawn);

                    // Использование Unity.Mathematics.Random для случайного угла и радиуса спавна (п. 3)
                    float angle = randomHolder.rng.NextFloat(0f, math.PI * 2f);
                    float distance = randomHolder.rng.NextFloat(9f, 13f);
                    float3 spawnPosition = new float3(
                        math.cos(angle) * distance,
                        1f,
                        math.sin(angle) * distance
                    );

                    ecb.SetComponent(spawned, LocalTransform.FromPositionRotation(spawnPosition, quaternion.identity));

                    // Случайный разброс характеристик (скорость и выносливость) через Random (п. 3)
                    float speedMultiplier = randomHolder.rng.NextFloat(0.85f, 1.25f);
                    ecb.SetComponent(spawned, new MoveData
                    {
                        Speed = baseSpeed * speedMultiplier,
                        TargetPosition = float3.zero
                    });

                    float staminaMultiplier = randomHolder.rng.NextFloat(0.9f, 1.2f);
                    ecb.SetComponent(spawned, new StaminaData
                    {
                        Current = baseStamina * staminaMultiplier,
                        Max = baseStamina * staminaMultiplier,
                        DrainRate = 22f,
                        RecoveryRate = 18f
                    });

                    // Задаем время жизни сущности (35 секунд)
                    ecb.AddComponent(spawned, new LifetimeData
                    {
                        RemainingTime = 35f
                    });

                    // Помечаем тегом рантайм-капсулы
                    ecb.AddComponent(spawned, new SpawnedCapsuleTag());

                    spawnerState.TotalSpawned++;
                }
            }

            // Воспроизведение всех накопленных команд и освобождение памяти (слайд 7)
            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            // Переход к следующей волне
            spawnerState.CurrentWaveIndex++;
            if (spawnerState.CurrentWaveIndex >= wavesBlob.Waves.Length)
            {
                spawnerState.AllWavesSpawned = true;
            }
        }
    }
}
