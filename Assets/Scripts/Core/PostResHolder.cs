using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

// Ссылка на ресурсы пакета постобработки: ассет создаёт ProjectSetup, так шейдеры эффектов попадают в билд.
public class PostResHolder : ScriptableObject
{
    public PostProcessResources resources;
}
