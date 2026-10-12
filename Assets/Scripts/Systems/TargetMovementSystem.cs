using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace VisualProgramming.Capsules
{
    [BurstCompile]
    public partial struct TargetMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TargetMovementData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            double elapsedTime = SystemAPI.Time.ElapsedTime;

            foreach (var (transform, movement) in 
                SystemAPI.Query<RefRW<LocalTransform>, RefRO<TargetMovementData>>())
            {
                float angle = (float)elapsedTime * movement.ValueRO.Speed;
                float3 offset = new float3(
                    math.cos(angle) * movement.ValueRO.Radius,
                    0f,
                    math.sin(angle) * movement.ValueRO.Radius
                );

                transform.ValueRW.Position = movement.ValueRO.Center + offset;
            }
        }
    }
}
