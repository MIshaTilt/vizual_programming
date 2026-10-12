using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Entities;

namespace VisualProgramming.Capsules
{
    [Serializable]
    public class WaveEnemyData
    {
        public CapsuleType EnemyType;
        public int Amount = 1;
    }

    [Serializable]
    public class EnemiesWaveData
    {
        public float WaitTimeBeforeWave = 3f;
        public List<WaveEnemyData> EnemiesInWave = new List<WaveEnemyData>();
    }

    [DisallowMultipleComponent]
    public class WaveConfigAuthoring : MonoBehaviour
    {
        [Header("Spawn Waves Configuration (Blob Asset)")]
        [SerializeField] public List<EnemiesWaveData> Waves = new List<EnemiesWaveData>();

        public class WaveConfigBaker : Baker<WaveConfigAuthoring>
        {
            public override void Bake(WaveConfigAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);

                BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp);

                ref var root = ref blobBuilder.ConstructRoot<WavesBlob>();

                int waveCount = authoring.Waves != null ? authoring.Waves.Count : 0;
                BlobBuilderArray<Wave> wavesBuilderArray = blobBuilder.Allocate(ref root.Waves, waveCount);

                for (int i = 0; i < waveCount; i++)
                {
                    var waveConfig = authoring.Waves[i];
                    wavesBuilderArray[i].WaitTimeBeforeWave = waveConfig.WaitTimeBeforeWave;

                    int enemiesCount = waveConfig.EnemiesInWave != null ? waveConfig.EnemiesInWave.Count : 0;
                    BlobBuilderArray<CapsuleInWave> enemyBuilderArray = blobBuilder.Allocate(ref wavesBuilderArray[i].Enemies, enemiesCount);

                    for (int j = 0; j < enemiesCount; j++)
                    {
                        enemyBuilderArray[j] = new CapsuleInWave
                        {
                            EnemyType = waveConfig.EnemiesInWave[j].EnemyType,
                            Amount = waveConfig.EnemiesInWave[j].Amount
                        };
                    }
                }

                BlobAssetReference<WavesBlob> wavesBlobAssetReference = blobBuilder.CreateBlobAssetReference<WavesBlob>(Allocator.Persistent);
                blobBuilder.Dispose();

                AddBlobAsset(ref wavesBlobAssetReference, out _);

                AddComponent(entity, new WavesHolder
                {
                    WavesReference = wavesBlobAssetReference
                });

                AddComponent(entity, new WaveSpawnerState
                {
                    CurrentWaveIndex = 0,
                    WaveTimer = 0f,
                    TotalSpawned = 0,
                    AllWavesSpawned = false
                });

                uint randomSeed = (uint)UnityEngine.Random.Range(1, 1000000);
                AddComponent(entity, new RandomHolder
                {
                    rng = new Unity.Mathematics.Random(randomSeed)
                });
            }
        }
    }
}
