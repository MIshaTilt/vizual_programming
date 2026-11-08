using UnityEditor;
using UnityEngine;
using MCPForUnity.Editor.Services;

namespace VisualProgramming.Editor
{
    [InitializeOnLoad]
    public static class MCPAutoConnector
    {
        static MCPAutoConnector()
        {
            EditorApplication.delayCall += async () =>
            {
                // Автоматическая настройка сцены под Практическое задание 4 (Unity Physics),
                // если на префабах еще не настроен Rigidbody и ProvidesContacts
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    var fastPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/FastCapsulePrefab.prefab");
                    if (fastPrefab == null || fastPrefab.GetComponent<Rigidbody>() == null)
                    {
                        SceneSetupHelper.SetupAndValidatePracticalTask();
                    }
                }

                var bridge = MCPServiceLocator.Bridge;
                if (bridge != null && !bridge.IsRunning)
                {
                    await bridge.StartAsync();
                }
            };
        }
    }
}
