using Unity.Entities;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Пользовательская группа систем спавна (слайд 9 методички).
    /// Обновляется внутри SimulationSystemGroup со своей частотой (раз в 1 секунду).
    /// Системы внутри группы выполняются только при вызове base.OnUpdate().
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class WaveSpawnSystemGroup : ComponentSystemGroup
    {
        private float _timer = 0f;

        protected override void OnUpdate()
        {
            _timer += SystemAPI.Time.DeltaTime;
            if (_timer >= 1f)
            {
                base.OnUpdate();
                _timer -= 1f;
            }
        }
    }
}
