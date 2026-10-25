using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Параллельная система перемещения капсул к их целевой позиции (слайды 1–6 методички 3).
    /// Использует EntityQuery через QueryBuilder для фильтрации выключенных компонентов DeadTag и ExhaustedTag (слайд 4),
    /// а также EntityCommandBuffer.ParallelWriter с [ChunkIndexInQuery] для активации ExhaustedTag (слайды 5, 6).
    /// </summary>
    [UpdateBefore(typeof(StaminaSystem))]
    [BurstCompile]
    public partial struct MovementSystem : ISystem
    {
        private EntityQuery _movementQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Тонкая настройка EntityQuery через QueryBuilder для фильтрации WithDisabled (слайд 4)
            _movementQuery = SystemAPI.QueryBuilder()
                .WithAllRW<LocalTransform, StaminaData>()
                .WithAll<MoveData>()
                .WithDisabled<DeadTag, ExhaustedTag>()
                .Build();

            state.RequireForUpdate(_movementQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Для параллельных джобов создаем ECB с аллокатором TempJob (слайды 5, 6)
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

            var job = new MovementJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                ecb = ecb.AsParallelWriter()
            };

            // Планируем параллельное выполнение джобы по настроенному запросу _movementQuery (слайды 3, 4)
            state.Dependency = job.ScheduleParallel(_movementQuery, state.Dependency);

            // Дожидаемся завершения параллельной джобы перед воспроизведением ECB (слайды 5, 6)
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    /// <summary>
    /// Параллельная джоба движения сущностей и расхода выносливости (слайды 2, 5 методички 3).
    /// </summary>
    [BurstCompile]
    public partial struct MovementJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ecb;

        [BurstCompile]
        public void Execute(
            [ChunkIndexInQuery] int chunkIndexInQuery,
            Entity entity,
            ref LocalTransform transform,
            in MoveData move,
            ref StaminaData stamina)
        {
            float3 currentPos = transform.Position;
            float3 targetPos = move.TargetPosition;

            // Двигаемся в плоскости XZ (сохраняя высоту Y капсулы)
            targetPos.y = currentPos.y;

            float3 direction = targetPos - currentPos;
            float distance = math.length(direction);

            if (distance > 0.5f)
            {
                float3 dirNormalized = math.normalize(direction);
                float step = math.min(move.Speed * DeltaTime, distance);

                transform.Position += dirNormalized * step;
                transform.Rotation = quaternion.LookRotationSafe(dirNormalized, math.up());

                // Расход выносливости
                stamina.Current -= stamina.DrainRate * DeltaTime;

                // Если выносливость закончилась — включаем тег истощения через ParallelWriter (слайд 5)
                if (stamina.Current <= 0f)
                {
                    stamina.Current = 0f;
                    ecb.SetComponentEnabled<ExhaustedTag>(chunkIndexInQuery, entity, true);
                }
            }
        }
    }
}
