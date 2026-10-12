using Unity.Entities;
using Unity.Mathematics;

namespace VisualProgramming.Capsules
{
    // ==========================================
    // БАЗОВЫЕ КОМПОНЕНТЫ КАПСУЛ И ЦЕЛИ (Задание 1)
    // ==========================================

    /// <summary>
    /// Данные для движения сущности к целевой позиции.
    /// </summary>
    public struct MoveData : IComponentData
    {
        public float Speed;
        public float3 TargetPosition;
    }

    /// <summary>
    /// Характеристика выносливости сущности (расходуется при беге, восстанавливается при отдыхе).
    /// </summary>
    public struct StaminaData : IComponentData
    {
        public float Current;
        public float Max;
        public float DrainRate;
        public float RecoveryRate;
    }

    /// <summary>
    /// Выключаемый компонент (тег истощения). Когда активен - сущность не может двигаться.
    /// </summary>
    public struct ExhaustedTag : IComponentData, IEnableableComponent
    {
    }

    /// <summary>
    /// Характеристика здоровья сущности.
    /// </summary>
    public struct HealthData : IComponentData
    {
        public float Current;
        public float Max;
    }

    /// <summary>
    /// Выключаемый компонент (тег смерти).
    /// </summary>
    public struct DeadTag : IComponentData, IEnableableComponent
    {
    }

    /// <summary>
    /// Тег для сущности-цели, к которой движутся капсулы.
    /// </summary>
    public struct TargetTag : IComponentData
    {
    }

    /// <summary>
    /// Параметры кругового движения для цели.
    /// </summary>
    public struct TargetMovementData : IComponentData
    {
        public float Radius;
        public float Speed;
        public float3 Center;
    }

    // ==========================================
    // НОВЫЕ КОМПОНЕНТЫ: BLOB, DYNAMICBUFFER, PREFAB, RANDOM (Задание 2)
    // ==========================================

    /// <summary>
    /// Тип противника/капсулы (слайд 1-2).
    /// </summary>
    public enum CapsuleType : byte
    {
        Fast = 0,
        Balanced = 1,
        Tank = 2
    }

    /// <summary>
    /// Описание группы противников в волне для Blob (слайд 2).
    /// </summary>
    public struct CapsuleInWave
    {
        public CapsuleType EnemyType;
        public int Amount;
    }

    /// <summary>
    /// Структура волны в Blob со статическим вложенным массивом противников (слайд 2-3).
    /// </summary>
    public struct Wave
    {
        public float WaitTimeBeforeWave;
        public BlobArray<CapsuleInWave> Enemies;
    }

    /// <summary>
    /// Корневая структура Blob-ассета со списком всех волн (слайд 2-4).
    /// </summary>
    public struct WavesBlob
    {
        public BlobArray<Wave> Waves;
    }

    /// <summary>
    /// Компонент-хранилище ссылки на BlobAsset (слайд 4).
    /// </summary>
    public struct WavesHolder : IComponentData
    {
        public BlobAssetReference<WavesBlob> WavesReference;
    }

    /// <summary>
    /// Синглтон-компонент для хранения заготовленных префабов (слайд 8).
    /// </summary>
    public struct PrefabHolder : IComponentData
    {
        public Entity FastCapsulePrefab;
        public Entity BalancedCapsulePrefab;
        public Entity TankCapsulePrefab;
    }

    /// <summary>
    /// Элемент DynamicBuffer для хранения путевых точек патрулирования (слайды 5, 6).
    /// Вместимость 8 точек без дополнительных аллокаций.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct WaypointElement : IBufferElementData
    {
        public float3 Position;
    }

    /// <summary>
    /// Данные патрулирования по точкам из DynamicBuffer.
    /// </summary>
    public struct WaypointPatrolData : IComponentData
    {
        public int CurrentWaypointIndex;
        public float Speed;
        public float ArriveDistance;
    }

    /// <summary>
    /// Компонент для хранения генератора случайных чисел Unity.Mathematics.Random (слайд 7).
    /// </summary>
    public struct RandomHolder : IComponentData
    {
        public Unity.Mathematics.Random rng;
    }

    /// <summary>
    /// Состояние спавнера волн в рантайме.
    /// </summary>
    public struct WaveSpawnerState : IComponentData
    {
        public int CurrentWaveIndex;
        public float WaveTimer;
        public int TotalSpawned;
        public bool AllWavesSpawned;
    }

    /// <summary>
    /// Время жизни заспавненной капсулы (для демонстрации удаления через EntityCommandBuffer).
    /// </summary>
    public struct LifetimeData : IComponentData
    {
        public float RemainingTime;
    }

    /// <summary>
    /// Тег для заспавненных в рантайме капсул.
    /// </summary>
    public struct SpawnedCapsuleTag : IComponentData
    {
    }
}
