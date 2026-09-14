#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sunward.Editor {
[DefaultExecutionOrder(-300)] public sealed class DrivingAuditDriver : MonoBehaviour {
 [Serializable] public struct Sample { public float time,kph,roadError,steer,throttle,brake;public Vector3 position;public bool grounded; }
 public float maxSpeed,distance,offRoadSeconds,time,maxRoadError,routeProgress,airborneSeconds;
 public int collisions,flips,poseJumps; public bool Running; public string StopReason=""; public readonly List<Sample> Samples=new();
 public float TargetKph=120; public bool Boost;
 ArcadeCar car;FestivalSession session;RoadPath route;DrivingAuditContactRelay relay;
 Vector3 lastPosition;float lastRouteDistance,sampleClock,stallClock;bool wasFlipped,observedRace,activePhase,priorAuto,priorInput,priorBoost;
 float priorThrottle,priorSteer,priorBrake;
 public void Begin(RoadPath route,float targetKph=120,bool boost=false){
  if(Running)End();car=GetComponent<ArcadeCar>();if(!car&&SunwardGame.Instance)car=SunwardGame.Instance.Player;
  if(!car||!car.Body||route==null||route.Points==null||route.Points.Length<3)throw new InvalidOperationException("Driving audit needs a live ArcadeCar and a sampled road.");
  this.route=route;session=SunwardGame.Instance?SunwardGame.Instance.Session:null;TargetKph=Mathf.Clamp(targetKph,15,car.Spec.TopSpeed*1.1f);Boost=boost;
  priorAuto=car.AutoDrive;priorInput=car.InputEnabled;priorBoost=car.AutoBoost;priorThrottle=car.AutoThrottle;priorBrake=car.AutoBrake;priorSteer=car.AutoSteer;
  maxSpeed=distance=offRoadSeconds=time=maxRoadError=routeProgress=airborneSeconds=0;collisions=flips=poseJumps=0;Samples.Clear();StopReason="";sampleClock=stallClock=0;wasFlipped=observedRace=activePhase=false;
  lastPosition=car.Body.position;lastRouteDistance=route.ClosestDistance(lastPosition);Running=true;
  if(gameObject!=car.gameObject){relay=car.gameObject.AddComponent<DrivingAuditContactRelay>();relay.Owner=this;}
  SetHold();enabled=true;
 }
 public void End(){
  if(!Running)return;Running=false;activePhase=false;
  if(car){car.AutoDrive=priorAuto;car.AutoThrottle=priorThrottle;car.AutoSteer=priorSteer;car.AutoBrake=priorBrake;car.AutoBoost=priorBoost;car.InputEnabled=session?session.Menu==FestivalMenu.None&&session.State!=FestivalRace.Countdown:priorInput;}
  if(relay){relay.Owner=null;Destroy(relay);relay=null;}if(string.IsNullOrEmpty(StopReason))StopReason="Stopped by caller";
 }
 void SetHold(){car.AutoThrottle=0;car.AutoSteer=0;car.AutoBrake=1;car.AutoBoost=false;car.AutoDrive=false;car.InputEnabled=false;activePhase=false;}
 void Update(){
  if(!Running)return;if(!car||!car.Body){StopReason="Player no longer available";End();return;}
  if(session&&session.State==FestivalRace.Finished){StopReason="Session reached race results";End();return;}
  if(session&&(session.Menu!=FestivalMenu.None||session.State==FestivalRace.Countdown)){SetHold();lastPosition=car.Body.position;lastRouteDistance=route.ClosestDistance(lastPosition);return;}
  if(Time.deltaTime<=0)return;activePhase=true;if(session&&session.State==FestivalRace.Racing)observedRace=true;
  float dt=Time.deltaTime;Vector3 p=car.Body.position,velocity=Vector3.ProjectOnPlane(car.Body.linearVelocity,Vector3.up),bodyForward=Vector3.ProjectOnPlane(car.transform.forward,Vector3.up).normalized;
  float speed=velocity.magnitude,d=route.ClosestDistance(p);route.Sample(d,out var closest,out var tangent);tangent=Vector3.ProjectOnPlane(tangent,Vector3.up).normalized;
  float roadError=Vector3.ProjectOnPlane(p-closest,Vector3.up).magnitude;float lookahead=Mathf.Clamp(10+speed*.48f,12,36);lookahead+=Mathf.Min(8,roadError*.4f);
  Vector3 target=route.Position(d+lookahead),toTarget=Vector3.ProjectOnPlane(target-p,Vector3.up);
  Vector3 heading=speed>5?Vector3.Slerp(bodyForward,velocity.normalized,.72f):bodyForward;
  float angle=Vector3.SignedAngle(heading,toTarget,Vector3.up)*Mathf.Deg2Rad;
  float curvature=2*Mathf.Sin(angle)/Mathf.Max(5,toTarget.magnitude);
  float yawCapacity=car.Spec.Steering*Mathf.Lerp(1.14f,.46f,car.Speed01)*Mathf.Clamp01(Mathf.Max(2,speed)/5);
  float demand=speed*curvature;float steer=Mathf.Clamp(demand/Mathf.Max(.15f,yawCapacity),-1,1);
  float wantedSpeed=TargetKph/3.6f,gripAcceleration=Mathf.Min(15,car.Spec.Grip*1.55f),preview=Mathf.Clamp(speed*2.5f,45,115);
  for(int i=0;i<9;i++){
   float ahead=i*preview/8;route.Sample(d+ahead,out _,out var a);route.Sample(d+ahead+12,out _,out var b);
   float k=Vector3.Angle(Vector3.ProjectOnPlane(a,Vector3.up),Vector3.ProjectOnPlane(b,Vector3.up))*Mathf.Deg2Rad/12;
   if(k<.00015f)continue;
   float tireLimit=Mathf.Sqrt(gripAcceleration/k),yawLimit=car.Spec.Steering*1.14f/(k+car.Spec.Steering*.68f/(car.Spec.TopSpeed/3.6f));
   float cornerSpeed=Mathf.Min(tireLimit,yawLimit*.80f);wantedSpeed=Mathf.Min(wantedSpeed,Mathf.Sqrt(cornerSpeed*cornerSpeed+2*13*Mathf.Max(0,ahead-7)));
  }
  if(roadError>route.Width*.34f)wantedSpeed=Mathf.Min(wantedSpeed,Mathf.Lerp(17,7,Mathf.InverseLerp(route.Width*.34f,route.Width*1.5f,roadError)));
  if(Mathf.Abs(angle)>1.1f){wantedSpeed=Mathf.Min(wantedSpeed,6);steer=Mathf.Sign(angle);}
  if(!route.Closed){float remaining=route.Length-d;wantedSpeed=Mathf.Min(wantedSpeed,Mathf.Sqrt(2*9*Mathf.Max(0,remaining-4)));if(remaining<6&&speed<2){StopReason="Reached end of open route";End();return;}}
  float speedError=wantedSpeed-speed;
  car.AutoDrive=true;car.InputEnabled=false;car.AutoSteer=steer;
  car.AutoBrake=speedError<-.8f?Mathf.Clamp01(-speedError/9):0;
  car.AutoThrottle=car.AutoBrake>.01f?0:Mathf.Clamp01(.07f+speedError*.21f);
  car.AutoBoost=Boost&&Mathf.Abs(steer)<.23f&&roadError<route.Width*.28f&&wantedSpeed>speed+3&&car.SpeedKph>45;
  time+=dt;maxSpeed=Mathf.Max(maxSpeed,car.SpeedKph);maxRoadError=Mathf.Max(maxRoadError,roadError);
  if(roadError>route.Width*.5f)offRoadSeconds+=dt;if(!car.Grounded)airborneSeconds+=dt;
  bool flipped=Vector3.Dot(car.transform.up,Vector3.up)<.25f;if(flipped&&!wasFlipped)flips++;if(flipped||Vector3.Dot(car.transform.up,Vector3.up)>.65f)wasFlipped=flipped;
  float moved=Vector3.Distance(p,lastPosition);if(moved>Mathf.Max(12,speed*dt*4+3))poseJumps++;else distance+=moved;
  float advance=d-lastRouteDistance;if(route.Closed){if(advance<-route.Length*.5f)advance+=route.Length;else if(advance>route.Length*.5f)advance-=route.Length;}
  if(Mathf.Abs(advance)<Mathf.Max(20,speed*dt*5))routeProgress+=advance;
  lastPosition=p;lastRouteDistance=d;sampleClock+=dt;
  if(sampleClock>=.5f){sampleClock=0;Samples.Add(new Sample{time=time,kph=car.SpeedKph,roadError=roadError,steer=steer,throttle=car.AutoThrottle,brake=car.AutoBrake,position=p,grounded=car.Grounded});}
  stallClock=speed<.7f&&car.AutoThrottle>.5f?stallClock+dt:0;
  if(stallClock>15){StopReason="No forward progress under throttle for 15 seconds";End();}
  else if(!observedRace&&route.Closed&&routeProgress>=route.Length){StopReason="Travelled one closed route";End();}
 }
 void OnCollisionEnter(Collision collision)=>RecordCollision(collision);
 internal void RecordCollision(Collision collision){if(Running&&activePhase&&collision.relativeVelocity.sqrMagnitude>4)collisions++;}
 void OnDisable(){End();}void OnDestroy(){End();}
}
public sealed class DrivingAuditContactRelay:MonoBehaviour {internal DrivingAuditDriver Owner;void OnCollisionEnter(Collision collision){if(Owner)Owner.RecordCollision(collision);}}
}
#endif
