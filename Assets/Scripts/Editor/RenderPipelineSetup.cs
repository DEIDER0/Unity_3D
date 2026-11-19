using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Dungeon.EditorTools
{
    /// <summary>
    /// EDITOR. Si el proyecto todavía no tiene un Render Pipeline asignado, crea un asset de URP
    /// (Assets/Settings) y lo asigna en Graphics. Después reimporta los modelos para que sus
    /// materiales se regeneren con el shader de URP (si no, se verían rosados).
    /// Menú: Dungeon > Setup URP.
    /// </summary>
    public static class RenderPipelineSetup
    {
        private const string SettingsFolder = "Assets/Settings";

        [MenuItem("Dungeon/Setup URP", priority = 30)]
        public static void SetupMenu()
        {
            if (!EnsureUrp()) Debug.Log("[Dungeon] URP ya estaba configurado.");
        }

        /// <summary>Devuelve true si tuvo que configurar URP en este momento.</summary>
        public static bool EnsureUrp()
        {
            if (GraphicsSettings.defaultRenderPipeline != null) return false;

            EditorUtil.EnsureFolder(SettingsFolder);
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rendererData, $"{SettingsFolder}/URP_Renderer.asset");

            var pipeline = UniversalRenderPipelineAsset.Create(rendererData);
            AssetDatabase.CreateAsset(pipeline, $"{SettingsFolder}/URP_PipelineAsset.asset");
            AssetDatabase.SaveAssets();

            GraphicsSettings.defaultRenderPipeline = pipeline;
            // Los niveles de calidad sin pipeline propio usan el de Graphics.
            AssetDatabase.SaveAssets();

            // Regenera los materiales de los FBX con el shader de URP.
            AssetDatabase.ImportAsset("Assets/Models", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            Debug.Log("[Dungeon] URP configurado: Assets/Settings/URP_PipelineAsset.asset asignado en Project Settings > Graphics.");
            return true;
        }
    }
}
