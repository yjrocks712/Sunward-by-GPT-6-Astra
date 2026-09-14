using UnityEngine;

namespace Sunward {
public sealed class VehicleVisual : MonoBehaviour {
 public Transform[] Wheels; public Renderer[] PaintRenderers;
 [SerializeField] internal Renderer Shell;
 [SerializeField] internal Transform[] Calipers;
 [SerializeField] internal Light[] Headlights;
 [SerializeField] internal bool Simple;
 [SerializeField] Color paint=Color.white;
 [SerializeField] float roll;
 MaterialPropertyBlock paintBlock,tailBlock;
 static readonly int BaseColor=Shader.PropertyToID("_BaseColor"),Emission=Shader.PropertyToID("_EmissionColor");
 void EnsureBlocks(){paintBlock??=new MaterialPropertyBlock();tailBlock??=new MaterialPropertyBlock();}
 void OnEnable(){EnsureBlocks();if(PaintRenderers!=null)SetPaint(paint);}
 public void SetPaint(Color value){
  EnsureBlocks();paint=value;paintBlock.SetColor(BaseColor,value);
  if(PaintRenderers!=null)foreach(var r in PaintRenderers)if(r)r.SetPropertyBlock(paintBlock,0);
 }
 public void Animate(float speedMps,float steer,float brake,bool boost){
  EnsureBlocks();roll=Mathf.Repeat(roll+speedMps*Time.deltaTime/.36f*Mathf.Rad2Deg,360);
  if(Wheels!=null)for(int i=0;i<Wheels.Length;i++){
   float turn=i<2?steer*29:0;
   if(Wheels[i])Wheels[i].localRotation=Quaternion.Euler(0,turn,0)*Quaternion.Euler(roll,0,0);
   if(Calipers!=null&&i<Calipers.Length&&Calipers[i])Calipers[i].localRotation=Quaternion.Euler(0,turn,0);
  }
  if(!Shell)return;
  tailBlock.SetColor(BaseColor,new Color(.62f,.016f,.015f));
  tailBlock.SetColor(Emission,new Color(1,.013f,.008f)*(brake>.08f?4.5f:1.0f));
  Shell.SetPropertyBlock(tailBlock,5);
 }
}
}
