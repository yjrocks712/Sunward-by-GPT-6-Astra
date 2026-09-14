using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sunward {
public static class VehicleArt {
 static Material[] materials; static readonly Dictionary<int,Mesh> shells=new(); static Mesh detailedWheel,trafficWheel,caliperMesh;
 const int Paint=0,Dark=1,Glass=2,Metal=3,White=4,Red=5,Accent=6;
 static readonly Color shadow=new(.022f,.028f,.036f);
 public static VehicleVisual Build(Transform parent,CarSpec spec,Color paint,bool simple=false){
  Init();int style=Mathf.Clamp(spec.Style,0,3),key=style+(simple?4:0);if(!shells.TryGetValue(key,out var shell)){shell=Body(style,simple);shells.Add(key,shell);}
  var root=new GameObject(spec.Name+" / coachwork");root.transform.SetParent(parent,false);var visual=root.AddComponent<VehicleVisual>();visual.Simple=simple;
  var body=Piece(root.transform,"Sculpted monocoque",shell,materials);visual.Shell=body;visual.PaintRenderers=new Renderer[]{body};visual.Wheels=new Transform[4];visual.Calipers=simple?null:new Transform[4];
  for(int i=0;i<4;i++){var t=new GameObject(i==0?"FL":i==1?"FR":i==2?"RL":"RR").transform;t.SetParent(root.transform,false);t.localPosition=new Vector3(i%2==0?-.86f:.86f,.38f,i<2?1.35f:-1.35f);visual.Wheels[i]=t;Piece(t,"Forged wheel",simple?trafficWheel:detailedWheel,new[]{materials[Dark],materials[Metal]});if(!simple){var c=new GameObject("Fixed ceramic caliper").transform;c.SetParent(root.transform,false);c.localPosition=t.localPosition;visual.Calipers[i]=c;Piece(c,"Caliper",caliperMesh,new[]{materials[Accent]});}}
  if(!simple){visual.Headlights=new Light[2];for(int s=0;s<2;s++){var l=new GameObject("Projector "+s).AddComponent<Light>();l.transform.SetParent(root.transform,false);l.transform.localPosition=new Vector3(s==0?-.66f:.66f,.74f,Front(style)-.02f);l.transform.localRotation=Quaternion.Euler(8,0,0);l.type=LightType.Spot;l.color=new Color(.77f,.91f,1);l.intensity=3;l.range=29;l.spotAngle=58;l.innerSpotAngle=27;l.shadows=LightShadows.None;visual.Headlights[s]=l;}}
  visual.SetPaint(paint);return visual;
 }
 static MeshRenderer Piece(Transform parent,string name,Mesh mesh,Material[] mats){var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterials=mats;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;return r;}
 static void Init(){if(materials!=null&&materials[0])return;var shader=Shader.Find("Universal Render Pipeline/Lit");if(!shader)shader=Shader.Find("Standard");
  Material M(string name,Color color,float metal,float smooth,Color glow){var m=new Material(shader){name=name};m.SetColor("_BaseColor",color);m.SetColor("_Color",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Glossiness",smooth);if(glow.maxColorComponent>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",glow);}return m;}
  materials=new[]{M("Sunward • ceramic pearl",Color.white,.63f,.84f,Color.black),M("Sunward • basalt composite",shadow,.30f,.36f,Color.black),M("Sunward • smoked indigo glazing",new Color(.055f,.135f,.185f),.64f,.96f,Color.black),M("Sunward • brushed titanium",new Color(.43f,.49f,.53f),.93f,.75f,Color.black),M("Sunward • ice phosphor",new Color(.77f,.93f,1),.25f,.85f,new Color(.57f,.80f,1)*2.5f),M("Sunward • ruby phosphor",new Color(.64f,.018f,.014f),.2f,.85f,new Color(1,.014f,.009f)*1.2f),M("Sunward • copper ceramic",new Color(.95f,.27f,.064f),.7f,.52f,Color.black)};
  detailedWheel=Wheel(false);trafficWheel=Wheel(true);var cb=new Builder(1);cb.Box(new Vector3(.09f,.03f,-.18f),new Vector3(.09f,.29f,.13f),0,.026f);cb.Box(new Vector3(-.09f,.03f,-.18f),new Vector3(.09f,.29f,.13f),0,.026f);caliperMesh=cb.Finish("Copper calipers");
 }
 static float Front(int s)=>s==1?2.04f:s==2?2.57f:s==3?2.42f:2.29f;
 static float Rear(int s)=>s==1?-1.98f:s==2?-2.40f:s==3?-2.36f:-2.22f;
 static Mesh Body(int style,bool simple){var b=new Builder(7);float front=Front(style),rear=Rear(style),width=style==3?1.035f:style==2?.99f:.95f;int rings=simple?39:73;var grid=new Vector3[rings,18];
  for(int j=0;j<rings;j++){float z=Mathf.Lerp(rear,front,j/(float)(rings-1));float nose=Mathf.InverseLerp(front-.63f,front,z),tail=Mathf.InverseLerp(rear+.43f,rear,z);float w=width*(1-.15f*nose*nose-.065f*tail);float shoulder=.94f-.155f*nose-.055f*tail+(style==1?.04f:style==3?-.04f:0);float arch=.38f;foreach(float axle in new[]{-1.35f,1.35f}){float d=Mathf.Abs(z-axle);if(d<.465f)arch=Mathf.Max(arch,.38f+Mathf.Sqrt(.465f*.465f-d*d));}arch=Mathf.Min(arch,shoulder-.018f);
   float top=shoulder+.035f;float[] xs={0,.48f,.79f,.945f,1,1,.91f,.62f,0,-.62f,-.91f,-1,-1,-.945f,-.79f,-.48f,0,0};float[] ys={top+.012f,top+.024f,top+.014f,shoulder+.006f,shoulder-.047f,arch,arch-.035f,.32f,.31f,.32f,arch-.035f,arch,shoulder-.047f,shoulder+.006f,top+.014f,top+.024f,top+.012f,top+.012f};
   for(int i=0;i<16;i++)grid[j,i]=new Vector3(xs[i]*w,ys[i],z);
  }
  b.Skin(grid,rings,16,Paint,true);b.Cap(grid,0,16,Paint,false);b.Cap(grid,rings-1,16,Paint,true);
  float roof=style==1?1.59f:style==2?1.40f:style==3?1.29f:1.42f,frontFoot=style==2?.76f:style==3?.84f:1.01f,frontRoof=style==1?.44f:style==2?-.06f:.23f,rearRoof=style==1?-1.07f:style==3?-.51f:-.73f,rearFoot=style==1?-1.64f:style==3?-1.35f:-1.53f;
  float roofW=style==3?.65f:.635f,baseW=width*.81f,baseY=style==1?1.005f:.972f;
  Vector3 fl=new(-baseW,baseY,frontFoot),fr=new(baseW,baseY,frontFoot),tl=new(-roofW,roof-.022f,frontRoof),tr=new(roofW,roof-.022f,frontRoof),bl=new(-roofW,roof-.047f,rearRoof),br=new(roofW,roof-.047f,rearRoof),rl=new(-baseW,baseY,rearFoot),rr=new(baseW,baseY,rearFoot);
  b.Quad(fl,fr,tr,tl,Glass);b.Quad(rr,rl,bl,br,Glass);b.Quad(fr,rr,br,tr,Glass);b.Quad(rl,fl,tl,bl,Glass);
  var roofGrid=new Vector3[9,9];for(int z=0;z<9;z++)for(int x=0;x<9;x++){float u=x/8f,v=z/8f;roofGrid[z,x]=new Vector3(Mathf.Lerp(-roofW,roofW,u),Mathf.Lerp(roof-.022f,roof-.047f,v)+Mathf.Sin(u*Mathf.PI)*.050f+Mathf.Sin(v*Mathf.PI)*.025f,Mathf.Lerp(frontRoof,rearRoof,v));}b.Skin(roofGrid,9,9,Paint,false,true);
  for(int s=-1;s<=1;s+=2){var f=s<0?fl:fr;var t=s<0?tl:tr;var r=s<0?rl:rr;var q=s<0?bl:br;b.Beam(f,t,.045f,Paint);b.Beam(t,q,.045f,Paint);b.Beam(q,r,.059f,Paint);b.Beam(f,r,.032f,Dark);float pillar=style==1?.54f:.64f;b.Beam(Vector3.Lerp(f,r,pillar),Vector3.Lerp(t,q,pillar),style==1?.08f:.055f,Dark);
   b.Box(new Vector3(s*(width-.009f),.374f,-.01f),new Vector3(.105f,.115f,1.66f),Dark,.025f);b.Box(new Vector3(s*(width+.008f),.451f,-.03f),new Vector3(.024f,.025f,1.44f),Paint,.009f);
   b.Beam(new Vector3(s*baseW,.995f,.60f),new Vector3(s*(width+.12f),1.012f,.55f),.036f,Dark);b.Box(new Vector3(s*(width+.13f),1.025f,.54f),new Vector3(.22f,.11f,.245f),Paint,.045f);b.Box(new Vector3(s*(width+.13f),1.025f,.42f),new Vector3(.17f,.073f,.012f),Glass,.01f);
   if(!simple){b.Box(new Vector3(s*(width+.006f),.854f,-.36f),new Vector3(.018f,.030f,.14f),Dark,.008f);b.Beam(new Vector3(s*width,.805f,-.74f),new Vector3(s*width,.52f,-.73f),.009f,Dark);b.Beam(new Vector3(s*(width+.001f),.510f,-.71f),new Vector3(s*(width+.001f),.51f,.71f),.008f,Dark);
    b.Beam(new Vector3(s*.39f,1.004f,frontFoot+.02f),new Vector3(s*.53f,.91f,front-.35f),.011f,Dark);
   }
   for(int axle=0;axle<2;axle++){float az=axle==0?1.35f:-1.35f;var arc=new Vector3[19];for(int a=0;a<19;a++){float angle=Mathf.Lerp(0,Mathf.PI,a/18f);arc[a]=new Vector3(s*(width+.009f),.38f+Mathf.Sin(angle)*.465f,az+Mathf.Cos(angle)*.465f);}for(int a=0;a<18;a++)b.Beam(arc[a],arc[a+1],style==1?.055f:.018f,style==1?Dark:Paint);}
  }
  // Fascia is assembled as layered trapezoids, keeping the fictional face recognizable at distance.
  float face=front+.003f,back=rear-.005f;b.Box(new Vector3(0,.46f,front-.07f),new Vector3(width*1.68f,.20f,.19f),Dark,.055f);b.Box(new Vector3(0,.345f,front-.035f),new Vector3(width*1.84f,.066f,.31f),Dark,.025f);b.Box(new Vector3(0,.44f,rear+.06f),new Vector3(width*1.84f,.22f,.20f),Dark,.04f);b.Box(new Vector3(0,.329f,rear+.12f),new Vector3(width*1.73f,.070f,.37f),Dark,.023f);
  for(int s=-1;s<=1;s+=2){float x=s*width*.65f;QuadFace(b,new Vector3(x,.720f,face),.48f,.135f,Dark,s*.04f);for(int row=0;row<(style==1?2:3);row++){float y=.751f-row*.041f;QuadFace(b,new Vector3(x,y,face+.012f),style==2?.51f:.39f,.017f,White,s*.025f);}
   QuadFace(b,new Vector3(s*width*.62f,.782f,back),style==3?.58f:.52f,.111f,Dark,0,true);QuadFace(b,new Vector3(s*width*.62f,.795f,back-.012f),style==1?.34f:.46f,.025f,Red,0,true);QuadFace(b,new Vector3(s*width*.77f,.752f,back-.014f),.12f,.014f,Red,0,true);
   if(!simple){for(int v=0;v<3;v++)b.Box(new Vector3(s*(.67f+v*.036f),.524f,front+.017f),new Vector3(.011f,.083f,.025f),Metal,.003f);b.Tube(new Vector3(s*.66f,.391f,rear-.055f),.074f,.047f,.145f,Metal,12,2);b.Tube(new Vector3(s*.66f,.391f,rear-.134f),.047f,0,.011f,Dark,12,2);}
  }
  if(!simple){for(int i=-5;i<=5;i++)b.Box(new Vector3(i*.075f,.488f,face+.014f),new Vector3(.018f,.084f,.025f),Dark,.003f);for(int i=-3;i<=3;i++)b.Box(new Vector3(i*.115f,.333f,rear-.02f),new Vector3(.021f,.11f,.33f),Dark,.006f);
   b.Box(new Vector3(0,.598f,back-.016f),new Vector3(.45f,.113f,.029f),Metal,.012f);Lettering(b,"SUN-"+(style==0?"01":style==1?"04":style==2?"09":"12"),new Vector3(0,.598f,back-.033f),.0105f,Dark);
   Lettering(b,style==0?"SEREIN":style==1?"TAMAR":style==2?"VEYLAN":"ORISON",new Vector3(0,.704f,back-.020f),.008f,Metal);Digits(b,style==0?1:style==1?4:style==2?9:12,new Vector3(.66f,.656f,back-.019f));
   b.Beam(new Vector3(-.039f,.820f,face+.021f),new Vector3(0,.782f,face+.021f),.013f,Metal);b.Beam(new Vector3(0,.782f,face+.021f),new Vector3(.041f,.824f,face+.021f),.013f,Metal);
  }
  if(style==1){b.Box(new Vector3(0,roof-.02f,rearRoof-.06f),new Vector3(1.37f,.075f,.27f),Dark,.027f);for(int s=-1;s<=1;s+=2){b.Beam(new Vector3(s*.57f,roof+.006f,rearRoof+.17f),new Vector3(s*.57f,roof+.006f,frontRoof-.12f),.036f,Dark);if(!simple){b.Tube(new Vector3(s*.27f,.65f,front+.064f),.095f,0,.05f,Dark,16,2);b.Tube(new Vector3(s*.27f,.65f,front+.095f),.069f,0,.009f,White,16,2);}}
  }else if(style==3){for(int s=-1;s<=1;s+=2){b.Box(new Vector3(s*.62f,1.015f,rear+.44f),new Vector3(.045f,.33f,.20f),Dark,.016f);b.Box(new Vector3(s*.99f,1.184f,rear+.36f),new Vector3(.037f,.22f,.43f),Dark,.014f);b.Box(new Vector3(s*.83f,.64f,-.56f),new Vector3(.21f,.17f,.46f),Dark,.03f);if(!simple)for(int v=0;v<4;v++)b.Box(new Vector3(s*.63f,.972f,1.43f+v*.076f),new Vector3(.19f,.013f,.025f),Dark,.006f);}b.Box(new Vector3(0,1.175f,rear+.38f),new Vector3(2.05f,.065f,.39f),Dark,.025f);b.Box(new Vector3(0,1.204f,rear+.20f),new Vector3(1.94f,.021f,.046f),Paint,.009f);
  }else{b.Box(new Vector3(0,.985f,rear+.26f),new Vector3(width*1.69f,.055f,.19f),Paint,.025f);if(style==2){for(int s=-1;s<=1;s+=2)for(int v=0;v<3;v++)b.Box(new Vector3(s*.50f,.985f,1.14f+v*.10f),new Vector3(.23f,.016f,.025f),Dark,.006f);}}
  return b.Finish("Sunward / "+style+(simple?" traffic":" atelier"));
 }
 static void QuadFace(Builder b,Vector3 p,float w,float h,int mat,float skew=0,bool reverse=false){var a=p+new Vector3(-w/2,-h/2-skew,0);var c=p+new Vector3(w/2,h/2+skew,0);var d=p+new Vector3(-w/2,h/2-skew,0);var e=p+new Vector3(w/2,-h/2+skew,0);if(reverse)b.Quad(e,a,d,c,mat);else b.Quad(a,e,c,d,mat);}
 static void Lettering(Builder b,string text,Vector3 center,float pitch,int mat){for(int c=0;c<text.Length;c++){string glyph=text[c] switch {
  'A'=>"01110/10001/10001/11111/10001/10001/10001",'E'=>"11111/10000/10000/11110/10000/10000/11111",'I'=>"11111/00100/00100/00100/00100/00100/11111",'L'=>"10000/10000/10000/10000/10000/10000/11111",'M'=>"10001/11011/10101/10101/10001/10001/10001",'N'=>"10001/11001/11001/10101/10011/10011/10001",'O'=>"01110/10001/10001/10001/10001/10001/01110",'R'=>"11110/10001/10001/11110/10100/10010/10001",'S'=>"01111/10000/10000/01110/00001/00001/11110",'T'=>"11111/00100/00100/00100/00100/00100/00100",'U'=>"10001/10001/10001/10001/10001/10001/01110",'V'=>"10001/10001/10001/10001/01010/01010/00100",'Y'=>"10001/10001/01010/00100/00100/00100/00100",
  '0'=>"01110/10001/10011/10101/11001/10001/01110",'1'=>"00100/01100/00100/00100/00100/00100/01110",'2'=>"01110/10001/00001/00010/00100/01000/11111",'4'=>"00010/00110/01010/10010/11111/00010/00010",'9'=>"01110/10001/10001/01111/00001/00001/01110",'-'=>"00000/00000/00000/11111/00000/00000/00000",_=>"00000/00000/00000/00000/00000/00000/00000"};
  for(int y=0;y<7;y++)for(int x=0;x<5;x++)if(glyph[y*6+x]=='1')QuadFace(b,center+new Vector3((c*6+x-(text.Length*6-2)*.5f)*pitch,(3-y)*pitch,0),pitch*.90f,pitch*.90f,mat,0,true);
 }}
 static void Digits(Builder b,int value,Vector3 pos){string text=value.ToString("00");int[] masks={63,6,91,79,102,109,125,7,127,111};for(int d=0;d<2;d++){int bits=masks[text[d]-'0'];for(int s=0;s<7;s++)if((bits&(1<<s))!=0){bool horizontal=s==0||s==3||s==6;float x=s==1||s==2?.018f:s==4||s==5?-.018f:0,y=s==0?.041f:s==1||s==5?.0205f:s==2||s==4?-.0205f:s==3?-.041f:0;b.Box(pos+new Vector3((d-.5f)*.052f+x,y,0),new Vector3(horizontal?.035f:.008f,horizontal?.008f:.033f,.006f),Metal,.002f);}}}
 static Mesh Wheel(bool simple){var b=new Builder(2);int n=simple?18:40;float[] x={-.139f,-.133f,-.112f,-.073f,.073f,.112f,.133f,.139f};float[] r={.244f,.300f,.343f,.360f,.360f,.343f,.300f,.244f};var skin=new Vector3[x.Length,n];for(int j=0;j<x.Length;j++)for(int i=0;i<n;i++){float a=i*2*Mathf.PI/n;skin[j,i]=new Vector3(x[j],Mathf.Cos(a)*r[j],Mathf.Sin(a)*r[j]);}b.Skin(skin,x.Length,n,0,true,true);
  for(int side=-1;side<=1;side+=2){b.Tube(new Vector3(side*.14f,0,0),.248f,.214f,.018f,1,n,0);b.Tube(new Vector3(side*.12f,0,0),.198f,0,.012f,0,n,0);b.Tube(new Vector3(side*.146f,0,0),.072f,0,.025f,1,12,0);
   for(int i=0;i<(simple?5:7);i++){float a=i*Mathf.PI*2/(simple?5:7);Vector3 start=new(side*.154f,Mathf.Cos(a)*.062f,Mathf.Sin(a)*.062f),end=new(side*.153f,Mathf.Cos(a+.21f)*.22f,Mathf.Sin(a+.21f)*.22f);b.Beam(start,end,simple?.026f:.024f,1);if(!simple)b.Beam(start,end+new Vector3(0,-Mathf.Sin(a)*.032f,Mathf.Cos(a)*.032f),.014f,1);}
   if(!simple){for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;b.Tube(new Vector3(side*.16f,Mathf.Cos(a)*.044f,Mathf.Sin(a)*.044f),.009f,0,.009f,0,6,0);}for(int i=0;i<24;i++){float a=i*Mathf.PI/12;var p=new Vector3(side*.104f,Mathf.Cos(a)*.321f,Mathf.Sin(a)*.321f);b.Beam(p,new Vector3(side*.112f,Mathf.Cos(a+.065f)*.324f,Mathf.Sin(a+.065f)*.324f),.009f,0);}}
  }return b.Finish(simple?"Lightweight traffic wheel":"Seven-split forged wheel");
 }
 sealed class Builder {
  readonly List<Vector3> verts=new();readonly List<Vector2> uv=new();readonly List<int>[] tris;
  public Builder(int count){tris=new List<int>[count];for(int i=0;i<count;i++)tris[i]=new List<int>();}
  int V(Vector3 p){int i=verts.Count;verts.Add(p);uv.Add(new Vector2(p.x+p.z,p.y));return i;}
  void Tri(int a,int b,int c,int m){tris[m].Add(a);tris[m].Add(b);tris[m].Add(c);}
  public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int m){int n=V(a);V(b);V(c);V(d);Tri(n,n+1,n+2,m);Tri(n,n+2,n+3,m);}
  public void Skin(Vector3[,] points,int rows,int cols,int mat,bool close,bool reverse=false){int start=verts.Count;for(int j=0;j<rows;j++)for(int i=0;i<cols;i++)V(points[j,i]);for(int j=0;j<rows-1;j++)for(int i=0;i<(close?cols:cols-1);i++){int a=start+j*cols+i,b=start+(j+1)*cols+i,c=start+(j+1)*cols+(i+1)%cols,d=start+j*cols+(i+1)%cols;if(reverse){Tri(a,c,b,mat);Tri(a,d,c,mat);}else{Tri(a,b,c,mat);Tri(a,c,d,mat);}}}
  public void Cap(Vector3[,] p,int row,int count,int mat,bool front){Vector3 center=Vector3.zero;for(int i=0;i<count;i++)center+=p[row,i];center/=count;for(int i=0;i<count;i++){int a=V(center),b=V(p[row,i]),c=V(p[row,(i+1)%count]);if(front)Tri(a,c,b,mat);else Tri(a,b,c,mat);}}
  public void Beam(Vector3 a,Vector3 b,float thickness,int mat){Vector3 delta=b-a;if(delta.sqrMagnitude<.00000001f)return;Box((a+b)*.5f,new Vector3(thickness,thickness,delta.magnitude+thickness*.2f),mat,Mathf.Min(thickness*.22f,.014f),Quaternion.LookRotation(delta));}
  public void Box(Vector3 c,Vector3 size,int mat,float bevel=0,Quaternion? orientation=null){Quaternion q=orientation??Quaternion.identity;Vector3 half=size*.5f;float v=Mathf.Min(bevel,Mathf.Min(half.x,Mathf.Min(half.y,half.z))*.8f);var axes=new[]{Vector3.right,Vector3.up,Vector3.forward};for(int axis=0;axis<3;axis++)for(int sign=-1;sign<=1;sign+=2){int u=(axis+1)%3,w=(axis+2)%3;Vector3 n=axes[axis]*sign,U=axes[u],W=axes[w];float hx=half[u]-v,hy=half[w]-v;Vector3 p=n*half[axis];Vector3[] corners={p-U*hx-W*hy,p+U*hx-W*hy,p+U*hx+W*hy,p-U*hx+W*hy};if(sign<0)Array.Reverse(corners);Quad(c+q*corners[0],c+q*corners[1],c+q*corners[2],c+q*corners[3],mat);}
   if(v>.00001f){for(int axis=0;axis<3;axis++){int u=(axis+1)%3,w=(axis+2)%3;for(int su=-1;su<=1;su+=2)for(int sw=-1;sw<=1;sw+=2){Vector3 a=axes[u]*su*half[u]+axes[w]*sw*(half[w]-v),b=axes[u]*su*(half[u]-v)+axes[w]*sw*half[w],d=axes[axis]*(half[axis]-v);if(su*sw>0)Quad(c+q*(a-d),c+q*(b-d),c+q*(b+d),c+q*(a+d),mat);else Quad(c+q*(b-d),c+q*(a-d),c+q*(a+d),c+q*(b+d),mat);}}
    for(int sx=-1;sx<=1;sx+=2)for(int sy=-1;sy<=1;sy+=2)for(int sz=-1;sz<=1;sz+=2){Vector3 p=new(sx*half.x,sy*half.y,sz*half.z);int a=V(c+q*(p-new Vector3(0,sy*v,sz*v))),b=V(c+q*(p-new Vector3(sx*v,0,sz*v))),d=V(c+q*(p-new Vector3(sx*v,sy*v,0)));if(sx*sy*sz>0)Tri(a,b,d,mat);else Tri(a,d,b,mat);}}
  }
  public void Tube(Vector3 center,float outer,float inner,float depth,int mat,int segments,int axis){Vector3 Rotate(Vector3 p)=>axis==0?new Vector3(p.z,p.x,p.y):axis==1?new Vector3(p.x,p.z,p.y):p;for(int i=0;i<segments;i++){float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;Vector3 P(float angle,float radius,float z)=>center+Rotate(new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,z));float h=depth/2;Quad(P(a,outer,-h),P(b,outer,-h),P(b,outer,h),P(a,outer,h),mat);Quad(P(a,inner,h),P(a,outer,h),P(b,outer,h),P(b,inner,h),mat);Quad(P(b,inner,-h),P(b,outer,-h),P(a,outer,-h),P(a,inner,-h),mat);if(inner>0)Quad(P(b,inner,-h),P(a,inner,-h),P(a,inner,h),P(b,inner,h),mat);}}
  public Mesh Finish(string name){var mesh=new Mesh{name=name};if(verts.Count>65535)mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(verts);mesh.SetUVs(0,uv);mesh.subMeshCount=tris.Length;for(int i=0;i<tris.Length;i++)mesh.SetTriangles(tris[i],i);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;}
 }
}
}
