using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Система патрулирования цели по путевым точкам из DynamicBuffer (слайды 5, 6 методички).
    /// Демонстрирует работу с DynamicBuffer<WaypointElement> напрямую в запросе SystemAPI.Query
    /// без использования оберток RefRO / RefRW.
    /// </summary>
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
            float deltaTime = SystemAPI.Time.DeltaTime;

            // Слайд 6: DynamicBuffer перечисляется напрямую без RefRO/RefRW
            foreach (var (transform, patrol, waypoints) in 
                SystemAPI.Query<RefRW<LocalTransform>, RefRW<WaypointPatrolData>, DynamicBuffer<WaypointElement>>())
            {
                if (waypoints.IsEmpty)
                {
                    continue;
                }

                int currentIndex = patrol.ValueRO.CurrentWaypointIndex;
                if (currentIndex >= waypoints.Length)
                {
                    patrol.ValueRW.CurrentWaypointIndex = 0;
                    currentIndex = 0;
                }

                float3 currentPos = transform.ValueRO.Position;
                float3 targetWaypointPos = waypoints[currentIndex].Position;
                targetWaypointPos.y = currentPos.y; // Движение на высоте цели

                float3 direction = targetWaypointPos - currentPos;
                float distance = math.length(direction);

                if (distance <= patrol.ValueRO.ArriveDistance)
                {
                    // Достигли текущей путевой точки — переключаемся на следующую (циклический обход)
                    patrol.ValueRW.CurrentWaypointIndex = (currentIndex + 1) % waypoints.Length;
                }
                else
                {
                    float3 dirNorm = math.normalize(direction);
                    float step = math.min(patrol.ValueRO.Speed * deltaTime, distance);
                    transform.ValueRW.Position += dirNorm * step;
                    transform.ValueRW.Rotation = quaternion.LookRotationSafe(dirNorm, math.up());
                }
            }
        }
    }
}
