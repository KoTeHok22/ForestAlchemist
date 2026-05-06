using UnityEngine;
using UnityEditor;
using UnityEditor.Animations; // Важно для работы с контроллером
using System.Linq;
using System.IO;

public class SpriteAnimationGenerator : EditorWindow
{
    private Texture2D spriteSheet;
    private string animName = "Attack";
    private int framesPerRow = 8;
    private float frameRate = 12f;

    [MenuItem("Tools/Generate Full Animator")]
    public static void ShowWindow() => GetWindow<SpriteAnimationGenerator>("Anim Gen");

    private void OnGUI()
    {
        spriteSheet = (Texture2D)EditorGUILayout.ObjectField("Sprite Sheet", spriteSheet, typeof(Texture2D), false);
        animName = EditorGUILayout.TextField("Base Name", animName);
        framesPerRow = EditorGUILayout.IntField("Frames Per Row", framesPerRow);
        frameRate = EditorGUILayout.FloatField("Frame Rate", frameRate);

        if (GUILayout.Button("Generate All") && spriteSheet != null)
        {
            Generate();
        }
    }

    private void Generate()
    {
        string path = AssetDatabase.GetAssetPath(spriteSheet);
        string directory = Path.GetDirectoryName(path);
        Sprite[] allSprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        
        // 1. Создаем Animator Controller
        var controller = AnimatorController.CreateAnimatorControllerAtPath($"{directory}/{animName}Controller.controller");
        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);

        // 2. Создаем Blend Tree в базовом слое
        var rootStateMachine = controller.layers[0].stateMachine;
        var blendTree = new BlendTree();
        AssetDatabase.AddObjectToAsset(blendTree, controller);
        
        blendTree.name = animName + "BlendTree";
        blendTree.blendType = BlendTreeType.SimpleDirectional2D;
        blendTree.blendParameter = "MoveX";
        blendTree.blendParameterY = "MoveY";

        string[] dirs = { "South", "North", "West", "East" };
        Vector2[] positions = { new Vector2(0, -1), new Vector2(0, 1), new Vector2(-1, 0), new Vector2(1, 0) };

        for (int i = 0; i < dirs.Length; i++)
        {
            // Создаем клип
            AnimationClip clip = new AnimationClip { frameRate = frameRate };
            EditorCurveBinding binding = EditorCurveBinding.FloatCurve("", typeof(SpriteRenderer), "m_Sprite");
            var keyframes = allSprites.Skip(i * framesPerRow).Take(framesPerRow)
                .Select((s, f) => new ObjectReferenceKeyframe { time = f / frameRate, value = s }).ToArray();
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
            
            // Настройка цикла
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            // Сохраняем клип
            AssetDatabase.CreateAsset(clip, $"{directory}/{animName}_{dirs[i]}.anim");
            
            // Добавляем клип в Blend Tree
            blendTree.AddChild(clip, positions[i]);
        }

        // Добавляем Blend Tree как состояние в контроллер
        rootStateMachine.AddAnyStateTransition(rootStateMachine.AddState(blendTree.name)); 
        // Примечание: можно просто сделать Default State
        var state = rootStateMachine.states[0].state;
        state.motion = blendTree;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Контроллер и анимации созданы!");
    }
}
