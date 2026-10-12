using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace VisualProgramming.Capsules
{
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

            ref var wavesBlob = ref SystemAPI.GetSingleton<WavesHolder>().WavesReference.Value;
            if (spawnerState.CurrentWaveIndex >= wavesBlob.Waves.Length)
            {
                spawnerState.AllWavesSpawned = true;
                return;
            }

            ref var currentWave = ref wavesBlob.Waves[spawnerState.CurrentWaveIndex];
            
            spawnerState.WaveTimer += 1.0f;

            if (spawnerState.WaveTimer < currentWave.WaitTimeBeforeWave)
            {
                return;
            }

            spawnerState.WaveTimer = 0f;

            var prefabHolder = SystemAPI.GetSingleton<PrefabHolder>();

            ref var randomHolder = ref SystemAPI.GetSingletonRW<RandomHolder>().ValueRW;

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
