using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Авторинг-компонент цели для запекания в Entity.
    /// </summary>
    [DisallowMultipleComponent]
    public class TargetAuthoring : MonoBehaviour
    {
        [Header("Circular Movement")]
        public bool EnableCircularMovement = true;
        public float Radius = 6f;
        public float Speed = 1f;

        public class TargetBaker : Baker<TargetAuthoring>
        {
            public override void Bake(TargetAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                // Тег цели для системы синглтона (слайд 9)
                AddComponent(entity, new TargetTag());

                // Если включено автоматическое круговое движение цели
                if (authoring.EnableCircularMovement)
                {
                    AddComponent(entity, new TargetMovementData
                    {
                        Radius = authoring.Radius,
                        Speed = authoring.Speed,
                        Center = (float3)authoring.transform.position
                    });
                }
            }
        }
    }
}
