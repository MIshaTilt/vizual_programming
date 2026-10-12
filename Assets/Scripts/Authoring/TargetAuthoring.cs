using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace VisualProgramming.Capsules
{
    [DisallowMultipleComponent]
    public class TargetAuthoring : MonoBehaviour
    {
        [Header("Waypoint Patrol (DynamicBuffer)")]
        [Tooltip("Включить патрулирование по путевым точкам из DynamicBuffer")]
        public bool EnableWaypointPatrol = true;

        [Tooltip("Скорость перемещения между точками")]
        public float PatrolSpeed = 4.0f;

        [Tooltip("Дистанция смены целевой точки")]
        public float ArriveDistance = 0.5f;

        [Tooltip("Список путевых точек патрулирования")]
        public List<Vector3> Waypoints = new List<Vector3>();

        [Header("Circular Movement (Альтернативный режим)")]
        public bool EnableCircularMovement = false;
        public float Radius = 6f;
        public float Speed = 1f;

        public class TargetBaker : Baker<TargetAuthoring>
        {
            public override void Bake(TargetAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new TargetTag());

                if (authoring.EnableWaypointPatrol)
                {
                    DynamicBuffer<WaypointElement> buffer = AddBuffer<WaypointElement>(entity);

                    if (authoring.Waypoints != null && authoring.Waypoints.Count > 0)
                    {
                        for (int i = 0; i < authoring.Waypoints.Count; i++)
                        {
                            buffer.Add(new WaypointElement
                            {
                                Position = (float3)authoring.Waypoints[i]
                            });
                        }
                    }
                    else
                    {
                        buffer.Add(new WaypointElement { Position = new float3(6f, 0.5f, 6f) });
                        buffer.Add(new WaypointElement { Position = new float3(6f, 0.5f, -6f) });
                        buffer.Add(new WaypointElement { Position = new float3(-6f, 0.5f, -6f) });
                        buffer.Add(new WaypointElement { Position = new float3(-6f, 0.5f, 6f) });
                    }

                    AddComponent(entity, new WaypointPatrolData
                    {
                        CurrentWaypointIndex = 0,
                        Speed = authoring.PatrolSpeed,
                        ArriveDistance = authoring.ArriveDistance
                    });
                }
                else if (authoring.EnableCircularMovement)
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
