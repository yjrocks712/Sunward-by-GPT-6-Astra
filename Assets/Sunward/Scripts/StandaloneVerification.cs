using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Sunward {
public sealed class StandaloneVerification:MonoBehaviour {
 string destination;readonly List<string> errors=new();
 [Serializable] sealed class Evidence {public bool passed,grounded;public string unity,car;public int traffic,landmarks,frames;public float roadKm,averageFps;public long memoryBytes;public string[] errors;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Begin(){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-sunward-verify");if(i<0||i+1>=args.Length)return;var go=new GameObject("Standalone validation");DontDestroyOnLoad(go);var test=go.AddComponent<StandaloneVerification>();test.destination=Path.GetFullPath(args[i+1]);}
 void Awake(){Application.logMessageReceived+=Log;}
 void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);}
 IEnumerator Start(){Directory.CreateDirectory(destination);float until=Time.realtimeSinceStartup+40;while((!SunwardGame.Instance||!SunwardGame.Instance.Ready)&&Time.realtimeSinceStartup<until)yield return null;var g=SunwardGame.Instance;
  if(!g||!g.Ready){File.WriteAllText(Path.Combine(destination,"player-validation.json"),"{\"passed\":false,\"error\":\"startup timeout\"}");Application.Quit(1);yield break;}
  g.Player.InputEnabled=false;float start=Time.realtimeSinceStartup;for(int f=0;f<180;f++)yield return null;float elapsed=Time.realtimeSinceStartup-start;
  yield return Capture("drive");g.Session.ShowMenu(FestivalMenu.Events);yield return Capture("events");g.Session.ShowMenu(FestivalMenu.Garage);yield return new WaitForSecondsRealtime(2);yield return Capture("garage");g.Session.ShowMenu(FestivalMenu.Map);yield return Capture("map");g.Session.CloseMenu();
  foreach(var shaderName in new[]{"Sunward/CoastalSky","Universal Render Pipeline/Lit","Universal Render Pipeline/Unlit","Universal Render Pipeline/Particles/Unlit"}){var shader=Shader.Find(shaderName);if(!shader||!shader.isSupported)errors.Add("Missing or unsupported shader: "+shaderName);}
  foreach(var label in g.Session.HUD.GetComponentsInChildren<TMPro.TMP_Text>(true)){var material=label.fontSharedMaterial;if(!material||!material.shader.isSupported||material.shader.name=="Hidden/InternalErrorShader")errors.Add("Unsupported font material: "+label.name);}
  float length=0;foreach(var road in g.World.Roads)length+=road.Length;var e=new Evidence{passed=errors.Count==0&&g.Player.Grounded&&g.World.Roads.Count==3&&g.Traffic.TrafficCount>20,grounded=g.Player.Grounded,unity=Application.unityVersion,car=g.Player.Spec.Name,traffic=g.Traffic.TrafficCount,landmarks=g.World.Landmarks.Count,frames=180,roadKm=length/1000,averageFps=180/elapsed,memoryBytes=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),errors=errors.ToArray()};File.WriteAllText(Path.Combine(destination,"player-validation.json"),JsonUtility.ToJson(e,true));Application.Quit(e.passed?0:1);
 }
 IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(destination,name+".png"));yield return new WaitForSecondsRealtime(.4f);}
 void OnDestroy(){Application.logMessageReceived-=Log;}
}
}
