using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace VisualProgramming.Capsules
{
    [UpdateBefore(typeof(StaminaSystem))]
    [BurstCompile]
    public partial struct MovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (transform, move, stamina, entity) in 
                SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveData>, RefRW<StaminaData>>()
                    .WithDisabled<DeadTag>()
                    .WithDisabled<ExhaustedTag>()
                    .WithEntityAccess())
            {
                float3 currentPos = transform.ValueRO.Position;
                float3 targetPos = move.ValueRO.TargetPosition;

                targetPos.y = currentPos.y;

                float3 direction = targetPos - currentPos;
                float distance = math.length(direction);

                if (distance > 0.5f)
                {
                    float3 dirNormalized = math.normalize(direction);
                    float step = math.min(move.ValueRO.Speed * deltaTime, distance);

                    transform.ValueRW.Position += dirNormalized * step;

                    transform.ValueRW.Rotation = quaternion.LookRotationSafe(dirNormalized, math.up());

                    stamina.ValueRW.Current -= stamina.ValueRO.DrainRate * deltaTime;

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
