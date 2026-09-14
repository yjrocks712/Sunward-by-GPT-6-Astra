using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Sunward {
public sealed class RadioVerification:MonoBehaviour {
 public string destination;readonly List<string> errors=new(),checks=new();readonly List<ClipEvidence> clips=new();float[] samples=new float[1024];SunwardGame game;SunwardRadio radio;bool restored;int savedStation,savedTrack;float savedMaster,savedMusic,savedEffects;bool savedMute;
 [Serializable] sealed class ClipEvidence {public string title,station,load;public int channels;public float seconds,rms;}
 [Serializable] sealed class Evidence {public bool passed;public string[] checks,errors;public ClipEvidence[] clips;public float averageFps;public long memoryBytes;public int listeners;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Begin(){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-sunward-radio-verify");if(i<0||i+1>=args.Length)return;var test=new GameObject("Radio verification").AddComponent<RadioVerification>();DontDestroyOnLoad(test);test.destination=Path.GetFullPath(args[i+1]);}
 void Awake()=>Application.logMessageReceived+=Log;
 void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);}
 void Check(bool passed,string message){if(passed)checks.Add(message);else errors.Add(message);}
 AudioSource[] Decks()=>radio.GetComponentsInChildren<AudioSource>().Where(s=>s.name.StartsWith("Radio deck")).ToArray();
 AudioSource Active()=>Decks().FirstOrDefault(s=>s.isPlaying&&s.clip==radio.Track?.clip);
 float Rms(AudioSource source){if(!source)return 0;source.GetOutputData(samples,0);double sum=0;foreach(float v in samples)sum+=v*v;return (float)Math.Sqrt(sum/samples.Length);}
 IEnumerator WaitReady(){float until=Time.realtimeSinceStartup+15;while((radio.Loading||!radio.Playing)&&Time.realtimeSinceStartup<until)yield return null;yield return new WaitForSecondsRealtime(2.6f);}
 IEnumerator KeyPress(Key key){var keyboard=Keyboard.current;if(keyboard==null)keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;}
 IEnumerator Start(){
  Directory.CreateDirectory(destination);float deadline=Time.realtimeSinceStartup+40;while((!SunwardGame.Instance||!SunwardGame.Instance.Ready)&&Time.realtimeSinceStartup<deadline)yield return null;
  game=SunwardGame.Instance;if(!game||!game.Ready){errors.Add("Game startup timeout");Finish(0);yield break;}radio=game.Radio;if(!radio||!radio.Library){errors.Add("Missing radio library");Finish(0);yield break;}
  savedStation=radio.StationIndex;savedTrack=radio.TrackIndex;savedMaster=radio.MasterVolume;savedMusic=radio.MusicVolume;savedEffects=radio.EffectsVolume;savedMute=radio.Muted;
  game.Session.ShowMenu(FestivalMenu.Radio);if(radio.Muted)radio.ToggleMute();radio.SetMaster(.85f);radio.SetMusic(.72f);radio.SetEffects(.68f);
  Check(radio.Library.stations.Length==3,"Three stations available");
  for(int station=0;station<radio.Library.stations.Length;station++){
   radio.SelectStation(station);yield return WaitReady();for(int n=0;n<radio.Station.tracks.Length;n++){
    var source=Active();Check(source&&source.isPlaying,"Streaming playback: "+radio.Track.title);if(source)source.time=40;yield return new WaitForSecondsRealtime(.5f);float rms=0;for(int f=0;f<25;f++){rms=Mathf.Max(rms,Rms(source));yield return null;}
    var clip=radio.Track.clip;clips.Add(new ClipEvidence{title=radio.Track.title,station=radio.Station.name,load=clip.loadType.ToString(),channels=clip.channels,seconds=clip.length,rms=rms});Check(rms>.0001f,"Audible samples: "+radio.Track.title);Check(clip.channels==2&&clip.loadType==AudioClipLoadType.Streaming,"Stereo stream: "+radio.Track.title);
    if(n+1<radio.Station.tracks.Length){radio.NextTrack();yield return new WaitForSecondsRealtime(.8f);Check(Decks().Count(s=>s.isPlaying&&s.volume>0)==2,"Two decks overlap during crossfade");yield return WaitReady();}
   }
  }
  int old=radio.TrackIndex;var current=Active();if(current)current.time=current.clip.length-3;yield return new WaitForSecondsRealtime(4);Check(radio.TrackIndex!=old&&radio.Playing,"Automatic advance before track end");
  radio.SetMusic(0);yield return new WaitForSecondsRealtime(1);Check(Decks().All(s=>s.volume<.001f),"Radio volume zero silences both decks");radio.SetMusic(.72f);
  radio.ToggleMute();yield return new WaitForSecondsRealtime(1);Check(Decks().All(s=>s.volume<.001f)&&radio.EffectsGain==0,"Global mute silences music and effects");radio.ToggleMute();
  radio.SetEffects(.37f);radio.SetMaster(.63f);yield return new WaitForSecondsRealtime(.8f);Check(Mathf.Abs(PlayerPrefs.GetFloat("Sunward.radio.effects")-.37f)<.001f&&Mathf.Abs(PlayerPrefs.GetFloat("Sunward.radio.master")-.63f)<.001f,"Volume settings persist");radio.SetMaster(.85f);radio.SetEffects(.68f);
  yield return KeyPress(Key.V);yield return new WaitForSecondsRealtime(1);Check(!radio.On&&Decks().All(s=>!s.isPlaying),"V switches radio off");yield return KeyPress(Key.V);yield return WaitReady();Check(radio.On&&radio.Playing,"V restores station");
  int before=radio.StationIndex;yield return KeyPress(Key.T);yield return WaitReady();Check(radio.StationIndex==(before+1)%3,"T switches station");int beforeTrack=radio.TrackIndex;yield return KeyPress(Key.N);yield return WaitReady();Check(radio.TrackIndex!=beforeTrack,"N skips track");
  yield return KeyPress(Key.B);Check(game.Session.Menu==FestivalMenu.None,"B closes radio settings");yield return KeyPress(Key.B);Check(game.Session.Menu==FestivalMenu.Radio,"B opens radio settings");
  var slider=game.Session.HUD.GetComponentsInChildren<UnityEngine.UI.Slider>(true).First(s=>s.name=="RADIO volume");slider.value=.48f;Check(Mathf.Abs(radio.MusicVolume-.48f)<.001f,"Radio slider controls music volume");radio.SetMusic(.72f);
  var next=game.Session.HUD.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b=>b.name=="NEXT TRACK  /  N");beforeTrack=radio.TrackIndex;next.onClick.Invoke();yield return WaitReady();Check(radio.TrackIndex!=beforeTrack,"Next-track button is wired");
  Check(FindObjectsByType<UnityEngine.EventSystems.EventSystem>().Length==1,"One interactive UI event system");
  var src=game.GetComponentsInChildren<AudioSource>().First(s=>s.name=="Combustion");yield return new WaitForSecondsRealtime(1);Check(src.volume<.001f,"Driving sounds fade out in menus");
  radio.SelectStation(0);yield return WaitReady();yield return Capture("radio-settings");game.Session.CloseMenu();game.Player.InputEnabled=false;yield return Capture("radio-driving");float start=Time.realtimeSinceStartup;for(int f=0;f<180;f++)yield return null;float fps=180/(Time.realtimeSinceStartup-start);Restore();Finish(fps);
 }
 IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(destination,name+".png"));yield return new WaitForSecondsRealtime(.4f);}
 void Restore(){if(restored||!radio)return;restored=true;radio.SetMaster(savedMaster);radio.SetMusic(savedMusic);radio.SetEffects(savedEffects);if(radio.Muted!=savedMute)radio.ToggleMute();radio.SelectStation(savedStation);if(savedStation>=0&&radio.TrackIndex!=savedTrack)radio.NextTrack();PlayerPrefs.Save();}
 void Finish(float fps){int listeners=FindObjectsByType<AudioListener>().Length;Check(listeners==1,"Exactly one audio listener");var e=new Evidence{passed=errors.Count==0,checks=checks.ToArray(),errors=errors.ToArray(),clips=clips.ToArray(),averageFps=fps,memoryBytes=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),listeners=listeners};File.WriteAllText(Path.Combine(destination,"radio-validation.json"),JsonUtility.ToJson(e,true));
#if UNITY_EDITOR
  Debug.Log("SUNWARD_RADIO_VALIDATION "+e.passed);Destroy(gameObject);
#else
  Application.Quit(e.passed?0:1);
#endif
 }
 void OnDestroy(){Restore();Application.logMessageReceived-=Log;}
}
}
