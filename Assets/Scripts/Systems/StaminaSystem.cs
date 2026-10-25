using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Параллельная система восстановления выносливости у истощенных капсул (слайды 4–6 методички 3).
    /// Использует EntityQuery через QueryBuilder и EntityCommandBuffer.ParallelWriter.
    /// </summary>
    [UpdateAfter(typeof(MovementSystem))]
    [BurstCompile]
    public partial struct StaminaSystem : ISystem
    {
        private EntityQuery _staminaQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _staminaQuery = SystemAPI.QueryBuilder()
                .WithAllRW<StaminaData>()
                .WithAll<ExhaustedTag>()
                .WithDisabled<DeadTag>()
                .Build();

            state.RequireForUpdate(_staminaQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

            var job = new StaminaRecoveryJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                ecb = ecb.AsParallelWriter()
            };

            state.Dependency = job.ScheduleParallel(_staminaQuery, state.Dependency);

            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    /// <summary>
    /// Параллельная джоба восстановления выносливости (слайды 2, 5 методички 3).
    /// </summary>
    [BurstCompile]
    public partial struct StaminaRecoveryJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ecb;

        [BurstCompile]
        public void Execute([ChunkIndexInQuery] int chunkIndexInQuery, Entity entity, ref StaminaData stamina)
        {
            stamina.Current += stamina.RecoveryRate * DeltaTime;

            if (stamina.Current >= stamina.Max)
            {
                stamina.Current = stamina.Max;
                ecb.SetComponentEnabled<ExhaustedTag>(chunkIndexInQuery, entity, false);
            }
        }
    }
}
