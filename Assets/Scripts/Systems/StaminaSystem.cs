using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Система восстановления выносливости (п. 4 практического задания).
    /// Плавно восстанавливает выносливость для истощенных капсул.
    /// Когда выносливость восстанавливается до максимума, отключает тег ExhaustedTag.
    /// </summary>
    [UpdateAfter(typeof(MovementSystem))]
    [BurstCompile]
    public partial struct StaminaSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            // Обработка истощенных капсул (ExhaustedTag включен)
            foreach (var (stamina, entity) in 
                SystemAPI.Query<RefRW<StaminaData>>()
                    .WithAll<ExhaustedTag>()
                    .WithDisabled<DeadTag>()
                    .WithEntityAccess())
            {
                // Плавное восстановление со временем
                stamina.ValueRW.Current += stamina.ValueRO.RecoveryRate * deltaTime;

                // Если выносливость полностью восстановлена — выключаем истощение (слайд 8)
                if (stamina.ValueRO.Current >= stamina.ValueRO.Max)
                {
                    stamina.ValueRW.Current = stamina.ValueRO.Max;
                    state.EntityManager.SetComponentEnabled<ExhaustedTag>(entity, false);
                }
            }
        }
    }
}
