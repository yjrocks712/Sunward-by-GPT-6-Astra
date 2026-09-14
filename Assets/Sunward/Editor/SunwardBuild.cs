using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Sunward.Editor {
public static class SunwardBuild {
 public const string ScenePath="Assets/Sunward/Scenes/Sunward.unity";
 [MenuItem("Sunward/Prepare and open game")]
 public static void Prepare(){EditorSettings.enterPlayModeOptionsEnabled=false;Directory.CreateDirectory("Assets/Sunward/Scenes");AssetDatabase.Refresh();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("SUNWARD • game director").AddComponent<SunwardGame>();EditorSceneManager.SaveScene(scene,ScenePath);EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};PlayerSettings.productName="Sunward";PlayerSettings.companyName="Sunward Studio";PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.FullScreenWindow;PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Linear;PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone,"studio.sunward.coast");int quality=Array.IndexOf(QualitySettings.names,"PC");if(quality>=0)QualitySettings.SetQualityLevel(quality,true);var rp=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");if(rp){GraphicsSettings.defaultRenderPipeline=rp;QualitySettings.renderPipeline=rp;rp.renderScale=1;rp.msaaSampleCount=2;rp.supportsHDR=true;rp.supportsCameraDepthTexture=true;rp.shadowDistance=180;rp.mainLightShadowmapResolution=2048;EditorUtility.SetDirty(rp);}KeepShaders();AssetDatabase.SaveAssets();Debug.Log("SUNWARD_SETUP_READY "+ScenePath);}
 static void KeepShaders(){
  const string root="Assets/Sunward/Resources/RuntimeShaders";Directory.CreateDirectory(root);AssetDatabase.Refresh();
  void MaterialAsset(string name,string shaderName,bool emission=false,bool transparent=false){var shader=Shader.Find(shaderName);if(!shader)throw new InvalidOperationException("Missing runtime shader: "+shaderName);string path=root+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(shader){name=name};AssetDatabase.CreateAsset(m,path);}m.enableInstancing=true;if(emission){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.white);}if(transparent){m.SetFloat("_Surface",1);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3000;}EditorUtility.SetDirty(m);}
  MaterialAsset("CoastSky","Sunward/CoastalSky");MaterialAsset("Opaque","Universal Render Pipeline/Lit");MaterialAsset("Emissive","Universal Render Pipeline/Lit",true);MaterialAsset("Markers","Universal Render Pipeline/Unlit");MaterialAsset("TireHaze","Universal Render Pipeline/Particles/Unlit",false,true);
  var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);var list=settings.FindProperty("m_AlwaysIncludedShaders");for(int i=list.arraySize-1;i>=0;i--){var shader=list.GetArrayElementAtIndex(i).objectReferenceValue as Shader;if(shader&&(shader.name.StartsWith("Universal Render Pipeline/")||shader.name=="Sunward/CoastalSky"||shader.name=="TextMeshPro/Distance Field")){list.GetArrayElementAtIndex(i).objectReferenceValue=null;list.DeleteArrayElementAtIndex(i);}}settings.ApplyModifiedPropertiesWithoutUndo();
 }
 [MenuItem("Sunward/Build macOS player")]
 public static void BuildMac(){AssetDatabase.SaveAssets();Directory.CreateDirectory("outputs");var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="outputs/Sunward.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.None});var summary=report.summary;File.WriteAllText("outputs/build-report.json",JsonUtility.ToJson(new BuildEvidence{result=summary.result.ToString(),errors=(int)summary.totalErrors,warnings=(int)summary.totalWarnings,bytes=(long)summary.totalSize,seconds=(float)summary.totalTime.TotalSeconds},true));if(summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Build failed: "+summary.result);Debug.Log("SUNWARD_BUILD_SUCCEEDED "+summary.totalSize);}
 [Serializable] sealed class BuildEvidence{public string result;public int errors,warnings;public long bytes;public float seconds;}
}
}
