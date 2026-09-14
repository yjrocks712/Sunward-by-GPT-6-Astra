#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Sunward.Editor {
public sealed class ControllerInputAudit:MonoBehaviour {
 [System.Serializable] public sealed class Evidence {public bool passed,reset,camera,events,garage,map;public float accelerationKph,boostUsed,maxSlip,driftPoints,reverseMps;}
 public Evidence Result=new();public bool Running;
 Keyboard keyboard;SunwardGame g;readonly List<InputDevice> disabled=new();readonly List<Collider> colliders=new();float boostBefore,driftBefore;bool drifting;
 public void Begin(){Running=true;StartCoroutine(Run());}
 void Keys(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));}
 IEnumerator Run(){g=SunwardGame.Instance;g.Session.ReturnToFestival();g.Player.AutoDrive=false;g.Player.InputEnabled=true;
  foreach(var device in InputSystem.devices)if(device.enabled&&(device is Keyboard||device is Mouse||device is Gamepad)){disabled.Add(device);InputSystem.DisableDevice(device);}
  foreach(var c in g.Traffic.GetComponentsInChildren<Collider>())if(c.enabled){colliders.Add(c);c.enabled=false;}
  keyboard=InputSystem.AddDevice<Keyboard>("Sunward verification keyboard");keyboard.MakeCurrent();boostBefore=g.Player.Boost01;driftBefore=g.Player.DriftPoints;
  Keys(Key.W,Key.LeftShift);yield return new WaitForSeconds(3.5f);Result.accelerationKph=g.Player.SpeedKph;Result.boostUsed=boostBefore-g.Player.Boost01;
  drifting=true;Keys(Key.W,Key.D,Key.Space);yield return new WaitForSeconds(.9f);drifting=false;Result.driftPoints=g.Player.DriftPoints-driftBefore;
  Keys(Key.S);yield return new WaitForSeconds(4.8f);Result.reverseMps=Vector3.Dot(g.Player.Body.linearVelocity,g.Player.transform.forward);
  Vector3 old=g.Player.transform.position;Keys(Key.R);yield return null;yield return null;Keys();yield return new WaitForSeconds(.3f);Result.reset=Vector3.Distance(old,g.Player.transform.position)>2&&g.Player.SpeedKph<5;
  int mode=g.CameraRig.Mode;Keys(Key.C);yield return null;yield return null;Keys();yield return null;Result.camera=g.CameraRig.Mode!=mode;
  Keys(Key.E);yield return new WaitForSecondsRealtime(.2f);Result.events=g.Session.Menu==FestivalMenu.Events;Keys(Key.Escape);yield return new WaitForSecondsRealtime(.2f);Keys();yield return null;
  Keys(Key.G);yield return new WaitForSecondsRealtime(.2f);Result.garage=g.Session.Menu==FestivalMenu.Garage;Keys(Key.Escape);yield return new WaitForSecondsRealtime(.2f);Keys();yield return null;
  Keys(Key.M);yield return new WaitForSecondsRealtime(.2f);Result.map=g.Session.Menu==FestivalMenu.Map;Keys(Key.Escape);yield return new WaitForSecondsRealtime(.2f);Keys();
  Result.passed=Result.accelerationKph>90&&Result.boostUsed>.1f&&Result.maxSlip>.2f&&Result.driftPoints>0&&Result.reverseMps< -2&&Result.reset&&Result.camera&&Result.events&&Result.garage&&Result.map;
  System.IO.File.WriteAllText("work/controller-input-audit.json",JsonUtility.ToJson(Result,true));g.Session.ReturnToFestival();Finish();
 }
 void Update(){if(drifting&&g&&g.Player)Result.maxSlip=Mathf.Max(Result.maxSlip,g.Player.Slip);}
 void Finish(){if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;}foreach(var device in disabled)if(device.added)InputSystem.EnableDevice(device);disabled.Clear();foreach(var c in colliders)if(c)c.enabled=true;colliders.Clear();Running=false;}
 void OnDestroy(){Finish();}
}
}
#endif
