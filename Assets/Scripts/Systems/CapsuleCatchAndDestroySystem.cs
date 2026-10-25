using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Параллельная система удаления Entity через EntityCommandBuffer.ParallelWriter
    /// (задание 2 + оптимизация под Jobs из задания 3, слайды 5–7).
    /// </summary>
    [UpdateAfter(typeof(HealthSystem))]
    [BurstCompile]
    public partial struct CapsuleCatchAndDestroySystem : ISystem
    {
        private EntityQuery _lifetimeQuery;
        private EntityQuery _deadQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TargetTag>();

            _lifetimeQuery = SystemAPI.QueryBuilder()
                .WithAll<LocalTransform>()
                .WithAllRW<LifetimeData>()
                .Build();

            _deadQuery = SystemAPI.QueryBuilder()
                .WithAll<HealthData, DeadTag>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Если на сцене нет ни живых капсул со временем жизни, ни мертвых капсул — ничего не делаем
            if (_lifetimeQuery.IsEmptyIgnoreFilter && _deadQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            if (!SystemAPI.TryGetSingletonEntity<TargetTag>(out Entity targetEntity))
            {
                return;
            }

            // Передаем ComponentLookup только для чтения внутрь джобы (слайды 6, 7),
            // не вызывая чтение на главном потоке до завершения TargetPatrolJob
            ComponentLookup<LocalTransform> transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            EntityCommandBuffer.ParallelWriter parallelEcb = ecb.AsParallelWriter();

            // 1. Параллельная джоба проверки поимки цели и истечения времени жизни
            if (!_lifetimeQuery.IsEmptyIgnoreFilter)
            {
                var catchJob = new CapsuleCatchAndLifetimeJob
                {
                    TargetEntity = targetEntity,
                    TransformLookup = transformLookup,
                    DeltaTime = SystemAPI.Time.DeltaTime,
                    ecb = parallelEcb
                };
                state.Dependency = catchJob.ScheduleParallel(_lifetimeQuery, state.Dependency);
            }

            // 2. Параллельная джоба удаления погибших сущностей (с активным DeadTag)
            if (!_deadQuery.IsEmptyIgnoreFilter)
            {
                var deadCleanupJob = new DeadCapsuleDestroyJob
                {
                    ecb = parallelEcb
                };
                state.Dependency = deadCleanupJob.ScheduleParallel(_deadQuery, state.Dependency);
            }

            // Дожидаемся завершения параллельных джобов перед воспроизведением команд удаления (слайд 6)
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    /// <summary>
    /// Параллельная джоба проверки дистанции до цели и времени жизни капсулы.
    /// Считывает позицию цели потокобезопасно внутри джобы через [ReadOnly] ComponentLookup<LocalTransform>.
    /// </summary>
    [BurstCompile]
    public partial struct CapsuleCatchAndLifetimeJob : IJobEntity
    {
        public Entity TargetEntity;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
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

            if (lifetime.RemainingTime <= 0f)
            {
                ecb.DestroyEntity(chunkIndexInQuery, entity);
                return;
            }

            if (TransformLookup.TryGetComponent(TargetEntity, out LocalTransform targetTransform))
            {
                float3 targetPos = targetTransform.Position;
                float3 capsulePos = transform.Position;
                capsulePos.y = targetPos.y;
                float distanceToTarget = math.distance(capsulePos, targetPos);

                if (distanceToTarget < 0.8f)
                {
                    ecb.DestroyEntity(chunkIndexInQuery, entity);
                }
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
