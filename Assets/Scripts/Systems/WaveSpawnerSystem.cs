using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Система спавна волн капсул (слайд 10 методички 2, слайд 1 методички 3).
    /// Находится в пользовательской группе WaveSpawnSystemGroup.
    /// Согласно слайду 1 методички 3, система спавна намеренно остается последовательной (однопоточной),
    /// так как она работает по единому таймеру и создает сущности раз в N секунд.
    /// </summary>
    [UpdateInGroup(typeof(WaveSpawnSystemGroup))]
    [UpdateBefore(typeof(RegenerationSystem))]
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

            // Обращение к данным Blob через GetSingleton (слайд 5 методички 2)
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

            // Обращение к держателю префабов через GetSingleton (слайд 8 методички 2)
            var prefabHolder = SystemAPI.GetSingleton<PrefabHolder>();

            // Получаем генератор случайных чисел RandomHolder (слайд 7 методички 2)
            ref var randomHolder = ref SystemAPI.GetSingletonRW<RandomHolder>().ValueRW;

            // Создание EntityCommandBuffer с аллокатором Temp (слайд 7 методички 2)
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
                    Entity spawned = ecb.Instantiate(prefabToSpawn);

                    float angle = randomHolder.rng.NextFloat(0f, math.PI * 2f);
                    float distance = randomHolder.rng.NextFloat(9f, 13f);
                    float3 spawnPosition = new float3(
                        math.cos(angle) * distance,
                        1f,
                        math.sin(angle) * distance
                    );

                    ecb.SetComponent(spawned, LocalTransform.FromPositionRotation(spawnPosition, quaternion.identity));

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

                    ecb.AddComponent(spawned, new LifetimeData
                    {
                        RemainingTime = 35f
                    });

                    ecb.AddComponent(spawned, new SpawnedCapsuleTag());

                    spawnerState.TotalSpawned++;
                }
            }

            // Завершаем активные джобы перед структурными изменениями EntityManager
            state.Dependency.Complete();

            // Воспроизведение всех накопленных команд и освобождение памяти
            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            spawnerState.CurrentWaveIndex++;
            if (spawnerState.CurrentWaveIndex >= wavesBlob.Waves.Length)
            {
                spawnerState.AllWavesSpawned = true;
            }
        }
    }
}
