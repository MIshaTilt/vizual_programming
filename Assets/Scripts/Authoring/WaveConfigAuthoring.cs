using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Entities;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Данные о врагах в волне для сериализации в инспекторе Unity (слайд 1).
    /// </summary>
    [Serializable]
    public class WaveEnemyData
    {
        public CapsuleType EnemyType;
        public int Amount = 1;
    }

    /// <summary>
    /// Конфигурация отдельной волны противников (слайд 1).
    /// </summary>
    [Serializable]
    public class EnemiesWaveData
    {
        public float WaitTimeBeforeWave = 3f;
        public List<WaveEnemyData> EnemiesInWave = new List<WaveEnemyData>();
    }

    /// <summary>
    /// Авторинг-компонент конфигурации волн.
    /// Запекает данные волн в неизменяемый BlobAssetReference (слайды 1-4)
    /// и инициализирует генератор случайных чисел RandomHolder (слайд 7).
    /// </summary>
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

                // Создаем BlobBuilder (слайд 2)
                BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp);

                // Создаем корневой объект root типа WavesBlob (слайд 2)
                ref var root = ref blobBuilder.ConstructRoot<WavesBlob>();

                int waveCount = authoring.Waves != null ? authoring.Waves.Count : 0;
                // Аллоцируем массив Waves внутри root (слайд 3)
                BlobBuilderArray<Wave> wavesBuilderArray = blobBuilder.Allocate(ref root.Waves, waveCount);

                for (int i = 0; i < waveCount; i++)
                {
                    var waveConfig = authoring.Waves[i];
                    wavesBuilderArray[i].WaitTimeBeforeWave = waveConfig.WaitTimeBeforeWave;

                    int enemiesCount = waveConfig.EnemiesInWave != null ? waveConfig.EnemiesInWave.Count : 0;
                    // Аллоцируем вложенный массив Enemies внутри каждого элемента волны (слайд 3)
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

                // Создаем постоянную ссылку на Blob (слайды 3, 4)
                BlobAssetReference<WavesBlob> wavesBlobAssetReference = blobBuilder.CreateBlobAssetReference<WavesBlob>(Allocator.Persistent);
                blobBuilder.Dispose();

                // Регистрируем BlobAsset для автоматического управления памятью (слайд 4)
                AddBlobAsset(ref wavesBlobAssetReference, out _);

                // Добавляем синглтон-компонент WavesHolder со ссылкой на Blob (слайд 4)
                AddComponent(entity, new WavesHolder
                {
                    WavesReference = wavesBlobAssetReference
                });

                // Инициализируем состояние спавнера в рантайме
                AddComponent(entity, new WaveSpawnerState
                {
                    CurrentWaveIndex = 0,
                    WaveTimer = 0f,
                    TotalSpawned = 0,
                    AllWavesSpawned = false
                });

                // Добавляем компонент генератора случайных чисел (слайд 7)
                // Генерируем случайный сид при запекании через UnityEngine.Random
                uint randomSeed = (uint)UnityEngine.Random.Range(1, 1000000);
                AddComponent(entity, new RandomHolder
                {
                    rng = new Unity.Mathematics.Random(randomSeed)
                });
            }
        }
    }
}
