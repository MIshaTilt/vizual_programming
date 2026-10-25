using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Параллельная система регистрации атак и записи урона в NativeList (слайды 7–9 методички 3).
    /// Запускает параллельную джобу CombatAttackQueryJob (IJobEntity), которая через
    /// NativeList<DamageEvent>.ParallelWriter записывает события урона без гонки данных.
    /// </summary>
    [UpdateAfter(typeof(StaminaSystem))]
    [BurstCompile]
    public partial struct CombatAttackQuerySystem : ISystem
    {
        private EntityQuery _attackersQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DamageBufferHolder>();

            _attackersQuery = SystemAPI.QueryBuilder()
                .WithAll<LocalTransform, AttackerData>()
                .WithDisabled<DeadTag>()
                .Build();

            state.RequireForUpdate(_attackersQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ref var holder = ref SystemAPI.GetSingletonRW<DamageBufferHolder>().ValueRW;
            if (!holder.Damages.IsCreated)
            {
                holder.Damages = new NativeList<DamageEvent>(1024, Allocator.Persistent);
            }

            ComponentLookup<LocalTransform> transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

            var job = new CombatAttackQueryJob
            {
                Damages = holder.Damages.AsParallelWriter(),
                TransformLookup = transformLookup,
                DeltaTime = SystemAPI.Time.DeltaTime
            };

            // Планируем параллельную запись урона в очередь работ (слайд 9)
            state.Dependency = job.ScheduleParallel(_attackersQuery, state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            // Освобождение неуправляемой памяти NativeList при уничтожении мира (слайд 9)
            if (SystemAPI.TryGetSingleton<DamageBufferHolder>(out var holder) && holder.Damages.IsCreated)
            {
                holder.Damages.Dispose();
            }
        }
    }

    /// <summary>
    /// Параллельная джоба IJobEntity для записи событий урона в NativeList.ParallelWriter (слайд 9 методички 3).
    /// Использует атрибут [NativeDisableContainerSafetyRestriction], так как безопасность гарантируется
    /// цепочкой зависимостей state.Dependency между CombatAttackQuerySystem и CombatDamageHandlerSystem.
    /// </summary>
    [BurstCompile]
    public partial struct CombatAttackQueryJob : IJobEntity
    {
        [NativeDisableContainerSafetyRestriction]
        public NativeList<DamageEvent>.ParallelWriter Damages;

        [ReadOnly]
        public ComponentLookup<LocalTransform> TransformLookup;

        public float DeltaTime;

        [BurstCompile]
        public void Execute(Entity entity, in LocalTransform transform, in AttackerData attacker)
        {
            if (attacker.CurrentTarget == Entity.Null || !TransformLookup.HasComponent(attacker.CurrentTarget))
            {
                return;
            }

            float3 myPos = transform.Position;
            float3 targetPos = TransformLookup[attacker.CurrentTarget].Position;
            targetPos.y = myPos.y;

            float distance = math.distance(myPos, targetPos);

            // 1. Если капсула подошла на дистанцию атаки к цели — параллельно записываем урон по цели
            if (distance <= attacker.AttackRange)
            {
                Damages.AddNoResize(new DamageEvent
                {
                    DamageTarget = attacker.CurrentTarget,
                    Damage = attacker.DamagePerSecond * DeltaTime
                });
            }

            // 2. Защитное поле вокруг цели также наносит ответный урон капсулам в радиусе 5 метров
            if (distance <= 5.0f)
            {
                Damages.AddNoResize(new DamageEvent
                {
                    DamageTarget = entity,
                    Damage = 18.0f * DeltaTime
                });
            }
        }
    }
}
