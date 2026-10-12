using Unity.Entities;

namespace VisualProgramming.Capsules
{
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
