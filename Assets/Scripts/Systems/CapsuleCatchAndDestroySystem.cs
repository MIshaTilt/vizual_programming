using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Параллельная система удаления Entity через EntityCommandBuffer.ParallelWriter
    /// (задание 2 + оптимизация под Jobs из задания 3, слайды 5–6).
    /// </summary>
    [UpdateAfter(typeof(HealthSystem))]
    [BurstCompile]
    public partial struct CapsuleCatchAndDestroySystem : ISystem
    {
        private EntityQuery _deadQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TargetTag>();

            _deadQuery = SystemAPI.QueryBuilder()
                .WithAll<HealthData, DeadTag>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<TargetTag>(out Entity targetEntity))
            {
                return;
            }

            ComponentLookup<LocalTransform> transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            if (!transformLookup.TryGetComponent(targetEntity, out LocalTransform targetTransform))
            {
                return;
            }

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            EntityCommandBuffer.ParallelWriter parallelEcb = ecb.AsParallelWriter();

            // 1. Параллельная джоба проверки поимки цели и истечения времени жизни
            var catchJob = new CapsuleCatchAndLifetimeJob
            {
                TargetPos = targetTransform.Position,
                DeltaTime = SystemAPI.Time.DeltaTime,
                ecb = parallelEcb
            };
            state.Dependency = catchJob.ScheduleParallel(state.Dependency);

            // 2. Параллельная джоба удаления погибших сущностей (с активным DeadTag)
            var deadCleanupJob = new DeadCapsuleDestroyJob
            {
                ecb = parallelEcb
            };
            state.Dependency = deadCleanupJob.ScheduleParallel(_deadQuery, state.Dependency);

            // Дожидаемся завершения параллельных джобов перед воспроизведением команд удаления (слайд 6)
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    /// <summary>
    /// Параллельная джоба проверки дистанции до цели и времени жизни капсулы.
    /// </summary>
    [BurstCompile]
    public partial struct CapsuleCatchAndLifetimeJob : IJobEntity
    {
        public float3 TargetPos;
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ecb;

        [BurstCompile]
        public void Execute(
            [ChunkIndexInQuery] int chunkIndexInQuery,
            Entity entity,
            in LocalTransform transform,
            ref LifetimeData lifetime)
        {
            lifetime.RemainingTime -= DeltaTime;

            float3 capsulePos = transform.Position;
            capsulePos.y = TargetPos.y;
            float distanceToTarget = math.distance(capsulePos, TargetPos);

            if (distanceToTarget < 0.8f || lifetime.RemainingTime <= 0f)
            {
                ecb.DestroyEntity(chunkIndexInQuery, entity);
            }
        }
    }

    /// <summary>
    /// Параллельная джоба уничтожения сущностей с включенным компонентом DeadTag.
    /// </summary>
    [BurstCompile]
    public partial struct DeadCapsuleDestroyJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;

        [BurstCompile]
        public void Execute(
            [ChunkIndexInQuery] int chunkIndexInQuery,
            Entity entity,
            in HealthData health)
        {
            if (health.Current <= 0f)
            {
                ecb.DestroyEntity(chunkIndexInQuery, entity);
            }
        }
    }
}
