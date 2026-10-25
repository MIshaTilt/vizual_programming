using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Авторинг-компонент капсулы для запекания в Entity (слайды 3, 4 методички 1 + задание 3 по Jobs).
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

        [Header("Health & Regeneration")]
        [Tooltip("Максимальное здоровье")]
        public float MaxHealth = 100f;

        [Tooltip("Количество восстанавливаемого здоровья за тик группы регенерации (слайды 1-3 методички 3)")]
        public float HealthRegeneratedPerUpdate = 5f;

        [Header("Combat Attack (Jobs NativeList Buffer)")]
        [Tooltip("Урон в секунду при сближении с целью (слайды 7-10 методички 3)")]
        public float DamagePerSecond = 10f;

        [Tooltip("Дистанция атаки по цели")]
        public float AttackRange = 3.5f;

        public class CapsuleBaker : Baker<CapsuleAuthoring>
        {
            public override void Bake(CapsuleAuthoring authoring)
            {
                // Запекаем Entity с динамическим трансформом
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

                // Добавляем выключаемый компонент истощения и выключаем его по умолчанию
                AddComponent(entity, new ExhaustedTag());
                SetComponentEnabled<ExhaustedTag>(entity, false);

                // Добавляем компонент здоровья
                AddComponent(entity, new HealthData
                {
                    Current = authoring.MaxHealth,
                    Max = authoring.MaxHealth
                });

                // Добавляем компонент пассивной регенерации здоровья (слайды 1-3 методички 3)
                AddComponent(entity, new RegenerationData
                {
                    HealthRegeneratedPerUpdate = authoring.HealthRegeneratedPerUpdate
                });

                // Добавляем параметры атаки для параллельной записи в NativeList (слайды 7-10 методички 3)
                AddComponent(entity, new AttackerData
                {
                    CurrentTarget = Entity.Null,
                    DamagePerSecond = authoring.DamagePerSecond,
                    AttackRange = authoring.AttackRange
                });

                // Добавляем выключаемый компонент смерти и выключаем его по умолчанию
                AddComponent(entity, new DeadTag());
                SetComponentEnabled<DeadTag>(entity, false);
            }
        }
    }
}
