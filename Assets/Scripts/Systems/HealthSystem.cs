using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Система проверки здоровья сущностей (слайд 8 методички, п. 4 задания).
    /// Проверяет условие Current <= 0, активирует компонент DeadTag
    /// и уменьшает масштаб капсулы при гибели.
    /// </summary>
    [UpdateAfter(typeof(StaminaSystem))]
    [BurstCompile]
    public partial struct HealthSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (health, transform, entity) in 
                SystemAPI.Query<RefRO<HealthData>, RefRW<LocalTransform>>()
                    .WithDisabled<DeadTag>()
                    .WithEntityAccess())
            {
                if (health.ValueRO.Current <= 0f)
                {
                    // Активируем выключаемый компонент DeadTag (слайд 8)
                    state.EntityManager.SetComponentEnabled<DeadTag>(entity, true);

                    // Логика при гибели: уменьшаем масштаб
                    transform.ValueRW.Scale = 0.2f;
                }
            }
        }
    }
}
