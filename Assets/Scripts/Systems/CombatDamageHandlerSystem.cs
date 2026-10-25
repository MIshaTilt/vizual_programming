using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Система последовательной обработки накопленного буфера урона через IJob (слайд 10 методички 3).
    /// Запускается строго после CombatAttackQuerySystem и до HealthSystem.
    /// Выполняется в одном рабочем потоке (Schedule), поэтому безопасно модифицирует
    /// ComponentLookup<HealthData> без гонки данных, когда несколько капсул одновременно атакуют одну цель.
    /// </summary>
    [UpdateAfter(typeof(CombatAttackQuerySystem))]
    [UpdateBefore(typeof(HealthSystem))]
    [BurstCompile]
    public partial struct CombatDamageHandlerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DamageBufferHolder>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            NativeList<DamageEvent> damages = SystemAPI.GetSingleton<DamageBufferHolder>().Damages;
            if (!damages.IsCreated)
            {
                return;
            }

            // В IJob (один рабочий поток) ComponentLookup можно использовать для чтения и записи (isReadOnly: false) (слайд 10)
            ComponentLookup<HealthData> healthLookup = SystemAPI.GetComponentLookup<HealthData>(false);

            var job = new CombatDamageHandlerJob
            {
                Damages = damages,
                HealthLookup = healthLookup
            };

            // Планируем последовательный джоб IJob в цепочку зависимостей state.Dependency (слайд 10)
            state.Dependency = job.Schedule(state.Dependency);
        }
    }

    /// <summary>
    /// Последовательная джоба IJob для применения всех событий урона из NativeList (слайд 10 методички 3).
    /// </summary>
    [BurstCompile]
    public struct CombatDamageHandlerJob : IJob
    {
        [NativeDisableContainerSafetyRestriction]
        public NativeList<DamageEvent> Damages;

        public ComponentLookup<HealthData> HealthLookup;

        [BurstCompile]
        public void Execute()
        {
            foreach (DamageEvent damage in Damages)
            {
                if (HealthLookup.TryGetRefRW(damage.DamageTarget, out RefRW<HealthData> health))
                {
                    health.ValueRW.Current = math.max(0f, health.ValueRO.Current - damage.Damage);
                }
            }

            Damages.Clear();
        }
    }
}
