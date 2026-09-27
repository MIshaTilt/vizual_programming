using UnityEditor;
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
                var bridge = MCPServiceLocator.Bridge;
                if (bridge != null && !bridge.IsRunning)
                {
                    await bridge.StartAsync();
                }
            };
        }
    }
}
