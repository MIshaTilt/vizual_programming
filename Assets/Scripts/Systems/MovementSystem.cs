using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Параллельная система перемещения капсул к их целевой позиции (методички 3 и 4).
    /// Поддерживает:
    /// 1. Физическое движение через PhysicsVelocity.Linear и блокировку наклона капсулы
    ///    через PhysicsMass.InverseInertia (слайды 2, 3 методички 4).
    /// 2. Трансформ-движение для сущностей без PhysicsVelocity.
    /// </summary>
    [UpdateBefore(typeof(StaminaSystem))]
    [BurstCompile]
    public partial struct MovementSystem : ISystem
    {
        private EntityQuery _physicsMovementQuery;
        private EntityQuery _transformMovementQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _physicsMovementQuery = SystemAPI.QueryBuilder()
                .WithAllRW<LocalTransform, PhysicsVelocity>()
                .WithAllRW<PhysicsMass, StaminaData>()
                .WithAll<MoveData>()
                .WithDisabled<DeadTag, ExhaustedTag>()
                .Build();

            _transformMovementQuery = SystemAPI.QueryBuilder()
                .WithAllRW<LocalTransform, StaminaData>()
                .WithAll<MoveData>()
                .WithNone<PhysicsVelocity>()
                .WithDisabled<DeadTag, ExhaustedTag>()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_physicsMovementQuery.IsEmptyIgnoreFilter && _transformMovementQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            EntityCommandBuffer.ParallelWriter parallelEcb = ecb.AsParallelWriter();
            float deltaTime = SystemAPI.Time.DeltaTime;

            // 1. Физическое движение через PhysicsVelocity + ограничение вращения PhysicsMass (слайды 2, 3 методички 4)
            if (!_physicsMovementQuery.IsEmptyIgnoreFilter)
            {
                var uprightJob = new PhysicsUprightAndRotationJob();
                state.Dependency = uprightJob.ScheduleParallel(_physicsMovementQuery, state.Dependency);

                var physicsJob = new PhysicsMovementJob
                {
                    DeltaTime = deltaTime,
                    ecb = parallelEcb
                };
                state.Dependency = physicsJob.ScheduleParallel(_physicsMovementQuery, state.Dependency);
            }

            // 2. Прямое движение через LocalTransform (для нефизических сущностей)
            if (!_transformMovementQuery.IsEmptyIgnoreFilter)
            {
                var transformJob = new MovementJob
                {
                    DeltaTime = deltaTime,
                    ecb = parallelEcb
                };
                state.Dependency = transformJob.ScheduleParallel(_transformMovementQuery, state.Dependency);
            }

            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    /// <summary>
    /// Параллельная джоба фиксации вертикального положения капсулы через PhysicsMass.InverseInertia (слайд 2 методички 4)
    /// и поворота в сторону цели.
    /// </summary>
    [BurstCompile]
    public partial struct PhysicsUprightAndRotationJob : IJobEntity
    {
        [BurstCompile]
        public void Execute(ref PhysicsMass physicsMass, ref LocalTransform transform, in MoveData move)
        {
            // Ограничение поворота по осям X и Z (Rotation Constraints), чтобы капсула не падала (слайд 2 методички 4)
            physicsMass.InverseInertia = new float3(0f, physicsMass.InverseInertia.y, 0f);

            float3 currentPos = transform.Position;
            float3 targetPos = move.TargetPosition;
            targetPos.y = currentPos.y;

            float3 direction = targetPos - currentPos;
            if (math.lengthsq(direction) > 0.01f)
            {
                transform.Rotation = quaternion.LookRotationSafe(math.normalize(direction), math.up());
            }
        }
    }

    /// <summary>
    /// Параллельная джоба физического перемещения капсул через PhysicsVelocity.Linear (слайд 3 методички 4).
    /// </summary>
    [BurstCompile]
    public partial struct PhysicsMovementJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ecb;

        [BurstCompile]
        public void Execute(
            [ChunkIndexInQuery] int chunkIndexInQuery,
            Entity entity,
            ref PhysicsVelocity physicsVelocity,
            ref StaminaData stamina,
            in LocalTransform transform,
            in MoveData move)
        {
            float3 currentPos = transform.Position;
            float3 targetPos = move.TargetPosition;
            targetPos.y = currentPos.y;

            float3 direction = targetPos - currentPos;
            float distance = math.length(direction);

            if (distance > 0.25f)
            {
                float3 dirNormalized = math.normalize(direction);

                // Движение за счет физики через PhysicsVelocity.Linear (слайд 3 методички 4)
                float3 desiredVelocity = dirNormalized * move.Speed;
                physicsVelocity.Linear = new float3(desiredVelocity.x, physicsVelocity.Linear.y, desiredVelocity.z);
                physicsVelocity.Angular = float3.zero;

                // Расход выносливости
                stamina.Current -= stamina.DrainRate * DeltaTime;

                if (stamina.Current <= 0f)
                {
                    stamina.Current = 0f;
                    physicsVelocity.Linear = new float3(0f, physicsVelocity.Linear.y, 0f);
                    ecb.SetComponentEnabled<ExhaustedTag>(chunkIndexInQuery, entity, true);
                }
            }
            else
            {
                physicsVelocity.Linear = new float3(0f, physicsVelocity.Linear.y, 0f);
            }
        }
    }

    /// <summary>
    /// Параллельная джоба движения для сущностей без компонента PhysicsVelocity.
    /// </summary>
    [BurstCompile]
    public partial struct MovementJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ecb;

        [BurstCompile]
        public void Execute(
            [ChunkIndexInQuery] int chunkIndexInQuery,
            Entity entity,
            ref LocalTransform transform,
            in MoveData move,
            ref StaminaData stamina)
        {
            float3 currentPos = transform.Position;
            float3 targetPos = move.TargetPosition;
            targetPos.y = currentPos.y;

            float3 direction = targetPos - currentPos;
            float distance = math.length(direction);

            if (distance > 0.5f)
            {
                float3 dirNormalized = math.normalize(direction);
                float step = math.min(move.Speed * DeltaTime, distance);

                transform.Position += dirNormalized * step;
                transform.Rotation = quaternion.LookRotationSafe(dirNormalized, math.up());

                stamina.Current -= stamina.DrainRate * DeltaTime;

                if (stamina.Current <= 0f)
                {
                    stamina.Current = 0f;
                    ecb.SetComponentEnabled<ExhaustedTag>(chunkIndexInQuery, entity, true);
                }
            }
        }
    }
}
