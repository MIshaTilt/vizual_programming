using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace VisualProgramming.Capsules
{
    [UpdateAfter(typeof(MovementSystem))]
    [BurstCompile]
    public partial struct StaminaSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (stamina, entity) in 
                SystemAPI.Query<RefRW<StaminaData>>()
                    .WithAll<ExhaustedTag>()
                    .WithDisabled<DeadTag>()
                    .WithEntityAccess())
            {
                stamina.ValueRW.Current += stamina.ValueRO.RecoveryRate * deltaTime;

                if (stamina.ValueRO.Current >= stamina.ValueRO.Max)
                {
                    stamina.ValueRW.Current = stamina.ValueRO.Max;
                    state.EntityManager.SetComponentEnabled<ExhaustedTag>(entity, false);
                }
            }
        }
    }
}
