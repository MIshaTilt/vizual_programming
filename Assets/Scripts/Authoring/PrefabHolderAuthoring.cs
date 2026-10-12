using UnityEngine;
using Unity.Entities;

namespace VisualProgramming.Capsules
{
    [DisallowMultipleComponent]
    public class PrefabHolderAuthoring : MonoBehaviour
    {
        [Header("Capsule Prefabs")]
        [Tooltip("Быстрая капсула")]
        public GameObject FastCapsulePrefab;

        [Tooltip("Сбалансированная капсула")]
        public GameObject BalancedCapsulePrefab;

        [Tooltip("Тяжелая капсула (Танк)")]
        public GameObject TankCapsulePrefab;

        public class PrefabHolderAuthoringBaker : Baker<PrefabHolderAuthoring>
        {
            public override void Bake(PrefabHolderAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new PrefabHolder
                {
                    FastCapsulePrefab = authoring.FastCapsulePrefab != null 
                        ? GetEntity(authoring.FastCapsulePrefab, TransformUsageFlags.Dynamic) 
                        : Entity.Null,

                    BalancedCapsulePrefab = authoring.BalancedCapsulePrefab != null 
                        ? GetEntity(authoring.BalancedCapsulePrefab, TransformUsageFlags.Dynamic) 
                        : Entity.Null,

                    TankCapsulePrefab = authoring.TankCapsulePrefab != null 
                        ? GetEntity(authoring.TankCapsulePrefab, TransformUsageFlags.Dynamic) 
                        : Entity.Null
                });
            }
        }
    }
}
