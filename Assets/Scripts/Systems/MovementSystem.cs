using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Система перемещения капсул к их целевой позиции.
    /// Передвигает только живые и не истощенные капсулы.
    /// Расходует выносливость во время движения.
    /// </summary>
    [UpdateBefore(typeof(StaminaSystem))]
    [BurstCompile]
    public partial struct MovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Использование SystemAPI.Time.DeltaTime вместо Time.deltaTime (слайд 6)
            float deltaTime = SystemAPI.Time.DeltaTime;

            // Выборка только тех сущностей, у которых выключены DeadTag и ExhaustedTag (слайд 8)
            foreach (var (transform, move, stamina, entity) in 
                SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveData>, RefRW<StaminaData>>()
                    .WithDisabled<DeadTag>()
                    .WithDisabled<ExhaustedTag>()
                    .WithEntityAccess())
            {
                float3 currentPos = transform.ValueRO.Position;
                float3 targetPos = move.ValueRO.TargetPosition;

                // Двигаемся в плоскости XZ (сохраняя высоту Y капсулы)
                targetPos.y = currentPos.y;

                float3 direction = targetPos - currentPos;
                float distance = math.length(direction);

                // Если капсула еще не достигла цели (с небольшим запасом)
                if (distance > 0.5f)
                {
                    float3 dirNormalized = math.normalize(direction);
                    float step = math.min(move.ValueRO.Speed * deltaTime, distance);

                    // Смещение позиции (слайд 6)
                    transform.ValueRW.Position += dirNormalized * step;

                    // Плавный поворот в сторону цели
                    transform.ValueRW.Rotation = quaternion.LookRotationSafe(dirNormalized, math.up());

                    // Расход выносливости (п. 4 практического задания)
                    stamina.ValueRW.Current -= stamina.ValueRO.DrainRate * deltaTime;

                    // Если выносливость закончилась — активируем компонент истощения (слайд 8)
                    if (stamina.ValueRO.Current <= 0f)
                    {
                        stamina.ValueRW.Current = 0f;
                        state.EntityManager.SetComponentEnabled<ExhaustedTag>(entity, true);
                    }
                }
            }
        }
    }
}
