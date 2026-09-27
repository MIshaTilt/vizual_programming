using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Система, вращающая цель по кругу вокруг центральной точки.
    /// </summary>
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
            // Используем SystemAPI.Time.ElapsedTime вместо Time.time (слайд 6)
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
