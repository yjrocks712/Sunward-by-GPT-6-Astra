using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Sunward {
[DefaultExecutionOrder(-100)] public sealed class SunwardGame:MonoBehaviour {
 public static SunwardGame Instance; public WorldBuilder World;public ArcadeCar Player;public DrivingCamera CameraRig;public TrafficSystem Traffic;public FestivalSession Session;public int CurrentCarIndex;public bool Ready;public SunwardRadio Radio;public PhotoMode Photo;
 Transform modelRoot;public Camera ViewCamera;
 IEnumerator Start(){Instance=this;Time.timeScale=1;Application.runInBackground=true;Application.targetFrameRate=90;QualitySettings.vSyncCount=1;Time.fixedDeltaTime=1f/60;Time.maximumDeltaTime=.1f;Physics.defaultSolverIterations=8;Physics.defaultSolverVelocityIterations=3;Atmosphere();yield return null;
  World=new GameObject("AUREL COAST • world").AddComponent<WorldBuilder>();World.Generate();
  var car=new GameObject("Sunward • player");car.transform.SetPositionAndRotation(World.SpawnPosition,World.SpawnRotation);Player=car.AddComponent<ArcadeCar>();ChangeCar(0,CarSpec.All[0].Paint);
  var cam=new GameObject("Driving camera",typeof(Camera),typeof(AudioListener));cam.tag="MainCamera";ViewCamera=cam.GetComponent<Camera>();ViewCamera.nearClipPlane=.12f;ViewCamera.farClipPlane=4200;ViewCamera.fieldOfView=65;ViewCamera.allowHDR=true;var data=ViewCamera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
  CameraRig=cam.AddComponent<DrivingCamera>();CameraRig.Initialize(Player);CameraRig.Snap();
  Traffic=new GameObject("Coast traffic").AddComponent<TrafficSystem>();Traffic.Initialize(World,Player);
  Radio=gameObject.AddComponent<SunwardRadio>();Radio.Initialize();gameObject.AddComponent<SunwardAudio>().Initialize(this);Photo=gameObject.AddComponent<PhotoMode>();Photo.Initialize(this);Session=gameObject.AddComponent<FestivalSession>();Session.Initialize(this);Ready=true;
 }
 public void ChangeCar(int index,Color paint,float tune=0){CurrentCarIndex=Mathf.Clamp(index,0,CarSpec.All.Length-1);var src=CarSpec.All[CurrentCarIndex];var spec=new CarSpec{Id=src.Id,Name=src.Name,Category=src.Category,Description=src.Description,Price=src.Price,Style=src.Style,Mass=src.Mass,Power=src.Power*(1+tune*.08f),TopSpeed=src.TopSpeed*(1+tune*.045f),Grip=src.Grip*(1-tune*.11f),Steering=src.Steering,Paint=paint};if(modelRoot)Destroy(modelRoot.gameObject);modelRoot=new GameObject("Coachwork • "+spec.Name).transform;modelRoot.SetParent(Player.transform,false);var model=VehicleArt.Build(modelRoot,spec,paint);if(Player.Body==null)Player.Initialize(spec,model);else Player.SetSpec(spec,model);}
 void Atmosphere(){RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.00037f;RenderSettings.fogColor=new Color(.68f,.77f,.78f);RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.58f,.70f,.80f);RenderSettings.ambientEquatorColor=new Color(.66f,.61f,.49f);RenderSettings.ambientGroundColor=new Color(.25f,.28f,.28f);RenderSettings.ambientIntensity=1;RenderSettings.reflectionIntensity=.85f;
  var sun=new GameObject("Golden coast sunlight").AddComponent<Light>();sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(32,-38,0);sun.color=new Color(1,.95f,.85f);sun.intensity=1.65f;sun.shadows=LightShadows.Soft;sun.shadowStrength=.78f;sun.shadowBias=.035f;sun.shadowNormalBias=.25f;RenderSettings.sun=sun;
  var shader=Shader.Find("Sunward/CoastalSky");if(shader){var sky=new Material(shader);sky.SetVector("_SunDirection",-sun.transform.forward);RenderSettings.skybox=sky;}
  var volume=new GameObject("Sunward • color grade").AddComponent<Volume>();volume.isGlobal=true;volume.priority=1;var profile=ScriptableObject.CreateInstance<VolumeProfile>();volume.profile=profile;var tone=profile.Add<Tonemapping>();tone.mode.Override(TonemappingMode.ACES);var bloom=profile.Add<Bloom>();bloom.intensity.Override(.25f);bloom.threshold.Override(1.1f);bloom.scatter.Override(.62f);var color=profile.Add<ColorAdjustments>();color.postExposure.Override(.1f);color.contrast.Override(9);color.saturation.Override(8);var vignette=profile.Add<Vignette>();vignette.intensity.Override(.19f);vignette.smoothness.Override(.55f);ApplyAmbient();
 }
 public static void ApplyAmbient(){
  RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.60f,.70f,.80f);var sh=new SphericalHarmonicsL2();sh.AddAmbientLight(new Color(.49f,.59f,.69f));sh.AddDirectionalLight(new Vector3(.3f,1,-.2f).normalized,new Color(.57f,.64f,.70f),.35f);RenderSettings.ambientProbe=sh;
  const int size=64;var cube=new Cubemap(size,TextureFormat.RGBAHalf,true){name="Sunward • coastal environment",filterMode=FilterMode.Trilinear};
  for(int face=0;face<6;face++){var pixels=new Color[size*size];for(int y=0;y<size;y++)for(int x=0;x<size;x++){float u=(x+.5f)/size*2-1,v=(y+.5f)/size*2-1;Vector3 d=face switch{0=>new Vector3(1,-v,-u),1=>new Vector3(-1,-v,u),2=>new Vector3(u,1,v),3=>new Vector3(u,-1,-v),4=>new Vector3(u,-v,1),_=>new Vector3(-u,-v,-1)};d.Normalize();Color c=d.y<0?Color.Lerp(new Color(.81f,.77f,.63f),new Color(.20f,.24f,.22f),Mathf.Pow(-d.y,.4f)):Color.Lerp(new Color(.87f,.92f,.95f),new Color(.24f,.52f,.81f),Mathf.Pow(d.y,.55f));float l=Mathf.Pow(Mathf.Max(0,Vector3.Dot(d,new Vector3(-.4f,.45f,-.5f).normalized)),32);pixels[y*size+x]=c*(1.05f+l*.6f);}cube.SetPixels(pixels,(CubemapFace)face);}cube.Apply(true,false);RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;RenderSettings.customReflectionTexture=cube;RenderSettings.reflectionIntensity=1;
 }

 void OnDestroy(){Time.timeScale=1;if(Instance==this)Instance=null;}
}
}
