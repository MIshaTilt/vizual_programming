using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Параллельная система обновления целевой позиции для всех движущихся капсул (слайды 6, 7 методички 3).
    /// Переписана с однопоточного foreach на параллельные джобы IJobEntity с использованием
    /// [ReadOnly] ComponentLookup<LocalTransform>.
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
            // Поиск синглтона цели
            if (!SystemAPI.TryGetSingletonEntity<TargetTag>(out Entity targetEntity))
            {
                return;
            }

            // Получение ComponentLookup только для чтения (isReadOnly: true) (слайды 6, 7)
            ComponentLookup<LocalTransform> transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

            // Планируем параллельную джобу обновления целевой точки движения (слайд 7)
            var selectTargetJob = new SelectTargetJob
            {
                Target = targetEntity,
                TransformLookup = transformLookup
            };
            state.Dependency = selectTargetJob.ScheduleParallel(state.Dependency);

            // Планируем параллельную джобу назначения текущей цели для атаки
            var assignAttackTargetJob = new AssignAttackTargetJob
            {
                Target = targetEntity
            };
            state.Dependency = assignAttackTargetJob.ScheduleParallel(state.Dependency);
        }
    }

    /// <summary>
    /// Параллельная джоба чтения позиции цели через [ReadOnly] ComponentLookup (слайд 7 методички 3).
    /// </summary>
    [BurstCompile]
    public partial struct SelectTargetJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        public Entity Target;

        [BurstCompile]
        public void Execute(ref MoveData move)
        {
            if (TransformLookup.HasComponent(Target))
            {
                move.TargetPosition = TransformLookup[Target].Position;
            }
        }
    }

    /// <summary>
    /// Параллельная джоба привязки Entity цели для системы боя (слайд 9 методички 3).
    /// </summary>
    [BurstCompile]
    public partial struct AssignAttackTargetJob : IJobEntity
    {
        public Entity Target;

        [BurstCompile]
        public void Execute(ref AttackerData attacker)
        {
            attacker.CurrentTarget = Target;
        }
    }
}
