using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Параллельная система кругового движения цели (методички 1 и 3).
    /// Переписана на IJobEntity с планированием через ScheduleParallel.
    /// </summary>
    [UpdateBefore(typeof(TargetUpdateSystem))]
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
            var job = new TargetCircularMovementJob
            {
                ElapsedTime = (float)SystemAPI.Time.ElapsedTime
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
    }

    /// <summary>
    /// Параллельная джоба кругового перемещения вокруг центральной точки.
    /// </summary>
    [BurstCompile]
    public partial struct TargetCircularMovementJob : IJobEntity
    {
        public float ElapsedTime;

        [BurstCompile]
        public void Execute(ref LocalTransform transform, in TargetMovementData movement)
        {
            float angle = ElapsedTime * movement.Speed;
            float3 offset = new float3(
                math.cos(angle) * movement.Radius,
                0f,
                math.sin(angle) * movement.Radius
            );

            transform.Position = movement.Center + offset;
        }
    }
}
