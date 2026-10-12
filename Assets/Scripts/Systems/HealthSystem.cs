using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace VisualProgramming.Capsules
{
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
                    state.EntityManager.SetComponentEnabled<DeadTag>(entity, true);

                    transform.ValueRW.Scale = 0.2f;
                }
            }
        }
    }
}
