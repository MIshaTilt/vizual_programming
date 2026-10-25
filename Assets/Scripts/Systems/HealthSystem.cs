using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Параллельная система контроля здоровья сущностей (слайды 4, 5, 6 методички 3).
    /// Использует:
    /// 1. EntityQuery через QueryBuilder с фильтрацией WithDisabled<DeadTag>() (слайд 4).
    /// 2. Атрибут [WithChangeFilter(typeof(HealthData))] над джобой (слайды 4, 5).
    /// 3. EntityCommandBuffer.ParallelWriter с [ChunkIndexInQuery] и принудительным
    ///    завершением state.Dependency.Complete() перед Playback (слайды 5, 6).
    /// </summary>
    [UpdateAfter(typeof(StaminaSystem))]
    [BurstCompile]
    public partial struct HealthSystem : ISystem
    {
        private EntityQuery _healthSystemQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _healthSystemQuery = SystemAPI.QueryBuilder()
                .WithAll<HealthData>()
                .WithAllRW<LocalTransform>()
                .WithDisabled<DeadTag>()
                .Build();

            state.RequireForUpdate(_healthSystemQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

            var job = new HealthJob
            {
                ecb = ecb.AsParallelWriter()
            };

            state.Dependency = job.ScheduleParallel(_healthSystemQuery, state.Dependency);

            // Обязательное ожидание завершения параллельной джобы перед Playback (слайд 6)
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    /// <summary>
    /// Параллельная джоба проверки здоровья с фильтром изменений [WithChangeFilter] (слайд 5 методички 3).
    /// </summary>
    [WithChangeFilter(typeof(HealthData))]
    [BurstCompile]
    public partial struct HealthJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;

        [BurstCompile]
        public void Execute(
            [ChunkIndexInQuery] int chunkIndexInQuery,
            Entity entity,
            in HealthData health,
            ref LocalTransform transform)
        {
            if (health.Current <= 0f)
            {
                ecb.SetComponentEnabled<DeadTag>(chunkIndexInQuery, entity, true);
                transform.Scale = 0.2f;
            }
        }
    }
}
