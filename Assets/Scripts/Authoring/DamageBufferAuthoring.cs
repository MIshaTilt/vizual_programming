using UnityEngine;
using Unity.Collections;
using Unity.Entities;

namespace VisualProgramming.Capsules
{
    /// <summary>
    /// Авторинг-компонент для создания синглтон-буфера урона NativeList (слайд 8 методички 3).
    /// </summary>
    [DisallowMultipleComponent]
    public class DamageBufferAuthoring : MonoBehaviour
    {
        [Tooltip("Начальная вместимость буфера урона NativeList")]
        public int InitialCapacity = 1024;

        public class DamageBufferBaker : Baker<DamageBufferAuthoring>
        {
            public override void Bake(DamageBufferAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);

                int capacity = authoring.InitialCapacity > 0 ? authoring.InitialCapacity : 1024;

                // Создание синглтона с NativeList в неуправляемой памяти (слайд 8)
                AddComponent(entity, new DamageBufferHolder
                {
                    Damages = new NativeList<DamageEvent>(capacity, Allocator.Persistent)
                });
            }
        }
    }
}
