using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Параллельная система пространственных физических запросов через CollisionWorld (п. 1 задания 4, слайды 3–5 методички 4).
    /// Получает PhysicsWorldSingleton -> CollisionWorld и передает его в параллельную джобу IJobEntity,
    /// использующую методы OverlapSphere и CastRay.
    /// </summary>
    [UpdateBefore(typeof(MovementSystem))]
    [UpdateAfter(typeof(TargetUpdateSystem))]
    [BurstCompile]
    public partial struct PhysicsSpatialQuerySystem : ISystem
    {
        private EntityQuery _sensorQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();

            _sensorQuery = SystemAPI.QueryBuilder()
                .WithAll<LocalTransform>()
                .WithAllRW<AttackerData, MoveData>()
                .WithDisabled<DeadTag>()
                .Build();

            state.RequireForUpdate(_sensorQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Получение CollisionWorld из синглтона PhysicsWorldSingleton (слайды 3, 4 методички 4)
            PhysicsWorldSingleton physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            CollisionWorld collisionWorld = physicsWorld.CollisionWorld;

            ComponentLookup<TargetTag> targetLookup = SystemAPI.GetComponentLookup<TargetTag>(true);

            var job = new CapsulePhysicsSensorJob
            {
                CollisionWorld = collisionWorld,
                TargetLookup = targetLookup
            };

            // Планируем параллельное выполнение пространственных запросов в IJobEntity (слайд 4)
            state.Dependency = job.ScheduleParallel(_sensorQuery, state.Dependency);
        }
    }

    /// <summary>
    /// Параллельная джоба IJobEntity, использующая OverlapSphere и CastRay структуры CollisionWorld (слайды 4, 5 методички 4).
    /// </summary>
    [BurstCompile]
    public partial struct CapsulePhysicsSensorJob : IJobEntity
    {
        [ReadOnly] public CollisionWorld CollisionWorld;
        [ReadOnly] public ComponentLookup<TargetTag> TargetLookup;

        [BurstCompile]
        public void Execute(ref AttackerData attacker, ref MoveData move, in LocalTransform transform)
        {
            // Настройка фильтра коллизий CollisionFilter (слайды 3, 4)
            CollisionFilter filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = ~0u,
                GroupIndex = 0
            };

            // 1. Использование CollisionWorld.OverlapSphere внутри IJobEntity для поиска ближайшей цели в радиусе (слайд 4)
            NativeList<DistanceHit> distanceHits = new NativeList<DistanceHit>(Allocator.TempJob);
            float searchRadius = math.max(attacker.AttackRange * 5f, 20f);

            if (CollisionWorld.OverlapSphere(transform.Position, searchRadius, ref distanceHits, filter))
            {
                float minDist = float.PositiveInfinity;
                Entity foundTarget = Entity.Null;
                float3 foundPos = move.TargetPosition;

                foreach (var hit in distanceHits)
                {
                    if (TargetLookup.HasComponent(hit.Entity) && hit.Distance < minDist)
                    {
                        minDist = hit.Distance;
                        foundTarget = hit.Entity;
                        foundPos = hit.Position;
                    }
                }

                if (foundTarget != Entity.Null)
                {
                    attacker.CurrentTarget = foundTarget;
                    move.TargetPosition = foundPos;
                }
            }
            distanceHits.Dispose();

            // 2. Использование CollisionWorld.CastRay внутри IJobEntity для проверки прямой видимости перед капсулой (слайд 5)
            float3 forwardDir = math.forward(transform.Rotation);
            RaycastInput raycastInput = new RaycastInput
            {
                Start = transform.Position + new float3(0f, 0.5f, 0f),
                End = transform.Position + new float3(0f, 0.5f, 0f) + forwardDir * attacker.AttackRange,
                Filter = filter
            };

            if (CollisionWorld.CastRay(raycastInput, out RaycastHit rayHit))
            {
                if (TargetLookup.HasComponent(rayHit.Entity))
                {
                    attacker.CurrentTarget = rayHit.Entity;
                }
            }
        }
    }
}
