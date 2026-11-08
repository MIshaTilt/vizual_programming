using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Система обработки физических столкновений и триггеров через ICollisionEventsJob и ITriggerEventsJob
    /// (п. 2 задания 4, слайды 5–7 методички 4).
    /// Обновляется в группе PhysicsSystemGroup строго после PhysicsSimulationGroup.
    /// </summary>
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    [BurstCompile]
    public partial struct PhysicsCollisionEventsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Получаем SimulationSingleton и PhysicsWorld для расчета деталей коллизий (слайд 6)
            var simulation = SystemAPI.GetSingleton<SimulationSingleton>();
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

            // 1. Планируем джобу физических столкновений ICollisionEventsJob (слайды 6, 7)
            var collisionJob = new CapsuleCollisionEventJob
            {
                PhysicsWorld = physicsWorld,
                TargetLookup = SystemAPI.GetComponentLookup<TargetTag>(true),
                CapsuleLookup = SystemAPI.GetComponentLookup<MoveData>(true),
                HealthLookup = SystemAPI.GetComponentLookup<HealthData>(false)
            };
            state.Dependency = collisionJob.Schedule(simulation, state.Dependency);

            // 2. Планируем джобу физических триггеров ITriggerEventsJob (слайд 6)
            var triggerJob = new CapsuleTriggerEventJob
            {
                TargetLookup = SystemAPI.GetComponentLookup<TargetTag>(true),
                CapsuleLookup = SystemAPI.GetComponentLookup<MoveData>(true),
                StaminaLookup = SystemAPI.GetComponentLookup<StaminaData>(false)
            };
            state.Dependency = triggerJob.Schedule(simulation, state.Dependency);
        }
    }

    /// <summary>
    /// Джоба обработки физических столкновений ICollisionEventsJob (слайды 6, 7 методички 4).
    /// Поскольку ICollisionEventsJob выполняется в одном потоке, ComponentLookup<HealthData>
    /// безопасно используется для записи без гонки данных.
    /// Проверка EntityA и EntityB выполняется в обе стороны (слайд 7).
    /// </summary>
    [BurstCompile]
    public partial struct CapsuleCollisionEventJob : ICollisionEventsJob
    {
        [ReadOnly] public PhysicsWorld PhysicsWorld;
        [ReadOnly] public ComponentLookup<TargetTag> TargetLookup;
        [ReadOnly] public ComponentLookup<MoveData> CapsuleLookup;
        public ComponentLookup<HealthData> HealthLookup;

        [BurstCompile]
        public void Execute(CollisionEvent collisionEvent)
        {
            Entity entityA = collisionEvent.EntityA;
            Entity entityB = collisionEvent.EntityB;

            // Столкновение: EntityA = Цель, EntityB = Капсула (слайд 7)
            if (TargetLookup.TryGetComponent(entityA, out _) && CapsuleLookup.TryGetComponent(entityB, out _))
            {
                var details = collisionEvent.CalculateDetails(ref PhysicsWorld);
                float impactDamage = math.max(5f, details.EstimatedImpulse);

                if (HealthLookup.TryGetRefRW(entityA, out RefRW<HealthData> targetHealth))
                {
                    targetHealth.ValueRW.Current = math.max(0f, targetHealth.ValueRO.Current - impactDamage);
                }

                // При прямом физическом столкновении с целью капсула наносит удар и разрушается
                if (HealthLookup.TryGetRefRW(entityB, out RefRW<HealthData> capsuleHealth))
                {
                    capsuleHealth.ValueRW.Current = 0f;
                }
            }

            // Столкновение в обратную сторону: EntityB = Цель, EntityA = Капсула (слайд 7)
            if (TargetLookup.TryGetComponent(entityB, out _) && CapsuleLookup.TryGetComponent(entityA, out _))
            {
                var details = collisionEvent.CalculateDetails(ref PhysicsWorld);
                float impactDamage = math.max(5f, details.EstimatedImpulse);

                if (HealthLookup.TryGetRefRW(entityB, out RefRW<HealthData> targetHealth))
                {
                    targetHealth.ValueRW.Current = math.max(0f, targetHealth.ValueRO.Current - impactDamage);
                }

                if (HealthLookup.TryGetRefRW(entityA, out RefRW<HealthData> capsuleHealth))
                {
                    capsuleHealth.ValueRW.Current = 0f;
                }
            }
        }
    }

    /// <summary>
    /// Джоба обработки пересечения триггеров ITriggerEventsJob (слайд 6 методички 4).
    /// </summary>
    [BurstCompile]
    public partial struct CapsuleTriggerEventJob : ITriggerEventsJob
    {
        [ReadOnly] public ComponentLookup<TargetTag> TargetLookup;
        [ReadOnly] public ComponentLookup<MoveData> CapsuleLookup;
        public ComponentLookup<StaminaData> StaminaLookup;

        [BurstCompile]
        public void Execute(TriggerEvent triggerEvent)
        {
            Entity entityA = triggerEvent.EntityA;
            Entity entityB = triggerEvent.EntityB;

            if (TargetLookup.TryGetComponent(entityA, out _) && CapsuleLookup.TryGetComponent(entityB, out _))
            {
                if (StaminaLookup.TryGetRefRW(entityB, out RefRW<StaminaData> stamina))
                {
                    stamina.ValueRW.Current = stamina.ValueRO.Max;
                }
            }

            if (TargetLookup.TryGetComponent(entityB, out _) && CapsuleLookup.TryGetComponent(entityA, out _))
            {
                if (StaminaLookup.TryGetRefRW(entityA, out RefRW<StaminaData> stamina))
                {
                    stamina.ValueRW.Current = stamina.ValueRO.Max;
                }
            }
        }
    }
}
