using Unity.Entities;
using Unity.Mathematics;

namespace VisualProgramming.Capsules
{
    public struct MoveData : IComponentData
    {
        public float Speed;
        public float3 TargetPosition;
    }

    public struct StaminaData : IComponentData
    {
        public float Current;
        public float Max;
        public float DrainRate;
        public float RecoveryRate;
    }

    public struct ExhaustedTag : IComponentData, IEnableableComponent
    {
    }

    public struct HealthData : IComponentData
    {
        public float Current;
        public float Max;
    }

    public struct DeadTag : IComponentData, IEnableableComponent
    {
    }

    public struct TargetTag : IComponentData
    {
    }

    public struct TargetMovementData : IComponentData
    {
        public float Radius;
        public float Speed;
        public float3 Center;
    }

    public enum CapsuleType : byte
    {
        Fast = 0,
        Balanced = 1,
        Tank = 2
    }

    public struct CapsuleInWave
    {
        public CapsuleType EnemyType;
        public int Amount;
    }

    public struct Wave
    {
        public float WaitTimeBeforeWave;
        public BlobArray<CapsuleInWave> Enemies;
    }

    public struct WavesBlob
    {
        public BlobArray<Wave> Waves;
    }

    public struct WavesHolder : IComponentData
    {
        public BlobAssetReference<WavesBlob> WavesReference;
    }

    public struct PrefabHolder : IComponentData
    {
        public Entity FastCapsulePrefab;
        public Entity BalancedCapsulePrefab;
        public Entity TankCapsulePrefab;
    }

    [InternalBufferCapacity(8)]
    public struct WaypointElement : IBufferElementData
    {
        public float3 Position;
    }

    public struct WaypointPatrolData : IComponentData
    {
        public int CurrentWaypointIndex;
        public float Speed;
        public float ArriveDistance;
    }

    public struct RandomHolder : IComponentData
    {
        public Unity.Mathematics.Random rng;
    }

    public struct WaveSpawnerState : IComponentData
    {
        public int CurrentWaveIndex;
        public float WaveTimer;
        public int TotalSpawned;
        public bool AllWavesSpawned;
    }

    public struct LifetimeData : IComponentData
    {
        public float RemainingTime;
    }

    public struct SpawnedCapsuleTag : IComponentData
    {
    }
}
