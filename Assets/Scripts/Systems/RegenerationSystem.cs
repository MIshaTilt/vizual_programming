using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Параллельная система регенерации здоровья (слайды 1–3 методички 3).
    /// Обновляется внутри периодической группы WaveSpawnSystemGroup (раз в 1 секунду).
    /// Планирует параллельную джобу RegenerationJob через ScheduleParallel.
    /// </summary>
    [UpdateInGroup(typeof(WaveSpawnSystemGroup))]
    [BurstCompile]
    public partial struct RegenerationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new RegenerationJob();
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
    }

    /// <summary>
    /// Параллельная джоба IJobEntity для восстановления здоровья каждой сущности на рабочих потоках (слайды 2, 3).
    /// Вместо RefRO/RefRW используются модификаторы параметров in (только чтение) и ref (чтение и запись).
    /// </summary>
    [BurstCompile]
    public partial struct RegenerationJob : IJobEntity
    {
        [BurstCompile]
        public void Execute(in RegenerationData regeneration, ref HealthData health)
        {
            if (health.Current < health.Max)
            {
                health.Current = math.min(health.Max, health.Current + regeneration.HealthRegeneratedPerUpdate);
            }
        }
    }
}
