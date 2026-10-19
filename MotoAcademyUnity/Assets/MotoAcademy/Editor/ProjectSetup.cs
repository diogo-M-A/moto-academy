using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MotoAcademy.Editor
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        private const string Scene="Assets/MotoAcademy/Scenes/Academy.unity";
        private const string Settings="Assets/MotoAcademy/Settings";
        static ProjectSetup(){EditorApplication.delayCall+=FirstImport;}
        private static void FirstImport()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=FirstImport;return;}
            if(!File.Exists(Settings+"/AcademyPipeline.asset"))Configure();
        }

        [MenuItem("Moto Academy/1. Configurar projeto")]
        public static void Configure()
        {
            Directory.CreateDirectory(Settings);Directory.CreateDirectory("Assets/MotoAcademy/Resources");AssetDatabase.Refresh();
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Settings+"/AcademyPipeline.asset");
            if(pipeline==null)
            {
                var renderer=ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer,Settings+"/AcademyRenderer.asset");
                // The package-owned postprocess resource contains the bloom, grading and SMAA shaders.
                renderer.postProcessData=AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
                pipeline=UniversalRenderPipelineAsset.Create(renderer);
                pipeline.name="AcademyPipeline";pipeline.supportsHDR=true;pipeline.msaaSampleCount=4;
                pipeline.shadowDistance=110;pipeline.mainLightShadowmapResolution=2048;pipeline.useSRPBatcher=true;
                var serialized=new SerializedObject(pipeline);
                serialized.FindProperty("m_MainLightShadowsSupported").boolValue=true;
                serialized.FindProperty("m_AdditionalLightShadowsSupported").boolValue=false;
                serialized.FindProperty("m_SoftShadowsSupported").boolValue=true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(pipeline,Settings+"/AcademyPipeline.asset");EditorUtility.SetDirty(renderer);
            }
            GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
            QualitySettings.shadows=UnityEngine.ShadowQuality.All;QualitySettings.shadowResolution=UnityEngine.ShadowResolution.High;
            PlayerSettings.companyName="MotoAcademy";PlayerSettings.productName="Moto Academy • Costa Verde";
            PlayerSettings.defaultScreenWidth=1920;PlayerSettings.defaultScreenHeight=1080;
            PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=false;PlayerSettings.colorSpace=ColorSpace.Linear;
            PlayerSettings.fullScreenMode=FullScreenMode.FullScreenWindow;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            var playerSettings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input=playerSettings.FindProperty("activeInputHandler");if(input!=null)input.intValue=1;playerSettings.ApplyModifiedPropertiesWithoutUndo();
            var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            tags.FindProperty("layers").GetArrayElementAtIndex(8).stringValue="Ground";tags.ApplyModifiedPropertiesWithoutUndo();
            if(AssetDatabase.LoadAssetAtPath<Material>("Assets/MotoAcademy/Resources/AcademyLit.mat")==null)
            {
                var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.EnableKeyword("_EMISSION");
                AssetDatabase.CreateAsset(material,"Assets/MotoAcademy/Resources/AcademyLit.mat");
            }
            if(AssetDatabase.LoadAssetAtPath<Material>("Assets/MotoAcademy/Resources/AcademySky.mat")==null)
            {
                var sky=new Material(Shader.Find("Skybox/Procedural"));sky.SetFloat("_SunSize",.035f);sky.SetFloat("_AtmosphereThickness",1.1f);
                sky.SetColor("_SkyTint",new Color(.52f,.61f,.65f));sky.SetColor("_GroundColor",new Color(.32f,.38f,.30f));
                AssetDatabase.CreateAsset(sky,"Assets/MotoAcademy/Resources/AcademySky.mat");
            }
            if(AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/MotoAcademy/Resources/AcademyVolume.asset")==null)
            {
                var profile=ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile,"Assets/MotoAcademy/Resources/AcademyVolume.asset");
                var tone=profile.Add<Tonemapping>();tone.mode.Override(TonemappingMode.ACES);
                var bloom=profile.Add<Bloom>();bloom.intensity.Override(.22f);bloom.threshold.Override(1.2f);
                var vignette=profile.Add<Vignette>();vignette.intensity.Override(.2f);vignette.smoothness.Override(.5f);
                var colors=profile.Add<ColorAdjustments>();colors.postExposure.Override(.15f);colors.contrast.Override(10);colors.saturation.Override(-6);
                foreach(var component in profile.components)AssetDatabase.AddObjectToAsset(component,profile);
                EditorUtility.SetDirty(profile);
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};
            AssetDatabase.SaveAssets();
            Debug.Log("Moto Academy configurado. Abra Assets/MotoAcademy/Scenes/Academy.unity e pressione Play.");
        }

        [MenuItem("Moto Academy/2. Abrir cena principal")]
        public static void OpenScene()
        {
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(Scene);
        }

        [MenuItem("Moto Academy/3. Gerar Windows x64")]
        public static void BuildWindows()
        {
            Configure();Directory.CreateDirectory("Builds/Windows");
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{Scene},locationPathName="Builds/Windows/MotoAcademy.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(result.summary.result!=BuildResult.Succeeded)throw new BuildFailedException("Build falhou. Consulte o Console do Unity.");
            Debug.Log("Build Windows concluído: Builds/Windows/MotoAcademy.exe");
        }
    }
}
