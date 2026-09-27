using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Система обновления целевой позиции для всех движущихся капсул.
    /// Находит единственную цель через TryGetSingletonEntity (слайд 9 методички)
    /// и считывает её положение через ComponentLookup (слайд 8-9 методички).
    /// </summary>
    [UpdateBefore(typeof(MovementSystem))]
    [BurstCompile]
    public partial struct TargetUpdateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TargetTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Поиск синглтона цели (слайд 9)
            if (!SystemAPI.TryGetSingletonEntity<TargetTag>(out Entity targetEntity))
            {
                return;
            }

            // Использование ComponentLookup для чтения трансформа цели (слайды 8, 9)
            ComponentLookup<LocalTransform> transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            if (!transformLookup.TryGetComponent(targetEntity, out LocalTransform targetTransform))
            {
                return;
            }

            float3 targetPosition = targetTransform.Position;

            // Обновляем целевую позицию у всех капсул
            foreach (var move in SystemAPI.Query<RefRW<MoveData>>())
            {
                move.ValueRW.TargetPosition = targetPosition;
            }
        }
    }
}
