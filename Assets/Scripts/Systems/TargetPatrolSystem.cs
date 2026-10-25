using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Параллельная система патрулирования по путевым точкам из DynamicBuffer (методички 2 и 3).
    /// Использует IJobEntity для параллельного обхода маршрута из DynamicBuffer<WaypointElement>.
    /// </summary>
    [UpdateBefore(typeof(TargetUpdateSystem))]
    [BurstCompile]
    public partial struct TargetPatrolSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WaypointPatrolData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new TargetPatrolJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
    }

    /// <summary>
    /// Параллельная джоба перемещения по путевым точкам из DynamicBuffer<WaypointElement>.
    /// </summary>
    [BurstCompile]
    public partial struct TargetPatrolJob : IJobEntity
    {
        public float DeltaTime;

        [BurstCompile]
        public void Execute(
            ref LocalTransform transform,
            ref WaypointPatrolData patrol,
            in DynamicBuffer<WaypointElement> waypoints)
        {
            if (waypoints.IsEmpty)
            {
                return;
            }

            int currentIndex = patrol.CurrentWaypointIndex;
            if (currentIndex >= waypoints.Length)
            {
                patrol.CurrentWaypointIndex = 0;
                currentIndex = 0;
            }

            float3 currentPos = transform.Position;
            float3 targetWaypointPos = waypoints[currentIndex].Position;
            targetWaypointPos.y = currentPos.y;

            float3 direction = targetWaypointPos - currentPos;
            float distance = math.length(direction);

            if (distance <= patrol.ArriveDistance)
            {
                patrol.CurrentWaypointIndex = (currentIndex + 1) % waypoints.Length;
            }
            else
            {
                float3 dirNorm = math.normalize(direction);
                float step = math.min(patrol.Speed * DeltaTime, distance);
                transform.Position += dirNorm * step;
                transform.Rotation = quaternion.LookRotationSafe(dirNorm, math.up());
            }
        }
    }
}
