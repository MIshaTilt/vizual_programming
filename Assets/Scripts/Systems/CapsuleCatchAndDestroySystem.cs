using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace VisualProgramming.Capsules
{
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

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (transform, lifetime, entity) in 
                SystemAPI.Query<RefRO<LocalTransform>, RefRW<LifetimeData>>()
                    .WithEntityAccess())
            {
                lifetime.ValueRW.RemainingTime -= deltaTime;

                float3 capsulePos = transform.ValueRO.Position;
                capsulePos.y = targetPos.y;
                float distanceToTarget = math.distance(capsulePos, targetPos);

                if (distanceToTarget < 0.8f || lifetime.ValueRO.RemainingTime <= 0f)
                {
                    ecb.DestroyEntity(entity);
                }
            }

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

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
