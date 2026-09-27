using Unity.Entities;
using Unity.Mathematics;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Данные для движения сущности к целевой позиции.
    /// </summary>
    public struct MoveData : IComponentData
    {
        public float Speed;
        public float3 TargetPosition;
    }

    /// <summary>
    /// Характеристика выносливости сущности (расходуется при беге, восстанавливается при отдыхе).
    /// </summary>
    public struct StaminaData : IComponentData
    {
        public float Current;
        public float Max;
        public float DrainRate;
        public float RecoveryRate;
    }

    /// <summary>
    /// Выключаемый компонент (тег истощения). Когда активен - сущность не может двигаться.
    /// </summary>
    public struct ExhaustedTag : IComponentData, IEnableableComponent
    {
    }

    /// <summary>
    /// Характеристика здоровья сущности.
    /// </summary>
    public struct HealthData : IComponentData
    {
        public float Current;
        public float Max;
    }

    /// <summary>
    /// Выключаемый компонент (тег смерти).
    /// </summary>
    public struct DeadTag : IComponentData, IEnableableComponent
    {
    }

    /// <summary>
    /// Тег для сущности-цели, к которой движутся капсулы.
    /// </summary>
    public struct TargetTag : IComponentData
    {
    }

    /// <summary>
    /// Параметры кругового движения для цели.
    /// </summary>
    public struct TargetMovementData : IComponentData
    {
        public float Radius;
        public float Speed;
        public float3 Center;
    }
}
