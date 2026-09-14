using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sunward {
public sealed class SunwardAudio:MonoBehaviour {
 static SunwardAudio active;static AudioClip[] pings;static AudioClip celebration;
 SunwardGame game;AudioSource engine,wind,tires,ui;float rpm;
 const int Rate=24000;
 public void Initialize(SunwardGame g){active=this;game=g;engine=Loop("Combustion",Make("Combustion",1,t=>{float phase=t*2*Mathf.PI*60;return (Mathf.Sin(phase)*.62f+Mathf.Sin(phase*2)*.18f+Mathf.Sin(phase*3)*.06f)*(1+.08f*Mathf.Sin(phase*.5f));}));wind=Loop("Slipstream",Noise("Slipstream",3,.96f));tires=Loop("Tire scrub",Noise("Tire scrub",2,.6f));engine.gameObject.AddComponent<AudioLowPassFilter>().cutoffFrequency=750;
  wind.gameObject.AddComponent<AudioLowPassFilter>().cutoffFrequency=1100;ui=gameObject.AddComponent<AudioSource>();ui.playOnAwake=false;ui.volume=.3f;if(pings==null){pings=new AudioClip[5];for(int i=0;i<5;i++){float freq=440*Mathf.Pow(2,i*3/12f);pings[i]=Make("Interface "+i,.21f,t=>Mathf.Sin(t*freq*2*Mathf.PI)*Mathf.Exp(-t*22)*.5f);}celebration=Make("Finish flourish",1.4f,t=>{int n=Mathf.Clamp((int)(t/.14f),0,7);int[] notes={0,4,7,12,7,12,16,19};float local=t-n*.14f;float f=330*Mathf.Pow(2,notes[n]/12f);return Mathf.Sin(t*f*2*Mathf.PI)*Mathf.Exp(-local*6)*Mathf.Clamp01((1.4f-t)*2)*.3f;});}g.Player.gameObject.AddComponent<SunwardImpact>();}
 AudioSource Loop(string name,AudioClip clip){var go=new GameObject(name);go.transform.SetParent(transform);var s=go.AddComponent<AudioSource>();s.clip=clip;s.loop=true;s.volume=0;s.playOnAwake=false;s.priority=name.StartsWith("Sunward")?100:32;s.Play();return s;}
 static AudioClip Make(string name,float duration,Func<float,float> wave){int size=Mathf.CeilToInt(duration*Rate);var samples=new float[size];for(int i=0;i<size;i++)samples[i]=wave(i/(float)Rate);var clip=AudioClip.Create(name,size,1,Rate,false);clip.SetData(samples,0);return clip;}
 static AudioClip Noise(string name,float length,float smoothing){var random=new System.Random(name.Length*371);float low=0;return Make(name,length,t=>{low=low*smoothing+(float)(random.NextDouble()*2-1)*(1-smoothing);return low*2;});}
 void Update(){
  if(game==null||game.Player==null||game.Radio==null)return;
  var p=game.Player;float speed=p.SpeedKph,dt=Time.unscaledDeltaTime;float throttle=p.InputEnabled?DriveInput.Throttle:0;
  int gear=Mathf.Clamp((int)(speed/46),0,5);float targetRpm=Mathf.Lerp(.65f,1.9f,Mathf.Clamp01((speed-gear*46)/60))+throttle*.16f;
  rpm=Mathf.Lerp(rpm,targetRpm,dt*7);float master=game.Radio.EffectsGain,drive=Time.timeScale>.01f?master:0;
  engine.pitch=rpm*(1-game.CurrentCarIndex*.045f);
  engine.volume=Mathf.Lerp(engine.volume,drive*(.038f+throttle*.14f+Mathf.Clamp01(speed/180)*.035f),dt*7);
  wind.volume=Mathf.Lerp(wind.volume,drive*Mathf.Pow(Mathf.Clamp01(speed/260),2)*.11f,dt*5);wind.pitch=1+speed/600;
  tires.volume=Mathf.Lerp(tires.volume,drive*(p.IsDrifting?.15f:0),dt*9);tires.pitch=1+Mathf.Clamp01(p.Slip)*.25f;
  ui.volume=master*.30f;
 }
 public static void Ping(int pitch=0){if(active&&active.ui&&pings!=null)active.ui.PlayOneShot(pings[Mathf.Abs(pitch)%pings.Length]);}
 public static void Celebrate(){if(active&&active.ui&&celebration)active.ui.PlayOneShot(celebration);}
 void OnDestroy(){if(active==this)active=null;}
}
public sealed class SunwardImpact:MonoBehaviour {float last;void OnCollisionEnter(Collision c){if(c.relativeVelocity.magnitude>4&&Time.time-last>.35f){SunwardAudio.Ping(0);last=Time.time;}}}
}
