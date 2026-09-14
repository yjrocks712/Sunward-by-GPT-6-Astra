using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Sunward {
public sealed class PhotoMode:MonoBehaviour {
 public bool Active {get;private set;} public bool Busy {get;private set;}
 public bool Orbit {get;private set;}=true;public bool InterfaceVisible {get;private set;}=true;public bool Grid {get;private set;}
 public bool Blur {get;private set;}=true;public float Lens {get;private set;}=50;public float Roll {get;private set;}
 public float Focus {get;private set;}=8;public float Aperture {get;private set;}=2;public float Exposure {get;private set;}
 public int Look {get;private set;} public int Resolution {get;private set;}=1;
 public string LastPhoto {get;private set;}="";public string Message {get;private set;}="MAKE THIS MOMENT YOURS.";
 public static readonly string[] Looks={"COASTAL","AMBER","CHROME","MONO"};
 public string DirectoryPath {get;set;}
 public Vector2Int OutputSize {get {int edge=Resolution==0?1920:3840;float aspect=cameraLens?cameraLens.aspect:16f/9;return aspect>=1?new Vector2Int(edge,Mathf.Max(2,Mathf.RoundToInt(edge/aspect/2)*2)):new Vector2Int(Mathf.Max(2,Mathf.RoundToInt(edge*aspect/2)*2),edge);}}
 SunwardGame game;Camera cameraLens;Volume volume;VolumeProfile profile;DepthOfField dof;ColorAdjustments grade;WhiteBalance white;Vignette vignette;
 FestivalMenu oldMenu;Vector3 oldPosition;Quaternion oldRotation;float oldFov,oldNear,oldTime,yaw,pitch;bool oldRig,oldPlayer,oldInput,oldNavigation,oldCursor;CursorLockMode oldLock;
 Vector3 Target=>game.Player.transform.position+Vector3.up*.85f;
 public void Initialize(SunwardGame g){
  game=g;cameraLens=g.ViewCamera;DirectoryPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),"Sunward");
  if(!Path.IsPathRooted(DirectoryPath))DirectoryPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal),"Pictures","Sunward");
  var root=new GameObject("Sunward photo grade");root.transform.SetParent(transform,false);volume=root.AddComponent<Volume>();volume.isGlobal=true;volume.priority=100;volume.weight=0;
  volume.sharedProfile=Resources.Load<VolumeProfile>("PhotoProfile");profile=volume.profile;
  if(!profile.TryGet(out dof))dof=profile.Add<DepthOfField>();if(!profile.TryGet(out grade))grade=profile.Add<ColorAdjustments>();if(!profile.TryGet(out white))white=profile.Add<WhiteBalance>();if(!profile.TryGet(out vignette))vignette=profile.Add<Vignette>();
 }
 public void Enter(){
  if(Active||Busy||!game.Ready)return;oldMenu=game.Session.Menu;oldTime=Time.timeScale;oldInput=game.Player.InputEnabled;oldPlayer=game.Player.enabled;oldRig=game.CameraRig.enabled;
  oldPosition=cameraLens.transform.position;oldRotation=cameraLens.transform.rotation;oldFov=cameraLens.fieldOfView;oldNear=cameraLens.nearClipPlane;oldCursor=Cursor.visible;oldLock=Cursor.lockState;
  oldNavigation=game.Session.PhotoNavigation&&game.Session.PhotoNavigation.activeSelf;if(game.Session.PhotoNavigation)game.Session.PhotoNavigation.SetActive(false);
  Active=true;Time.timeScale=0;game.Player.enabled=false;game.CameraRig.enabled=false;cameraLens.nearClipPlane=.06f;volume.weight=1;InterfaceVisible=true;game.Session.SetPhotoMenu(FestivalMenu.Photo);ResetCamera();ApplyGrade();Message="MAKE THIS MOMENT YOURS.";
 }
 public void Exit(){
  if(!Active||Busy)return;Active=false;volume.weight=0;game.Session.HUD.SetPhotoCanvas(true);cameraLens.transform.SetPositionAndRotation(oldPosition,oldRotation);cameraLens.fieldOfView=oldFov;cameraLens.nearClipPlane=oldNear;
  game.CameraRig.enabled=oldRig;game.Player.enabled=oldPlayer;if(game.Session.PhotoNavigation)game.Session.PhotoNavigation.SetActive(oldNavigation);Time.timeScale=oldTime;game.Session.SetPhotoMenu(oldMenu);game.Player.InputEnabled=oldInput;Cursor.lockState=oldLock;Cursor.visible=oldCursor;
 }
 public void ResetCamera(){if(!Active||Busy)return;Lens=50;Roll=0;Orbit=true;cameraLens.transform.position=game.Player.transform.TransformPoint(new Vector3(5.4f,2.25f,6.6f));AimAtCar();SetLens(Lens);Message="CAMERA RESET";}
 void ReadAngles(){var a=cameraLens.transform.eulerAngles;yaw=a.y;pitch=Mathf.DeltaAngle(0,a.x);}
 void AimAtCar(){cameraLens.transform.rotation=Quaternion.LookRotation(Target-cameraLens.transform.position)*Quaternion.Euler(0,0,Roll);ReadAngles();FocusOnCar();}
 public void FocusOnCar(){if(!Active||Busy)return;SetFocus(Mathf.Max(.3f,Vector3.Dot(Target-cameraLens.transform.position,cameraLens.transform.forward)));Message="FOCUS / "+Focus.ToString("0.0")+" M";}
 public void SetLens(float v){Lens=Mathf.Clamp(v,18,120);if(Active)cameraLens.fieldOfView=2*Mathf.Atan(12/Lens)*Mathf.Rad2Deg;ApplyGrade();}
 public void SetRoll(float v){Roll=Mathf.Clamp(v,-90,90);if(Active)cameraLens.transform.rotation=Quaternion.Euler(pitch,yaw,0)*Quaternion.Euler(0,0,Roll);}
 public void SetFocus(float v){Focus=Mathf.Clamp(v,.3f,300);ApplyGrade();}
 public void SetAperture(float v){Aperture=Mathf.Clamp(v,1,16);ApplyGrade();}
 public void SetExposure(float v){Exposure=Mathf.Clamp(v,-2,2);ApplyGrade();}
 public void ToggleBlur(){Blur=!Blur;ApplyGrade();}
 public void CycleLook(){Look=(Look+1)%Looks.Length;ApplyGrade();}
 public void ToggleOrbit(){Orbit=!Orbit;if(Orbit)AimAtCar();Message=Orbit?"ORBIT / CAR LOCKED":"FREE FLIGHT";}
 public void ToggleGrid()=>Grid=!Grid;
 public void ToggleInterface(){InterfaceVisible=!InterfaceVisible;Cursor.lockState=CursorLockMode.None;Cursor.visible=InterfaceVisible;}
 public void CycleResolution()=>Resolution=1-Resolution;
 public void OpenFolder(){try{Directory.CreateDirectory(DirectoryPath);Application.OpenURL(new Uri(DirectoryPath+Path.DirectorySeparatorChar).AbsoluteUri);}catch(Exception e){Message="FOLDER UNAVAILABLE: "+e.Message;}}
 void ApplyGrade(){if(!profile)return;dof.mode.Override(Blur?DepthOfFieldMode.Bokeh:DepthOfFieldMode.Off);dof.focusDistance.Override(Focus);dof.focalLength.Override(Lens);dof.aperture.Override(Aperture);dof.bladeCount.Override(7);grade.postExposure.Override(.1f+Exposure);grade.contrast.Override(Look==1?14:Look==2?18:Look==3?24:9);grade.saturation.Override(Look==1?3:Look==2?-22:Look==3?-100:8);white.temperature.Override(Look==1?21:Look==2?-14:0);white.tint.Override(0);vignette.intensity.Override(Look==3?.27f:.19f);vignette.smoothness.Override(.55f);}
 public void MoveCamera(Vector3 local,float seconds,float multiplier=1){
  if(!Active||Busy)return;var t=cameraLens.transform;Vector3 move=t.right*local.x+t.forward*local.z+Vector3.up*local.y;Vector3 next=t.position+Vector3.ClampMagnitude(move,1)*seconds*7*multiplier;
  Vector3 delta=Vector3.ClampMagnitude(next-Target,200);if(Orbit&&delta.magnitude<1.2f)delta=delta.normalized*1.2f;next=Target+delta;next.y=Mathf.Max(next.y,game.World.GroundHeight(next.x,next.z)+.12f);t.position=next;if(Orbit)AimAtCar();
 }
 public void RotateCamera(Vector2 delta){if(!Active||Busy)return;yaw+=delta.x;pitch=Mathf.Clamp(pitch-delta.y,-88,88);var rotation=Quaternion.Euler(pitch,yaw,0);if(Orbit){float distance=Mathf.Max(1.2f,Vector3.Distance(cameraLens.transform.position,Target));var p=Target-rotation*Vector3.forward*distance;p.y=Mathf.Max(p.y,game.World.GroundHeight(p.x,p.z)+.12f);cameraLens.transform.position=p;cameraLens.transform.rotation=Quaternion.LookRotation(Target-p)*Quaternion.Euler(0,0,Roll);ReadAngles();FocusOnCar();}else cameraLens.transform.rotation=rotation*Quaternion.Euler(0,0,Roll);}
 void Update(){
  if(!Active||Busy)return;float dt=Mathf.Min(.05f,Time.unscaledDeltaTime);var mouse=Mouse.current;var pad=Gamepad.current;bool over=InterfaceVisible&&EventSystem.current&&EventSystem.current.IsPointerOverGameObject();
  if(DriveInput.Press(Key.H)||(pad?.buttonNorth.wasPressedThisFrame??false))ToggleInterface();if(DriveInput.Press(Key.G))ToggleGrid();if(DriveInput.Press(Key.O)||(pad?.leftStickButton.wasPressedThisFrame??false))ToggleOrbit();if(DriveInput.Press(Key.F)||(pad?.rightStickButton.wasPressedThisFrame??false))FocusOnCar();if(DriveInput.Press(Key.R))ResetCamera();
  if(DriveInput.Press(Key.Space)||(pad?.buttonSouth.wasPressedThisFrame??false)){Capture();return;}
  Vector3 movement=new((DriveInput.Held(Key.D)?1:0)-(DriveInput.Held(Key.A)?1:0),(DriveInput.Held(Key.E)?1:0)-(DriveInput.Held(Key.Q)?1:0),(DriveInput.Held(Key.W)?1:0)-(DriveInput.Held(Key.S)?1:0));
  if(pad!=null){Vector2 stick=pad.leftStick.ReadValue();movement+=new Vector3(stick.x,pad.rightTrigger.ReadValue()-pad.leftTrigger.ReadValue(),stick.y);RotateCamera(pad.rightStick.ReadValue()*dt*95);}
  if(movement.sqrMagnitude>0)MoveCamera(movement,dt,DriveInput.Held(Key.LeftShift)?3:DriveInput.Held(Key.LeftAlt)?.2f:1);
  bool looking=mouse!=null&&mouse.rightButton.isPressed&&!over;if(looking)RotateCamera(mouse.delta.ReadValue()*.16f);Cursor.lockState=looking?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!looking&&InterfaceVisible;
  if(mouse!=null&&!over){float scroll=mouse.scroll.ReadValue().y;if(scroll!=0)SetLens(Lens+scroll*.025f);if(mouse.leftButton.wasPressedThisFrame&&Physics.Raycast(cameraLens.ScreenPointToRay(mouse.position.ReadValue()),out var hit,600,~0,QueryTriggerInteraction.Ignore)){SetFocus(Vector3.Dot(hit.point-cameraLens.transform.position,cameraLens.transform.forward));Message="FOCUS / "+Focus.ToString("0.0")+" M";}}
  float roll=(DriveInput.Held(Key.X)?1:0)-(DriveInput.Held(Key.Z)?1:0);if(roll!=0)SetRoll(Roll+roll*dt*25);
 }
 public void Capture(){if(!Active||Busy)return;Busy=true;Message="DEVELOPING YOUR PHOTO...";StartCoroutine(SavePhoto());}
 IEnumerator SavePhoto(){
  yield return null;byte[] bytes=null;string path=null;RenderTexture rt=null;Texture2D image=null;var oldTarget=RenderTexture.active;var oldRect=cameraLens.rect;var oldCameraTarget=cameraLens.targetTexture;game.Session.HUD.SetPhotoCanvas(false);
  try{
   Directory.CreateDirectory(DirectoryPath);var size=OutputSize;path=Path.Combine(DirectoryPath,"Sunward-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")+"-"+Guid.NewGuid().ToString("N").Substring(0,4)+".png");rt=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){antiAliasing=1,name="Sunward photo output"};rt.Create();cameraLens.rect=new Rect(0,0,1,1);RenderPipeline.SubmitRenderRequest(cameraLens,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false,false);image.ReadPixels(new Rect(0,0,size.x,size.y),0,0,false);image.Apply(false,false);bytes=image.EncodeToPNG();
  }catch(Exception e){Message="PHOTO COULD NOT BE SAVED: "+e.Message;}
  finally{cameraLens.targetTexture=oldCameraTarget;cameraLens.rect=oldRect;RenderTexture.active=oldTarget;if(rt){rt.Release();Destroy(rt);}if(image)Destroy(image);game.Session.HUD.SetPhotoCanvas(true);}
  if(bytes!=null){var data=bytes;var output=path;var save=Task.Run(()=>{string temp=output+".tmp";try{File.WriteAllBytes(temp,data);File.Move(temp,output);}finally{if(File.Exists(temp))File.Delete(temp);}});while(!save.IsCompleted)yield return null;if(save.IsFaulted)Message="PHOTO COULD NOT BE SAVED: "+save.Exception.GetBaseException().Message;else{LastPhoto=path;Message="SAVED / "+Path.GetFileName(path);SunwardAudio.Ping(4);}}
  Busy=false;
 }
 void OnApplicationFocus(bool focused){if(!focused&&Active){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}}
 void OnDestroy(){if(Active){Busy=false;if(game&&game.Player&&game.CameraRig&&game.Session&&game.Session.HUD&&cameraLens&&volume)Exit();else{Active=false;Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}}if(profile){foreach(var component in profile.components)if(component)Destroy(component);Destroy(profile);}if(volume)Destroy(volume.gameObject);}
}
}
