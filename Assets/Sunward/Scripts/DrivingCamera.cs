using UnityEngine;
using UnityEngine.InputSystem;

namespace Sunward {
[DisallowMultipleComponent] public sealed class DrivingCamera : MonoBehaviour {
 public ArcadeCar Target; public int Mode; public bool Orbit;
 Camera lens; Vector3 forward=Vector3.forward,positionVelocity; float fovVelocity,orbitAngle; bool ready,wasOrbit;
 readonly RaycastHit[] hits=new RaycastHit[20];
 public void Initialize(ArcadeCar target){Target=target;lens=GetComponent<Camera>();if(!lens)lens=gameObject.AddComponent<Camera>();lens.nearClipPlane=.12f;lens.farClipPlane=Mathf.Max(4000,lens.farClipPlane);lens.allowHDR=true;ready=true;Snap();}
 public void Cycle(){Mode=(Mode+1)%3;Snap();}
 public void Snap(){if(!Target)return;if(!lens)lens=GetComponent<Camera>();forward=Vector3.ProjectOnPlane(Target.transform.forward,Vector3.up).normalized;if(forward.sqrMagnitude<.1f)forward=Vector3.forward;positionVelocity=Vector3.zero;Pose(out var p,out var aim);transform.SetPositionAndRotation(p,Quaternion.LookRotation(aim-p,Vector3.up));if(lens)lens.fieldOfView=Mode==1?79:65;}
 void LateUpdate(){
  if(!ready||!Target)return;float dt=Orbit?Mathf.Min(Time.unscaledDeltaTime,.05f):Mathf.Min(Time.deltaTime,.05f);if(dt<=0)return;
  if(!Orbit&&Target.InputEnabled&&(DriveInput.Press(Key.C)||(Gamepad.current?.rightShoulder.wasPressedThisFrame??false)))Cycle();
  if(Orbit&&!wasOrbit){Vector3 delta=transform.position-Target.transform.position;orbitAngle=Mathf.Atan2(delta.x,delta.z);}wasOrbit=Orbit;
  if(Orbit)orbitAngle+=dt*.16f;
  Vector3 desiredForward=Vector3.ProjectOnPlane(Target.transform.forward,Vector3.up).normalized;
  if(Target.Body&&Target.SpeedKph>28){Vector3 travel=Vector3.ProjectOnPlane(Target.Body.linearVelocity,Vector3.up).normalized;if(Vector3.Dot(travel,desiredForward)>.2f)desiredForward=Vector3.Slerp(desiredForward,travel,.16f);}
  forward=Vector3.Slerp(forward,desiredForward,1-Mathf.Exp(-dt*(Mode==1?18:5.5f))).normalized;
  Pose(out var desired,out var aim);
  if(Mode!=1||Orbit)desired=AvoidObstacles(Target.transform.position+Vector3.up*1.1f,desired);
  if(Vector3.Distance(transform.position,desired)>60){transform.position=desired;positionVelocity=Vector3.zero;}
  else if(Mode==1&&!Orbit)transform.position=desired;
  else{Vector3 travel=Orbit||!Target.Body?Vector3.zero:Target.Body.linearVelocity*dt;transform.position=Vector3.SmoothDamp(transform.position+travel,desired,ref positionVelocity,Orbit?.22f:.13f,1000,dt);}
  Quaternion rotation=Quaternion.LookRotation(aim-transform.position,Vector3.up);
  if(!Orbit&&Mode!=1&&Target.Body){float roll=Mathf.Clamp(-Target.Body.angularVelocity.y*.7f,-1.1f,1.1f);rotation*=Quaternion.Euler(0,0,roll);}
  transform.rotation=Quaternion.Slerp(transform.rotation,rotation,1-Mathf.Exp(-dt*(Mode==1?19:12)));
  float boost=Target.InputEnabled&&DriveInput.Boost&&Target.Boost01>.03f&&Target.SpeedKph>20?3:0;
  float desiredFov=Orbit?54:Mode==1?79+Target.Speed01*8:Mode==2?63+Target.Speed01*10+boost:63+Target.Speed01*13+boost;
  lens.fieldOfView=Mathf.SmoothDamp(lens.fieldOfView,desiredFov,ref fovVelocity,.32f,100,dt);
 }
 void Pose(out Vector3 p,out Vector3 aim){
  var t=Target.transform;Vector3 anchor=t.position;float speed=Target.Speed01;
  if(Orbit){p=anchor+new Vector3(Mathf.Sin(orbitAngle)*7.3f,2.65f,Mathf.Cos(orbitAngle)*7.3f);aim=anchor+Vector3.up*.84f-Vector3.Cross(Vector3.up,anchor-p).normalized*1.9f;return;}
  if(Mode==1){p=t.TransformPoint(new Vector3(0,1.2f,1.49f));aim=p+Vector3.Lerp(forward,t.forward,.7f)*30+Vector3.up*.22f;return;}
  bool wide=Mode==2;float distance=wide?11.3f+speed*1.6f:7.65f+speed*1.1f,height=wide?4.4f+speed*.4f:3.15f+speed*.2f;
  p=anchor-forward*distance+Vector3.up*height;aim=anchor+Vector3.up*(wide?1.1f:1.0f)+forward*Mathf.Lerp(3.7f,7.5f,speed);
 }
 Vector3 AvoidObstacles(Vector3 anchor,Vector3 desired){Vector3 ray=desired-anchor;float length=ray.magnitude;if(length<.01f)return desired;int n=Physics.SphereCastNonAlloc(anchor,.27f,ray/length,hits,length,~0,QueryTriggerInteraction.Ignore);float distance=length;
  for(int i=0;i<n;i++){var hit=hits[i];if(hit.collider.transform.IsChildOf(Target.transform)||hit.rigidbody==Target.Body||hit.distance<.01f)continue;distance=Mathf.Min(distance,Mathf.Max(1.1f,hit.distance-.19f));}return anchor+ray/length*distance;
 }
}
}
