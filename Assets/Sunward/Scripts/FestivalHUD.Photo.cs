using System;
using TMPro;
using UnityEngine;

namespace Sunward {
public sealed partial class FestivalHUD {
 GameObject photoPanel,photoControls,photoGrid;TextMeshProUGUI photoStatus,photoModeLabel,photoBlurLabel,photoLookLabel,photoQualityLabel,photoGridLabel,photoHeader;
 UnityEngine.UI.Button photoCapture;readonly UnityEngine.UI.Slider[] photoSliders=new UnityEngine.UI.Slider[5];readonly TextMeshProUGUI[] photoValues=new TextMeshProUGUI[5];float photoTick;
 public void SetPhotoCanvas(bool visible)=>canvas.enabled=visible;
 void BuildPhoto(){
  var f=S.Game.Photo;photoPanel=Fill(menuStage,"Photo studio").gameObject;menuRoots.Add(photoPanel);photoControls=Fill(photoPanel.transform,"Camera controls").gameObject;var p=photoControls.transform;
  var badge=Box(p,"Photo identity",40,38,392,110,Panel,true);Label(badge.transform,"SUNWARD / PHOTO STUDIO",20,13,350,Yellow);Text(badge.transform,"I WAS HERE.",17,43,356,56,37,Cream,FontStyles.Bold);photoHeader=Label(p,"",42,164,760,Cream);
  Button(p,"EXIT / P / ESC",1655,40,225,56,()=>f.Exit());
  var tools=Box(p,"Camera settings",1505,154,375,721,Panel,true);tools.raycastTarget=true;Label(tools.transform,"FIND YOUR ANGLE",20,17,338,Yellow);
  photoModeLabel=Button(tools.transform,"",20,58,335,45,()=>f.ToggleOrbit()).GetComponentInChildren<TextMeshProUGUI>();
  PhotoSlider(tools.transform,0,"LENS",124,18,120,()=>f.Lens,f.SetLens);PhotoSlider(tools.transform,1,"CAMERA TILT",202,-90,90,()=>f.Roll,f.SetRoll);
  PhotoSlider(tools.transform,2,"FOCUS DISTANCE",280,0,1,()=>Mathf.InverseLerp(Mathf.Log(.3f),Mathf.Log(300),Mathf.Log(f.Focus)),v=>f.SetFocus(Mathf.Exp(Mathf.Lerp(Mathf.Log(.3f),Mathf.Log(300),v))));
  PhotoSlider(tools.transform,3,"APERTURE",358,1,16,()=>f.Aperture,f.SetAperture);PhotoSlider(tools.transform,4,"EXPOSURE",436,-2,2,()=>f.Exposure,f.SetExposure);
  photoBlurLabel=Button(tools.transform,"",20,514,164,44,()=>f.ToggleBlur()).GetComponentInChildren<TextMeshProUGUI>();Button(tools.transform,"FOCUS CAR / F",193,514,162,44,()=>f.FocusOnCar()).GetComponentInChildren<TextMeshProUGUI>().fontSize=14;
  photoLookLabel=Button(tools.transform,"",20,574,335,44,()=>f.CycleLook()).GetComponentInChildren<TextMeshProUGUI>();
  photoQualityLabel=Button(tools.transform,"",20,634,164,44,()=>f.CycleResolution()).GetComponentInChildren<TextMeshProUGUI>();photoGridLabel=Button(tools.transform,"",193,634,162,44,()=>f.ToggleGrid()).GetComponentInChildren<TextMeshProUGUI>();Text(tools.transform,"R  RESET CAMERA     O  ORBIT / FREE",21,692,332,24,14,Muted,FontStyles.Bold);
  var guide=Box(p,"Photo controls",40,784,910,100,Panel,true);Text(guide.transform,"WASD  MOVE     Q / E  HEIGHT     HOLD RIGHT MOUSE  LOOK",20,15,875,28,19,Cream,FontStyles.Bold);Text(guide.transform,"SCROLL  LENS     Z / X  TILT     SHIFT  FAST     ALT  PRECISE     H  HIDE UI",20,51,875,29,17,Muted,FontStyles.Bold);
  photoStatus=Text(p,"",42,911,1438,39,22,Cream,FontStyles.Bold);photoCapture=Button(p,"TAKE PHOTO / SPACE",40,974,371,66,()=>f.Capture(),true);Button(p,"HIDE CONTROLS / H",434,974,314,66,()=>f.ToggleInterface());Button(p,"OPEN PHOTOS",772,974,270,66,()=>f.OpenFolder());Text(p,"PNG / full frame / no overlays",1070,993,490,32,19,Muted);
  photoGrid=Fill(menus,"Photo thirds grid").gameObject;photoGrid.transform.SetAsFirstSibling();for(int i=1;i<=2;i++){
   var v=Fill(photoGrid.transform,"Vertical third");v.anchorMin=new Vector2(i/3f,0);v.anchorMax=new Vector2(i/3f,1);v.offsetMin=new Vector2(-.6f,0);v.offsetMax=new Vector2(.6f,0);Image(v,new Color(1,1,1,.22f));
   var h=Fill(photoGrid.transform,"Horizontal third");h.anchorMin=new Vector2(0,i/3f);h.anchorMax=new Vector2(1,i/3f);h.offsetMin=new Vector2(0,-.6f);h.offsetMax=new Vector2(0,.6f);Image(h,new Color(1,1,1,.22f));
  }photoGrid.SetActive(false);
 }
 void PhotoSlider(Transform p,int index,string title,float y,float min,float max,Func<float> read,Action<float> write){
  Label(p,title,21,y,213,Cream);photoValues[index]=Text(p,"",226,y,130,28,20,Yellow,FontStyles.Bold,TextAlignmentOptions.TopRight);
  var root=Rect(p,title+" photo slider",22,y+28,330,32);var hit=Image(Fill(root,"Slider hit area"),new Color(1,1,1,.001f));hit.raycastTarget=true;Box(root,"Track",0,14,330,4,new Color(.26f,.31f,.30f));
  var fill=Image(Fill(Rect(root,"Fill area",0,14,330,4),"Level"),Yellow);var handle=Image(Fill(Rect(root,"Handle area",10,6,310,20),"Handle"),Cream);handle.sprite=dotSprite;handle.rectTransform.sizeDelta=new Vector2(20,0);
  var slider=root.gameObject.AddComponent<UnityEngine.UI.Slider>();slider.minValue=min;slider.maxValue=max;slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.SetValueWithoutNotify(read());slider.onValueChanged.AddListener(v=>write(v));photoSliders[index]=slider;
 }
 void RefreshPhoto(){
  if(!photoPanel||!S.Game.Photo)return;var f=S.Game.Photo;photoGrid.SetActive(f.Active&&f.Grid&&f.InterfaceVisible);photoControls.SetActive(f.InterfaceVisible);if(!f.Active||Time.unscaledTime<photoTick)return;photoTick=Time.unscaledTime+.06f;
  photoStatus.text=f.Message;photoHeader.text="WORLD PAUSED  /  "+(f.Orbit?"ORBIT CAMERA":"FREE CAMERA")+"  /  "+f.OutputSize.x+" × "+f.OutputSize.y;photoModeLabel.text=f.Orbit?"ORBIT CAMERA / O":"FREE CAMERA / O";photoBlurLabel.text=f.Blur?"BLUR / ON":"BLUR / OFF";photoBlurLabel.fontSize=17;photoLookLabel.text="LOOK / "+PhotoMode.Looks[f.Look];photoQualityLabel.text=f.Resolution==1?"4K PNG":"HD PNG";photoGridLabel.text=f.Grid?"GRID / ON":"GRID / OFF";photoGridLabel.fontSize=17;
  float[] values={f.Lens,f.Roll,Mathf.InverseLerp(Mathf.Log(.3f),Mathf.Log(300),Mathf.Log(f.Focus)),f.Aperture,f.Exposure};string[] labels={f.Lens.ToString("0")+" mm",f.Roll.ToString("0.0")+"°",f.Focus.ToString("0.0")+" m","f/"+f.Aperture.ToString("0.0"),f.Exposure.ToString("+0.0;-0.0;0.0")+" EV"};for(int i=0;i<5;i++){photoSliders[i].SetValueWithoutNotify(values[i]);photoValues[i].text=labels[i];photoSliders[i].interactable=!f.Busy&&(i!=2&&i!=3||f.Blur);}photoCapture.interactable=!f.Busy;
 }
}
}
