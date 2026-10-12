using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Система удаления Entity через EntityCommandBuffer (п. 1 практического задания, слайд 7 методички).
    /// Отслеживает поимку цели капсулами или истечение времени их жизни.
    /// Записывает команды удаления сущностей в EntityCommandBuffer, а затем разом воспроизводит их.
    /// </summary>
    [BurstCompile]
    public partial struct CapsuleCatchAndDestroySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TargetTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Находим позицию цели
            if (!SystemAPI.TryGetSingletonEntity<TargetTag>(out Entity targetEntity))
            {
                return;
            }

            ComponentLookup<LocalTransform> transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            if (!transformLookup.TryGetComponent(targetEntity, out LocalTransform targetTransform))
            {
                return;
            }

            float3 targetPos = targetTransform.Position;
            float deltaTime = SystemAPI.Time.DeltaTime;

            // Создаем EntityCommandBuffer с аллокатором Temp (слайд 7)
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            // 1. Проверяем капсулы с компонентом времени жизни (LifetimeData)
            foreach (var (transform, lifetime, entity) in 
                SystemAPI.Query<RefRO<LocalTransform>, RefRW<LifetimeData>>()
                    .WithEntityAccess())
            {
                lifetime.ValueRW.RemainingTime -= deltaTime;

                float3 capsulePos = transform.ValueRO.Position;
                capsulePos.y = targetPos.y; // Сравниваем дистанцию в плоскости движения
                float distanceToTarget = math.distance(capsulePos, targetPos);

                // Если капсула догнала цель (< 0.8м) или вышло время жизни (<= 0)
                if (distanceToTarget < 0.8f || lifetime.ValueRO.RemainingTime <= 0f)
                {
                    // Запись в EntityCommandBuffer команды удаления Entity (слайд 7)
                    ecb.DestroyEntity(entity);
                }
            }

            // 2. Также проверяем умершие капсулы (DeadTag) согласно слайду 7 методички
            foreach (var (health, entity) in 
                SystemAPI.Query<RefRO<HealthData>>()
                    .WithAll<DeadTag>()
                    .WithEntityAccess())
            {
                if (health.ValueRO.Current <= 0f)
                {
                    ecb.DestroyEntity(entity);
                }
            }

            // Воспроизведение всех накопленных команд и освобождение памяти (слайд 7)
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
