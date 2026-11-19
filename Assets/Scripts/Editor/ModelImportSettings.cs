using UnityEditor;

namespace Dungeon.EditorTools
{
    /// <summary>
    /// EDITOR. Configura automáticamente la importación de los FBX generados con Blender
    /// (carpeta Assets/Models): escala 1 (1 unidad = 1 metro), sin cámaras, luces ni animaciones
    /// y con materiales a partir del color del FBX.
    /// Solo actúa la primera vez que se importa cada modelo; después respeta los cambios hechos a mano.
    /// </summary>
    public class ModelImportSettings : AssetPostprocessor
    {
        private const string ModelsFolder = "Assets/Models/";

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(ModelsFolder)) return;

            var importer = (ModelImporter)assetImporter;
            if (!importer.importSettingsMissing) return;

            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = false;   // el eje ya viene corregido desde Blender
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.importBlendShapes = false;
            importer.importAnimation = false;      // las animaciones se hacen por código
            importer.animationType = ModelImporterAnimationType.None;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.addCollider = false;          // los colliders los pone PrefabBuilder
            importer.isReadable = false;
        }
    }
}
