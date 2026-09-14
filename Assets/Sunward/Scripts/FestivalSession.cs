using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sunward {
public enum FestivalMenu { None,Events,Garage,Map,Pause,Results,Radio,Photo }
public enum FestivalRace { Cruise,Countdown,Racing,Finished }
public sealed class FestivalSession:MonoBehaviour {
 public SunwardGame Game {get;private set;} public FestivalHUD HUD {get;private set;}
 public FestivalMenu Menu {get;private set;} public FestivalRace State {get;private set;}
 public int Credits {get;private set;} public int SelectedCar {get;private set;} public int PreviewCar {get;private set;}
 public int PreviewPaint {get;private set;} public int PreviewTune {get;private set;}
 public int EventIndex {get;private set;} public int GateIndex {get;private set;} public int GateCount {get;private set;}
 public bool RaceActive=>State==FestivalRace.Racing||State==FestivalRace.Countdown; public float RaceTime {get;private set;} public float Countdown {get;private set;} public int RacePlace {get;private set;}=1;
 public float RaceDistance {get;private set;} public float DriftChain {get;private set;} public float ChainMultiplier=>Mathf.Min(5,1+Mathf.Floor(DriftChain/500));
 public int ResultReward {get;private set;} public string ResultMedal {get;private set;} public bool PersonalBest {get;private set;}
 public string Notice {get;private set;}=""; public string NoticeDetail {get;private set;}=""; public float NoticeUntil {get;private set;}
 public string ContextHint {get;private set;}=""; public string Region {get;private set;}="SUNWARD COAST";
 public RoadPath ActiveRoute=>Game.World.Roads[Mathf.Clamp(EventIndex,0,Game.World.Roads.Count-1)];
 public Vector3 NextCheckpoint=>ActiveRoute.Position((GateIndex+1)*gateSpacing);
 public int Discoveries=>discovered.Count; public int TotalDiscoveries=>Game.World.Landmarks.Count;
 public float WelcomeUntil {get;private set;} public bool WrongWay {get;private set;} public bool MissedGate {get;private set;}
 public float GoldTime(int index)=>Game.World.Roads[index].Length/28f+12f;
 public float BestTime(int index)=>PlayerPrefs.GetFloat(SaveKey("best."+index),0);
 public bool Owned(int index)=>index==0||PlayerPrefs.GetInt(SaveKey("own."+index),0)==1;
 public bool Discovered(int index)=>discovered.Contains(index);
 public static readonly Color[] Paints={new(.96f,.28f,.12f),new(.98f,.83f,.27f),new(.16f,.70f,.65f),new(.51f,.44f,.87f),new(.93f,.91f,.83f),new(.14f,.20f,.27f)};
 public static readonly string[] PaintNames={"EMBER","SOLAR","TIDAL","AFTERGLOW","PORCELAIN","MIDNIGHT"};
 public static readonly string[] TuneNames={"GRIP","STREET","RUSH"};
 readonly HashSet<int> discovered=new(); readonly List<GameObject> ownedObjects=new();
 float gateSpacing,discoveryTick,driftSettle,lastDrift,lastDistance,tripDistance,speedCooldown,wrongTimer,missedTimer;
 Vector3 previousPosition; GameObject gateRoot; LineRenderer ribbon; Material markerMaterial,ribbonMaterial; Transform[] portalRoots; int nearestPortal=-1; bool initialized;
 static string SaveKey(string suffix)=>"Sunward.v1."+suffix;
 public void Initialize(SunwardGame game){
  Game=game;Credits=PlayerPrefs.GetInt(SaveKey("credits"),1600);SelectedCar=Mathf.Clamp(PlayerPrefs.GetInt(SaveKey("selected"),0),0,CarSpec.All.Length-1);if(!Owned(SelectedCar))SelectedCar=0;
  for(int i=0;i<TotalDiscoveries;i++)if(PlayerPrefs.GetInt(SaveKey("found."+i),0)==1)discovered.Add(i);
  ApplySavedCar();PreviewCar=SelectedCar;State=FestivalRace.Cruise;Menu=FestivalMenu.None;WelcomeUntil=Time.unscaledTime+15;
  BuildMarkers();HUD=new GameObject("Sunward Interface").AddComponent<FestivalHUD>();HUD.Initialize(this);ownedObjects.Add(HUD.gameObject);
  previousPosition=Game.Player.transform.position;lastDrift=Game.Player.DriftPoints;lastDistance=Game.Player.DistanceDrivenKm;initialized=true;SetInput();
 }
 void Update(){if(!initialized||Game.Player==null)return;
  HandleKeys();if(Menu!=FestivalMenu.None)return;
  var car=Game.Player;Vector3 pos=car.transform.position;
  if(State==FestivalRace.Countdown){float old=Countdown;Countdown-=Time.deltaTime;if(Mathf.CeilToInt(old)!=Mathf.CeilToInt(Countdown))SunwardAudio.Ping(Mathf.CeilToInt(Countdown));if(Countdown<=0){Countdown=0;State=FestivalRace.Racing;Game.Traffic.StartRace(ActiveRoute,1);SetInput();Toast("GO. FIND YOUR FLOW.",ActiveRoute.Name,2);}previousPosition=pos;return;}
  if(State==FestivalRace.Racing){RaceTime+=Time.deltaTime;AdvanceRace(pos);}
  else UpdateContext(pos);
  if(Time.time>=discoveryTick){discoveryTick=Time.time+.5f;Discover(pos);}
  UpdateDrift();float distance=car.DistanceDrivenKm;if(distance>=lastDistance)tripDistance+=distance-lastDistance;lastDistance=distance;if(tripDistance>=1){tripDistance-=1;Award(180);Toast("THE LONG WAY HOME","1 km explored  /  +180 CR",3);}
  previousPosition=pos;
 }
 internal GameObject PhotoNavigation=>gateRoot;
 internal void SetPhotoMenu(FestivalMenu menu){Menu=menu;SetInput();HUD.RefreshMenus();}
 void HandleKeys(){
  bool photo=DriveInput.Press(Key.P)||(Gamepad.current?.leftShoulder.wasPressedThisFrame??false);
  if(Game.Photo.Active){if(photo||DriveInput.Press(Key.Escape)||(Gamepad.current?.buttonEast.wasPressedThisFrame??false))Game.Photo.Exit();return;}
  if(photo){Game.Photo.Enter();return;}
  if(DriveInput.Press(Key.Escape)){if(Menu!=FestivalMenu.None)CloseMenu();else ShowMenu(FestivalMenu.Pause);return;}
  if(DriveInput.Press(Key.B)){if(Menu==FestivalMenu.Radio)CloseMenu();else ShowMenu(FestivalMenu.Radio);return;}
  if(DriveInput.Press(Key.G)&&State==FestivalRace.Cruise){if(Menu==FestivalMenu.Garage)CloseMenu();else ShowMenu(FestivalMenu.Garage);return;}
  if(DriveInput.Press(Key.M)){if(Menu==FestivalMenu.Map)CloseMenu();else ShowMenu(FestivalMenu.Map);return;}
  if(DriveInput.Press(Key.Enter)&&Menu==FestivalMenu.None)WelcomeUntil=0;
  if(Menu==FestivalMenu.None&&State==FestivalRace.Cruise&&DriveInput.Confirm)ShowMenu(FestivalMenu.Events);
  if(Menu==FestivalMenu.None&&State==FestivalRace.Racing&&DriveInput.Press(Key.Backspace))RecoverCheckpoint();
 }
 void UpdateContext(Vector3 p){nearestPortal=-1;float best=100*100;for(int i=0;i<Game.World.Roads.Count;i++){float d=(p-Game.World.Roads[i].Position(0)).sqrMagnitude;if(d<best){best=d;nearestPortal=i;}}
  ContextHint=nearestPortal>=0?"E  ENTER "+Game.World.Roads[nearestPortal].Name.ToUpperInvariant():"E  EVENTS     G  GARAGE     M  WORLD MAP";
 }
 void Discover(Vector3 p){float closest=float.MaxValue;for(int i=0;i<TotalDiscoveries;i++){var landmark=Game.World.Landmarks[i];float d=(p-landmark.Position).sqrMagnitude;if(d<closest){closest=d;Region=landmark.Name.ToUpperInvariant();}
   if(d<125*125&&!discovered.Contains(i)){discovered.Add(i);PlayerPrefs.SetInt(SaveKey("found."+i),1);Award(350);Toast("DISCOVERED / "+landmark.Name.ToUpperInvariant(),landmark.Kind+"  /  +350 CR",4.5f);SunwardAudio.Ping(3);}
  }
  if(State==FestivalRace.Cruise&&Game.Player.SpeedKph>155&&Time.time>speedCooldown){speedCooldown=Time.time+35;int reward=Mathf.RoundToInt(Game.Player.SpeedKph*1.5f);Award(reward);Toast("VELOCITY CAPTURED",Mathf.RoundToInt(Game.Player.SpeedKph)+" KM/H  /  +"+reward+" CR",3.5f);}
 }
 void UpdateDrift(){float score=Game.Player.DriftPoints;float delta=Mathf.Max(0,score-lastDrift);lastDrift=score;if(Game.Player.IsDrifting){DriftChain+=delta;driftSettle=1.8f;}else if(DriftChain>0){driftSettle-=Time.deltaTime;if(driftSettle<=0){int reward=Mathf.Clamp(Mathf.RoundToInt(DriftChain*.14f*ChainMultiplier),20,800);Award(reward);if(DriftChain>80)Toast("STYLE BANKED",Mathf.RoundToInt(DriftChain)+" STYLE  /  +"+reward+" CR",3);DriftChain=0;}}}
 public void Toast(string title,string detail="",float seconds=3.5f){Notice=title;NoticeDetail=detail;NoticeUntil=Time.unscaledTime+seconds;}
 public void Award(int amount){Credits+=amount;PlayerPrefs.SetInt(SaveKey("credits"),Credits);PlayerPrefs.Save();}
 public void ShowMenu(FestivalMenu menu){if(Game.Photo.Active)return;if(menu==FestivalMenu.Garage&&State!=FestivalRace.Cruise)return;if(Menu==FestivalMenu.Garage&&menu!=FestivalMenu.Garage){ApplySavedCar();Game.CameraRig.Orbit=false;Game.CameraRig.Snap();}Menu=menu;Time.timeScale=0;WelcomeUntil=0;
  if(menu==FestivalMenu.Garage){PreviewCar=SelectedCar;PreviewPaint=Mathf.Clamp(PlayerPrefs.GetInt(SaveKey("paint."+PreviewCar),PreviewCar%Paints.Length),0,Paints.Length-1);PreviewTune=Mathf.Clamp(PlayerPrefs.GetInt(SaveKey("tune."+PreviewCar),0),-1,1);Game.CameraRig.Orbit=true;}
  SetInput();HUD.RefreshMenus();SunwardAudio.Ping();
 }
 public void CloseMenu(){if(Game.Photo.Active){Game.Photo.Exit();return;}bool garage=Menu==FestivalMenu.Garage;if(garage)ApplySavedCar();Game.CameraRig.Orbit=false;if(garage)Game.CameraRig.Snap();if(State==FestivalRace.Finished){State=FestivalRace.Cruise;HideGate();}Menu=FestivalMenu.None;Time.timeScale=1;SetInput();HUD.RefreshMenus();previousPosition=Game.Player.transform.position;}
 void SetInput(){Game.Player.InputEnabled=Menu==FestivalMenu.None&&State!=FestivalRace.Countdown;Cursor.lockState=CursorLockMode.None;Cursor.visible=Menu!=FestivalMenu.None;}
 public void Preview(int index){PreviewCar=Mathf.Clamp(index,0,CarSpec.All.Length-1);PreviewPaint=Mathf.Clamp(PlayerPrefs.GetInt(SaveKey("paint."+PreviewCar),PreviewCar%Paints.Length),0,Paints.Length-1);PreviewTune=Mathf.Clamp(PlayerPrefs.GetInt(SaveKey("tune."+PreviewCar),0),-1,1);ApplyPreview();HUD.RefreshGarage();SunwardAudio.Ping();}
 public void Paint(int index){PreviewPaint=Mathf.Clamp(index,0,Paints.Length-1);ApplyPreview();HUD.RefreshGarage();SunwardAudio.Ping();}
 public void Tune(int value){PreviewTune=Mathf.Clamp(value,-1,1);ApplyPreview();HUD.RefreshGarage();SunwardAudio.Ping();}
 void ApplyPreview()=>Game.ChangeCar(PreviewCar,Paints[PreviewPaint],PreviewTune);
 void ApplySavedCar(){int paint=Mathf.Clamp(PlayerPrefs.GetInt(SaveKey("paint."+SelectedCar),SelectedCar%Paints.Length),0,Paints.Length-1);Game.ChangeCar(SelectedCar,Paints[paint],Mathf.Clamp(PlayerPrefs.GetInt(SaveKey("tune."+SelectedCar),0),-1,1));}
 public void DrivePreview(){var spec=CarSpec.All[PreviewCar];if(!Owned(PreviewCar)){if(Credits<spec.Price){Toast("MORE ROAD AWAITS","Earn credits in events, discoveries and drift chains.",4);return;}Credits-=spec.Price;PlayerPrefs.SetInt(SaveKey("credits"),Credits);PlayerPrefs.SetInt(SaveKey("own."+PreviewCar),1);SunwardAudio.Celebrate();}
  SelectedCar=PreviewCar;PlayerPrefs.SetInt(SaveKey("selected"),SelectedCar);PlayerPrefs.SetInt(SaveKey("paint."+SelectedCar),PreviewPaint);PlayerPrefs.SetInt(SaveKey("tune."+SelectedCar),PreviewTune);PlayerPrefs.Save();CloseMenu();Toast("YOUR "+spec.Name,PaintNames[PreviewPaint]+"  /  "+TuneNames[PreviewTune+1]+" TUNE",3);
 }
 public void BeginRace(int roadIndex)=>StartEvent(roadIndex);
 public void StartEvent(int index){if(index<0||index>=Game.World.Roads.Count)return;Game.Traffic.StopRace();CloseMenu();EventIndex=index;GateCount=Mathf.Clamp(Mathf.CeilToInt(ActiveRoute.Length/210),8,32);gateSpacing=ActiveRoute.Length/GateCount;GateIndex=0;RaceTime=0;RaceDistance=0;RacePlace=4;Countdown=3f;State=FestivalRace.Countdown;WrongWay=MissedGate=false;wrongTimer=missedTimer=0;Game.Player.ResetTo(ActiveRoute.Position(0)+Vector3.up*.9f,ActiveRoute.Rotation(0));Game.CameraRig.Snap();previousPosition=Game.Player.transform.position;SetInput();RefreshGate();Toast("GRID READY",ActiveRoute.Name+"  /  ONE LAP  /  4 DRIVERS",3);}
 void AdvanceRace(Vector3 p){
  float target=(GateIndex+1)*gateSpacing;ActiveRoute.Sample(target,out var gate,out var forward);float before=Vector3.Dot(previousPosition-gate,forward),after=Vector3.Dot(p-gate,forward);
  float crossing=before/(before-after);Vector3 projected=Vector3.Lerp(previousPosition,p,Mathf.Clamp01(crossing))-gate;float lateral=Mathf.Abs(Vector3.Dot(projected,Vector3.Cross(Vector3.up,forward).normalized));
  if(before<=0&&after>0&&lateral<=ActiveRoute.Width*.7f+5&&Mathf.Abs(projected.y)<12&&(p-previousPosition).sqrMagnitude<10000){GateIndex++;WrongWay=MissedGate=false;wrongTimer=missedTimer=0;SunwardAudio.Ping(1);if(GateIndex>=GateCount){RaceDistance=ActiveRoute.Length;UpdatePlace();FinishRace();return;}RefreshGate();}
  float d=ActiveRoute.ClosestDistance(p);if(GateIndex==GateCount-1&&d<gateSpacing*.5f)d+=ActiveRoute.Length;RaceDistance=Mathf.Clamp(d,GateIndex*gateSpacing,(GateIndex+1)*gateSpacing);UpdatePlace();
  ActiveRoute.Sample(d,out _,out var localForward);float alignment=Vector3.Dot(Game.Player.transform.forward,localForward);wrongTimer=alignment<-.4f&&Game.Player.SpeedKph>20?wrongTimer+Time.deltaTime:Mathf.Max(0,wrongTimer-Time.deltaTime*2);WrongWay=wrongTimer>2;
  missedTimer=d>(GateIndex+1)*gateSpacing+40&&d<(GateIndex+2)*gateSpacing&&GateIndex<GateCount?missedTimer+Time.deltaTime:0;MissedGate=missedTimer>2;
  ContextHint=MissedGate?"MISSED GATE  /  BACKSPACE TO RETURN  (+7 SEC)":WrongWay?"WRONG WAY  /  TURN BACK TOWARD THE YELLOW GATE":"SPACE  DRIFT     SHIFT  BOOST     BACKSPACE  RECOVER";
 }
 void UpdatePlace(){RacePlace=1;var distances=Game.Traffic.RaceDistances;if(distances!=null)for(int i=0;i<distances.Length;i++)if(distances[i]>RaceDistance)RacePlace++;}
 public void RecoverCheckpoint(){if(State!=FestivalRace.Racing)return;float d=Mathf.Max(0,GateIndex*gateSpacing+5);Game.Player.ResetTo(ActiveRoute.Position(d)+Vector3.up*.8f,ActiveRoute.Rotation(d));previousPosition=Game.Player.transform.position;RaceTime+=7;WrongWay=MissedGate=false;missedTimer=wrongTimer=0;Game.CameraRig.Snap();Toast("BACK ON THE LINE","Recovery  /  +7.00 sec",2);}
 void FinishRace(){State=FestivalRace.Finished;Game.Traffic.StopRace();HideGate();float gold=GoldTime(EventIndex);ResultMedal=RaceTime<=gold?"GOLD":RaceTime<=gold*1.3f?"SILVER":"BRONZE";int medal=ResultMedal=="GOLD"?3:ResultMedal=="SILVER"?2:1;ResultReward=1400+medal*700+Mathf.Max(0,4-RacePlace)*250;float best=BestTime(EventIndex);PersonalBest=best<=0||RaceTime<best;if(PersonalBest)PlayerPrefs.SetFloat(SaveKey("best."+EventIndex),RaceTime);if(PlayerPrefs.GetInt(SaveKey("finished."+EventIndex),0)==0){ResultReward+=600;PlayerPrefs.SetInt(SaveKey("finished."+EventIndex),1);}Award(ResultReward);SunwardAudio.Celebrate();ShowMenu(FestivalMenu.Results);}
 public void AbandonEvent(){Game.Traffic.StopRace();State=FestivalRace.Cruise;HideGate();CloseMenu();Toast("BACK TO THE OPEN ROAD","Your next story is around the corner.");}
 public void ReturnToFestival(){if(State==FestivalRace.Countdown||State==FestivalRace.Racing)AbandonEvent();CloseMenu();Game.Player.ResetTo(Game.World.SpawnPosition,Game.World.SpawnRotation);Game.CameraRig.Snap();Toast("SUNWARD MOTOR FESTIVAL","E  events  /  G  garage  /  M  map");}
 void BuildMarkers(){markerMaterial=NewMaterial(new Color(1,.80f,.15f),true);ribbonMaterial=NewMaterial(new Color(1,.79f,.17f),false);portalRoots=new Transform[Game.World.Roads.Count];
  for(int i=0;i<portalRoots.Length;i++){var road=Game.World.Roads[i];var root=new GameObject("Event portal "+road.Name);root.transform.SetPositionAndRotation(road.Position(0),road.Rotation(0));portalRoots[i]=root.transform;ownedObjects.Add(root);float half=road.Width*.58f;
   for(int side=-1;side<=1;side+=2){Stroke(root.transform,"Festival banner",new[]{new Vector3(side*(half+1),0,0),new Vector3(side*(half+1),8,0)},.12f,markerMaterial);Stroke(root.transform,"Banner ribbon",new[]{new Vector3(side*(half+1),8,0),new Vector3(side*(half+1),4,0)},.6f,ribbonMaterial);}
  }
  gateRoot=new GameObject("Next checkpoint");ownedObjects.Add(gateRoot);ribbon=Stroke(gateRoot.transform,"Racing line",Array.Empty<Vector3>(),.15f,ribbonMaterial);ribbon.useWorldSpace=true;gateRoot.SetActive(false);
 }
 static Material NewMaterial(Color c,bool glow){var shader=Shader.Find("Universal Render Pipeline/Unlit");if(!shader)shader=Shader.Find("Sprites/Default");var mat=new Material(shader){color=c};if(mat.HasProperty("_BaseColor"))mat.SetColor("_BaseColor",glow?c*1.7f:c);return mat;}
 static LineRenderer Stroke(Transform parent,string name,Vector3[] points,float width,Material material){var line=new GameObject(name).AddComponent<LineRenderer>();line.transform.SetParent(parent,false);line.useWorldSpace=false;line.sharedMaterial=material;line.widthMultiplier=width;line.positionCount=points.Length;if(points.Length>0)line.SetPositions(points);line.numCornerVertices=2;line.numCapVertices=2;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;return line;}
 void RefreshGate(){gateRoot.SetActive(true);for(int i=gateRoot.transform.childCount-1;i>=0;i--){var t=gateRoot.transform.GetChild(i);if(t!=ribbon.transform)Destroy(t.gameObject);}ActiveRoute.Sample((GateIndex+1)*gateSpacing,out var p,out var f);gateRoot.transform.SetPositionAndRotation(p,Quaternion.LookRotation(f));float half=ActiveRoute.Width*.7f;
  Stroke(gateRoot.transform,"Gate",new[]{new Vector3(-half,.1f,0),new Vector3(-half,7,0),new Vector3(half,7,0),new Vector3(half,.1f,0)},.24f,markerMaterial);
  float start=GateIndex*gateSpacing;int count=Mathf.CeilToInt(gateSpacing/8)+1;var points=new Vector3[count];for(int i=0;i<count;i++)points[i]=ActiveRoute.Position(Mathf.Lerp(start,(GateIndex+1)*gateSpacing,i/(float)(count-1)))+Vector3.up*.2f;ribbon.positionCount=count;ribbon.SetPositions(points);
 }
 void HideGate(){if(gateRoot)gateRoot.SetActive(false);}
 public static string Clock(float t){int minutes=(int)t/60;return minutes.ToString("00")+":"+(t%60).ToString("00.00");}
 void OnDestroy(){Time.timeScale=1;Cursor.visible=true;Cursor.lockState=CursorLockMode.None;foreach(var obj in ownedObjects)if(obj)Destroy(obj);if(markerMaterial)Destroy(markerMaterial);if(ribbonMaterial)Destroy(ribbonMaterial);PlayerPrefs.Save();}
}
}
