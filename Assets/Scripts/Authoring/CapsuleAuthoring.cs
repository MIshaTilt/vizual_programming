using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Авторинг-компонент капсулы для запекания в Entity (слайды 3, 4).
    /// </summary>
    [DisallowMultipleComponent]
    public class CapsuleAuthoring : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("Скорость передвижения")]
        public float Speed = 3.0f;

        [Header("Stamina")]
        [Tooltip("Максимальная выносливость")]
        public float MaxStamina = 100f;

        [Tooltip("Скорость расхода выносливости в секунду при беге")]
        public float StaminaDrainRate = 25f;

        [Tooltip("Скорость восстановления выносливости в секунду при отдыхе")]
        public float StaminaRecoveryRate = 20f;

        [Header("Health")]
        [Tooltip("Максимальное здоровье")]
        public float MaxHealth = 100f;

        public class CapsuleBaker : Baker<CapsuleAuthoring>
        {
            public override void Bake(CapsuleAuthoring authoring)
            {
                // Запекаем Entity с динамическим трансформом (слайд 3 методички)
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                // Добавляем компонент движения
                AddComponent(entity, new MoveData
                {
                    Speed = authoring.Speed,
                    TargetPosition = float3.zero
                });

                // Добавляем компонент выносливости
                AddComponent(entity, new StaminaData
                {
                    Current = authoring.MaxStamina,
                    Max = authoring.MaxStamina,
                    DrainRate = authoring.StaminaDrainRate,
                    RecoveryRate = authoring.StaminaRecoveryRate
                });

                // Добавляем выключаемый компонент истощения и выключаем его по умолчанию (слайд 4)
                AddComponent(entity, new ExhaustedTag());
                SetComponentEnabled<ExhaustedTag>(entity, false);

                // Добавляем компонент здоровья
                AddComponent(entity, new HealthData
                {
                    Current = authoring.MaxHealth,
                    Max = authoring.MaxHealth
                });

                // Добавляем выключаемый компонент смерти и выключаем его по умолчанию
                AddComponent(entity, new DeadTag());
                SetComponentEnabled<DeadTag>(entity, false);
            }
        }
    }
}
