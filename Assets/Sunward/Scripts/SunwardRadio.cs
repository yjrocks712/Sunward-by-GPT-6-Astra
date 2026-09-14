using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sunward {
public sealed class SunwardRadio:MonoBehaviour {
 public RadioLibrary Library {get;private set;}
 public int StationIndex {get;private set;} public int TrackIndex {get;private set;}
 public float MasterVolume {get;private set;} public float MusicVolume {get;private set;} public float EffectsVolume {get;private set;}
 public bool Muted {get;private set;} public bool Loading {get;private set;} public bool Unavailable {get;private set;}
 public bool On=>StationIndex>=0&&Library&&Library.stations.Length>0;
 public RadioLibrary.Station Station=>On?Library.stations[StationIndex]:null;
 public RadioLibrary.Track Track=>Station!=null&&TrackIndex<Station.tracks.Length?Station.tracks[TrackIndex]:null;
 public float Position=>sources!=null&&sources[current].clip?sources[current].time:0;
 public float Duration=>Track?.clip?Track.clip.length:0;
 public float Progress=>Duration>0?Mathf.Clamp01(Position/Duration):0;
 public float EffectsGain=>(Muted?0:MasterVolume)*EffectsVolume;
 public bool Playing=>On&&!Loading&&sources!=null&&sources[current].isPlaying;
 public string Status=>Unavailable?"SIGNAL UNAVAILABLE":Loading?"TUNING IN":Muted?"ALL AUDIO MUTED":!On?"RADIO OFF":MusicVolume<.001f?"MUSIC VOLUME ZERO":"NOW PLAYING";
 AudioSource[] sources;int current,previous=-1,lastStation;int[] cursors;float fade,fadeDuration=2.4f,gain,saveAt;bool dirty;Coroutine pending;
 const string KeyPrefix="Sunward.radio.";
 public void Initialize(){
  Library=Resources.Load<RadioLibrary>("RadioLibrary");StationIndex=-1;
  MasterVolume=Mathf.Clamp01(PlayerPrefs.GetFloat(KeyPrefix+"master",.85f));MusicVolume=Mathf.Clamp01(PlayerPrefs.GetFloat(KeyPrefix+"music",.72f));EffectsVolume=Mathf.Clamp01(PlayerPrefs.GetFloat(KeyPrefix+"effects",.68f));Muted=PlayerPrefs.GetInt("sunward.muted",0)==1;
  sources=new AudioSource[2];for(int i=0;i<2;i++){var go=new GameObject("Radio deck "+(i+1));go.transform.SetParent(transform,false);var s=go.AddComponent<AudioSource>();s.playOnAwake=false;s.loop=false;s.spatialBlend=0;s.priority=16;s.volume=0;s.ignoreListenerPause=true;sources[i]=s;}
  if(!Library||Library.stations==null||Library.stations.Length==0){Unavailable=true;return;}
  cursors=new int[Library.stations.Length];for(int i=0;i<cursors.Length;i++)cursors[i]=PlayerPrefs.GetInt(KeyPrefix+"track."+i,0);
  int saved=Mathf.Clamp(PlayerPrefs.GetInt(KeyPrefix+"station",0),-1,cursors.Length-1);lastStation=Mathf.Clamp(PlayerPrefs.GetInt(KeyPrefix+"last",0),0,cursors.Length-1);if(saved>=0)SelectStation(saved);
 }
 public void SelectStation(int index){
  if(!Library||cursors==null)return;index=Mathf.Clamp(index,-1,cursors.Length-1);if(index==StationIndex&&!Unavailable)return;
  if(pending!=null){StopCoroutine(pending);pending=null;}Loading=false;Unavailable=false;StationIndex=index;
  if(index>=0){lastStation=index;TrackIndex=Mathf.Clamp(cursors[index],0,Mathf.Max(0,Station.tracks.Length-1));Queue();}
  else {if(previous>=0){sources[previous].Stop();previous=-1;}fade=0;}
  PlayerPrefs.SetInt(KeyPrefix+"station",index);PlayerPrefs.SetInt(KeyPrefix+"last",lastStation);SaveSoon();
 }
 public void CycleStation(){if(Library)SelectStation(On?(StationIndex+1)%Library.stations.Length:lastStation);}
 public void Toggle()=>SelectStation(On?-1:lastStation);
 public void NextTrack(){if(!On){SelectStation(lastStation);return;}if(Station.tracks.Length==0)return;TrackIndex=(TrackIndex+1)%Station.tracks.Length;Queue();}
 void Queue(){if(pending!=null)StopCoroutine(pending);Unavailable=false;Loading=true;cursors[StationIndex]=TrackIndex;PlayerPrefs.SetInt(KeyPrefix+"track."+StationIndex,TrackIndex);SaveSoon();pending=StartCoroutine(LoadTrack());}
 IEnumerator LoadTrack(){
  var clip=Track?.clip;if(!clip){Unavailable=true;Loading=false;pending=null;yield break;}
  clip.LoadAudioData();float deadline=Time.realtimeSinceStartup+15;
  while(clip.loadState==AudioDataLoadState.Loading&&Time.realtimeSinceStartup<deadline)yield return null;
  if(clip.loadState!=AudioDataLoadState.Loaded){Unavailable=true;Loading=false;pending=null;yield break;}
  int next=1-current;sources[next].Stop();sources[next].clip=clip;sources[next].volume=0;sources[next].time=0;sources[next].Play();
  previous=sources[current].isPlaying?current:-1;current=next;fade=0;Loading=false;pending=null;
 }
 public void SetMaster(float value){MasterVolume=Mathf.Clamp01(value);PlayerPrefs.SetFloat(KeyPrefix+"master",MasterVolume);SaveSoon();}
 public void SetMusic(float value){MusicVolume=Mathf.Clamp01(value);PlayerPrefs.SetFloat(KeyPrefix+"music",MusicVolume);SaveSoon();}
 public void SetEffects(float value){EffectsVolume=Mathf.Clamp01(value);PlayerPrefs.SetFloat(KeyPrefix+"effects",EffectsVolume);SaveSoon();}
 public void ToggleMute(){Muted=!Muted;PlayerPrefs.SetInt("sunward.muted",Muted?1:0);SaveSoon();}
 void SaveSoon(){dirty=true;saveAt=Time.unscaledTime+.5f;}
 void Update(){
  if(sources==null)return;if(DriveInput.Press(Key.F8))ToggleMute();if(DriveInput.Press(Key.T))CycleStation();if(DriveInput.Press(Key.N))NextTrack();if(DriveInput.Press(Key.V))Toggle();
  var pad=Gamepad.current;if(pad!=null){if(pad.dpad.right.wasPressedThisFrame)CycleStation();if(pad.dpad.left.wasPressedThisFrame)NextTrack();}
  float dt=Time.unscaledDeltaTime;gain=Mathf.MoveTowards(gain,(Muted?0:MasterVolume)*MusicVolume*(Time.timeScale<.01f?.83f:1),dt*2);
  if(On){fade=Mathf.Min(1,fade+dt/fadeDuration);sources[current].volume=gain*Mathf.Sin(fade*Mathf.PI*.5f);if(previous>=0){sources[previous].volume=gain*Mathf.Cos(fade*Mathf.PI*.5f);if(fade>=1){sources[previous].Stop();previous=-1;}}
   if(!Loading&&!Unavailable&&sources[current].clip&&fade>=1&&(sources[current].clip.length-sources[current].time<=fadeDuration||!sources[current].isPlaying))NextTrack();
  }else {sources[current].volume=Mathf.MoveTowards(sources[current].volume,0,dt*2);if(sources[current].volume<=0)sources[current].Stop();}
  if(dirty&&Time.unscaledTime>=saveAt){PlayerPrefs.Save();dirty=false;}
 }
 void OnApplicationPause(bool paused){if(paused&&dirty)PlayerPrefs.Save();}
 void OnDestroy(){if(dirty)PlayerPrefs.Save();}
}
}
