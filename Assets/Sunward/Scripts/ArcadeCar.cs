using UnityEngine;
using UnityEngine.InputSystem;

namespace Sunward {
[DisallowMultipleComponent] public sealed class ArcadeCar : MonoBehaviour {
 public Rigidbody Body; public CarSpec Spec; public VehicleVisual Visual; public bool InputEnabled=true,AutoDrive;
 public float AutoThrottle,AutoSteer,AutoBrake; public bool AutoBoost;
 public float SpeedKph,Speed01,Slip,Boost01=1,DistanceDrivenKm,DriftPoints;
 public bool Grounded,IsDrifting;
 readonly Vector3[] mounts={new(-.79f,.8f,1.31f),new(.79f,.8f,1.31f),new(-.79f,.8f,-1.31f),new(.79f,.8f,-1.31f)};
 readonly RaycastHit[] hits=new RaycastHit[16]; readonly Vector3[] contacts=new Vector3[4]; readonly bool[] touching=new bool[4];
 readonly TrailRenderer[] trails=new TrailRenderer[2]; readonly ParticleSystem[] dust=new ParticleSystem[2];
 Vector3 normal=Vector3.up,safePosition,previousSafePosition,lastPosition; Quaternion safeRotation,previousSafeRotation;
 float throttle,steer,brake,driftBlend,boostDelay,recoverTimer,safeClock,effectsCooldown,forwardSpeed; bool handbrake,boosting,boostRequested,ready;
 PhysicsMaterial bodyMaterial; Material skidMaterial,dustMaterial; Texture2D dustTexture; BoxCollider hull;
 public void Initialize(CarSpec spec,VehicleVisual visual) {
  Body=GetComponent<Rigidbody>();if(!Body)Body=gameObject.AddComponent<Rigidbody>();
  Body.interpolation=RigidbodyInterpolation.Interpolate;Body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
  Body.linearDamping=.018f;Body.angularDamping=.8f;Body.maxAngularVelocity=3.5f;Body.solverIterations=10;Body.solverVelocityIterations=4;
  Body.centerOfMass=new Vector3(0,.39f,-.05f);Body.useGravity=true;Body.isKinematic=false;
  hull=GetComponent<BoxCollider>();if(!hull)hull=gameObject.AddComponent<BoxCollider>();hull.center=new Vector3(0,.8f,0);hull.size=new Vector3(1.83f,.96f,4.02f);
  if(!bodyMaterial){bodyMaterial=new PhysicsMaterial("Sunward vehicle contact"){dynamicFriction=.09f,staticFriction=.12f,bounciness=.04f,frictionCombine=PhysicsMaterialCombine.Minimum,bounceCombine=PhysicsMaterialCombine.Minimum};}hull.sharedMaterial=bodyMaterial;
  SetSpec(spec,visual);if(!ready)CreateEffects();ready=true;Boost01=1;safePosition=previousSafePosition=transform.position+Vector3.up*.3f;safeRotation=previousSafeRotation=Quaternion.Euler(0,transform.eulerAngles.y,0);lastPosition=transform.position;
 }
 public void SetSpec(CarSpec spec,VehicleVisual visual){Spec=spec??CarSpec.All[0];Visual=visual;if(Body){Body.mass=Spec.Mass;Body.centerOfMass=new Vector3(0,.39f,-.05f);}}
 public void ResetTo(Vector3 p,Quaternion r) {
  if(!Body)return;Body.linearVelocity=Vector3.zero;Body.angularVelocity=Vector3.zero;Body.position=p;Body.rotation=r;transform.SetPositionAndRotation(p,r);
  normal=r*Vector3.up;steer=throttle=brake=driftBlend=0;SpeedKph=Speed01=Slip=forwardSpeed=0;recoverTimer=0;Grounded=IsDrifting=boosting=false;effectsCooldown=.45f;lastPosition=p;
  safePosition=previousSafePosition=p;safeRotation=previousSafeRotation=r;
  for(int i=0;i<2;i++){if(trails[i]){trails[i].emitting=false;trails[i].Clear();}if(dust[i])dust[i].Clear();}Body.WakeUp();
 }
 void Update(){
  if(!ready)return;float dt=Time.deltaTime;
  bool hold=!InputEnabled&&!AutoDrive;
  throttle=AutoDrive?Mathf.Clamp01(AutoThrottle):InputEnabled?DriveInput.Throttle:0;
  brake=hold?1:AutoDrive?Mathf.Clamp01(AutoBrake):DriveInput.Brake;
  float desiredSteer=AutoDrive?Mathf.Clamp(AutoSteer,-1,1):InputEnabled?DriveInput.Steer:0;
  steer=Mathf.MoveTowards(steer,desiredSteer,dt*(Mathf.Abs(desiredSteer)<Mathf.Abs(steer)?6f:3.8f));
  handbrake=InputEnabled&&!AutoDrive&&DriveInput.Drift;
  boostRequested=AutoDrive?AutoBoost:InputEnabled&&DriveInput.Boost;
  if(InputEnabled&&!AutoDrive&&(DriveInput.Press(Key.R)||(Gamepad.current?.selectButton.wasPressedThisFrame??false)))ResetTo(previousSafePosition+Vector3.up*.5f,previousSafeRotation);
  Visual?.Animate(forwardSpeed,steer,Mathf.Max(brake,handbrake?1:0),boosting);
  UpdateEffects(dt);
 }
 void FixedUpdate(){
  if(!ready||!SpecIsValid())return;float dt=Time.fixedDeltaTime;Vector3 velocity=Body.linearVelocity,up=transform.up;int count=0;Vector3 normalSum=Vector3.zero;
  for(int i=0;i<4;i++){
   Vector3 origin=transform.TransformPoint(mounts[i]);touching[i]=Probe(origin,-up,1.43f,out var hit);
   if(!touching[i])continue;contacts[i]=hit.point;normalSum+=hit.normal;count++;
   float spring=(1.12f-hit.distance)*65f-Vector3.Dot(Body.GetPointVelocity(origin),hit.normal)*10.5f;
   Body.AddForceAtPosition(hit.normal*Mathf.Clamp(spring,-4,65)*Body.mass*.25f,origin,ForceMode.Force);
  }
  Grounded=count>=2;Vector3 wantedNormal=count>0?normalSum.normalized:Vector3.up;normal=Vector3.Slerp(normal,wantedNormal,1-Mathf.Exp(-dt*9));
  Body.AddForce(Vector3.down*7f,ForceMode.Acceleration);
  Vector3 forward=Vector3.ProjectOnPlane(transform.forward,normal).normalized,right=Vector3.Cross(normal,forward).normalized;
  forwardSpeed=Vector3.Dot(velocity,forward);float sideSpeed=Vector3.Dot(velocity,right),flatSpeed=Vector3.ProjectOnPlane(velocity,normal).magnitude;
  SpeedKph=flatSpeed*3.6f;Speed01=Mathf.Clamp01(SpeedKph/Spec.TopSpeed);Slip=Mathf.Clamp01(Mathf.Abs(Mathf.Atan2(sideSpeed,Mathf.Max(3,Mathf.Abs(forwardSpeed))))/.95f);
  bool hold=!InputEnabled&&!AutoDrive;
  float driftTarget=handbrake&&Mathf.Abs(forwardSpeed)>7?1:0;driftBlend=Mathf.MoveTowards(driftBlend,driftTarget,dt*(driftTarget>driftBlend?4.5f:2.3f));
  IsDrifting=Grounded&&SpeedKph>30&&Slip>.16f&&(driftBlend>.05f||Slip>.34f);
  boosting=boostRequested&&throttle>.1f&&brake<.1f&&Boost01>.025f&&Grounded&&forwardSpeed>1&&!hold&&boostDelay<=0;
  if(boosting){Boost01=Mathf.Max(0,Boost01-dt*.195f);if(Boost01<=.025f)boostDelay=1.4f;}else{boostDelay=Mathf.Max(0,boostDelay-dt);Boost01=Mathf.Min(1,Boost01+dt*(IsDrifting?.105f:.074f));}
  if(Grounded){
   float contactScale=Mathf.Min(1,count*.34f),top=Spec.TopSpeed/3.6f*(boosting?1.14f:1);
   float longitudinal=0;
   if(hold){longitudinal=-Mathf.Clamp(forwardSpeed*8,-42,42);sideSpeed=Vector3.Dot(velocity,right);}
   else if(throttle>.01f){longitudinal=forwardSpeed<-1?throttle*35:throttle*Spec.Power*Mathf.Lerp(1,.65f,Speed01)*Mathf.Clamp01((top-forwardSpeed)/4.5f);}
   if(!hold&&brake>.01f){if(forwardSpeed>1)longitudinal-=brake*43;else if(!AutoDrive&&throttle<.05f)longitudinal-=brake*Spec.Power*.54f*Mathf.Clamp01((10.5f+forwardSpeed)/2);else longitudinal-=Mathf.Clamp(forwardSpeed*12,-43,43)*brake;}
   if(boosting)longitudinal+=Spec.Power*.52f*Mathf.Clamp01((top-forwardSpeed)/5);
   if(handbrake)longitudinal-=Mathf.Sign(forwardSpeed)*Mathf.Min(Mathf.Abs(forwardSpeed)*3,6.8f);
   longitudinal-=Mathf.Sign(forwardSpeed)*Mathf.Min(Mathf.Abs(forwardSpeed)*3,.4f+flatSpeed*flatSpeed*.00025f);
   Body.AddForce(forward*longitudinal*contactScale,ForceMode.Acceleration);
   float grip=Spec.Grip*Mathf.Lerp(1,.22f,driftBlend),maxLateral=Mathf.Lerp(29,11,driftBlend)*(Spec.Grip/9f);
   Body.AddForce(right*(-Mathf.Clamp(sideSpeed*grip,-maxLateral,maxLateral))*contactScale,ForceMode.Acceleration);
   float desiredYaw=steer*Spec.Steering*Mathf.Lerp(1.14f,.46f,Speed01)*Mathf.Clamp01(Mathf.Abs(forwardSpeed)/5)*Mathf.Sign(forwardSpeed)*Mathf.Lerp(1,1.26f,driftBlend);
   float currentYaw=Vector3.Dot(Body.angularVelocity,normal);
   Body.AddTorque(normal*Mathf.Clamp((desiredYaw-currentYaw)*9,-9,9)*contactScale,ForceMode.Acceleration);
   Vector3 tilt=Vector3.Cross(up,normal),angularFlat=Vector3.ProjectOnPlane(Body.angularVelocity,normal);
   Body.AddTorque((tilt*31-angularFlat*7.5f)*contactScale,ForceMode.Acceleration);
   if(flatSpeed>20)Body.AddForce(-normal*Mathf.Min(6,(flatSpeed-20)*.14f),ForceMode.Acceleration);
   if(IsDrifting)DriftPoints+=dt*SpeedKph*Slip*.6f;
   if(Body.position.y>WorldBuilder.WaterLevel+3.6f&&Vector3.Dot(up,Vector3.up)>.88f&&Mathf.Abs(sideSpeed)<7&&forwardSpeed>3){safeClock+=dt;if(safeClock>1.5f){previousSafePosition=safePosition;previousSafeRotation=safeRotation;safePosition=Body.position+Vector3.up*.35f;safeRotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward,Vector3.up),Vector3.up);safeClock=0;}}
  }else{
   Vector3 angularFlat=Vector3.ProjectOnPlane(Body.angularVelocity,Vector3.up);Body.AddTorque(Vector3.Cross(up,Vector3.up)*5-angularFlat*1.5f,ForceMode.Acceleration);
   Body.AddTorque(Vector3.up*steer*.35f,ForceMode.Acceleration);
  }
  if(Vector3.Dot(up,Vector3.up)<.1f&&flatSpeed<4)recoverTimer+=dt;else recoverTimer=0;
  if(recoverTimer>3||Body.position.y<WorldBuilder.WaterLevel-.5f)ResetTo(previousSafePosition+Vector3.up*.6f,previousSafeRotation);
  float moved=Vector3.Distance(Body.position,lastPosition);if(moved<10)DistanceDrivenKm+=moved*.001f;lastPosition=Body.position;
 }
 bool SpecIsValid()=>Spec!=null&&Spec.TopSpeed>0;
 bool Probe(Vector3 p,Vector3 d,float length,out RaycastHit hit){int n=Physics.RaycastNonAlloc(p,d,hits,length,~0,QueryTriggerInteraction.Ignore);float nearest=float.MaxValue;hit=default;for(int i=0;i<n;i++){var h=hits[i];if(h.rigidbody==Body||h.collider.transform.IsChildOf(transform)||Vector3.Dot(h.normal,Vector3.up)<.25f||h.distance>=nearest)continue;hit=h;nearest=h.distance;}return nearest<float.MaxValue;}
 void CreateEffects(){
  Shader shader=Shader.Find("Universal Render Pipeline/Particles/Unlit");if(!shader)shader=Shader.Find("Universal Render Pipeline/Unlit");if(!shader)shader=Shader.Find("Sprites/Default");
  skidMaterial=new Material(shader){name="Tire rubber"};SetTransparent(skidMaterial,new Color(.06f,.055f,.045f,.58f));
  dustMaterial=new Material(shader){name="Warm tire haze"};SetTransparent(dustMaterial,new Color(.78f,.72f,.59f,.55f));
  dustTexture=new Texture2D(32,32,TextureFormat.RGBA32,false){name="Soft tire haze",wrapMode=TextureWrapMode.Clamp};var pixels=new Color[1024];for(int y=0;y<32;y++)for(int x=0;x<32;x++){float r=new Vector2((x-15.5f)/15.5f,(y-15.5f)/15.5f).magnitude;pixels[y*32+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-r*r),2));}dustTexture.SetPixels(pixels);dustTexture.Apply(false,true);if(dustMaterial.HasProperty("_BaseMap"))dustMaterial.SetTexture("_BaseMap",dustTexture);if(dustMaterial.HasProperty("_MainTex"))dustMaterial.SetTexture("_MainTex",dustTexture);
  for(int i=0;i<2;i++){
   var skid=new GameObject("Rear tire trace "+i);skid.transform.SetParent(transform,false);var tr=skid.AddComponent<TrailRenderer>();tr.sharedMaterial=skidMaterial;tr.time=3.5f;tr.minVertexDistance=.34f;tr.widthMultiplier=.21f;tr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;tr.receiveShadows=false;tr.alignment=LineAlignment.TransformZ;tr.textureMode=LineTextureMode.Tile;tr.emitting=false;tr.numCornerVertices=2;tr.startColor=new Color(.08f,.07f,.06f,.8f);tr.endColor=new Color(.08f,.07f,.06f,0);trails[i]=tr;
   var smoke=new GameObject("Rear tire haze "+i);smoke.transform.SetParent(transform,false);smoke.transform.localPosition=new Vector3(i==0?-.83f:.83f,.2f,-1.35f);smoke.transform.localRotation=Quaternion.Euler(-90,0,0);var ps=smoke.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var main=ps.main;main.duration=3;main.loop=true;main.startLifetime=new ParticleSystem.MinMaxCurve(.45f,.8f);main.startSpeed=new ParticleSystem.MinMaxCurve(.2f,1.1f);main.startSize=new ParticleSystem.MinMaxCurve(.22f,.46f);main.startColor=new Color(.84f,.80f,.69f,.2f);main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=70;main.gravityModifier=-.06f;main.playOnAwake=false;
   var em=ps.emission;em.rateOverTime=0;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=26;shape.radius=.13f;
   var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.35f,1,2.2f));
   var col=ps.colorOverLifetime;col.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.65f,0),new GradientAlphaKey(0,1)});col.color=gradient;
   var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=dustMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;ps.Play();dust[i]=ps;
  }
 }
 static void SetTransparent(Material m,Color color){if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);if(m.HasProperty("_Color"))m.SetColor("_Color",color);m.SetFloat("_Surface",1);m.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3000;}
 void UpdateEffects(float dt){effectsCooldown=Mathf.Max(0,effectsCooldown-dt);for(int i=0;i<2;i++){
  bool active=Grounded&&touching[i+2]&&effectsCooldown<=0&&SpeedKph>28&&(IsDrifting||brake>.6f||handbrake);
  if(trails[i]){trails[i].transform.position=touching[i+2]?contacts[i+2]+normal*.024f:transform.TransformPoint(mounts[i+2]-Vector3.up*.8f);trails[i].transform.rotation=Quaternion.LookRotation(normal,transform.forward);trails[i].emitting=active;}
  if(dust[i]){var emission=dust[i].emission;emission.rateOverTime=active?Mathf.Lerp(6,24,Slip):0;}
 }}
 void OnDestroy(){if(bodyMaterial)Destroy(bodyMaterial);if(skidMaterial)Destroy(skidMaterial);if(dustMaterial)Destroy(dustMaterial);if(dustTexture)Destroy(dustTexture);}
}
}
