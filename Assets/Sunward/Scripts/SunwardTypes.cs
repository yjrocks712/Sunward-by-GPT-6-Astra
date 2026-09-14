using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sunward {
[Serializable] public class CarSpec {
 public string Id,Name,Category,Description; public int Price,Style; public float Mass,Power,TopSpeed,Grip,Steering; public Color Paint;
 public static readonly CarSpec[] All={
  new(){Id="serein",Name="SEREIN 01",Category="SPORT COUPE",Description="Balanced, agile, made for the coast.",Price=0,Style=0,Mass=1180,Power=24,TopSpeed=225,Grip=9,Steering=1,Paint=new Color(.92f,.26f,.10f)},
  new(){Id="tamar",Name="TAMAR 04",Category="RALLY SPORT",Description="Short wheelbase. Loose surfaces. Long adventures.",Price=3800,Style=1,Mass=1080,Power=27,TopSpeed=205,Grip=10.5f,Steering=1.12f,Paint=new Color(.15f,.65f,.59f)},
  new(){Id="veylan",Name="VEYLAN 09",Category="GRAND TOURER",Description="Long legs and a beautifully unruly rear axle.",Price=6500,Style=2,Mass=1430,Power=30,TopSpeed=265,Grip=8.4f,Steering=.90f,Paint=new Color(.45f,.40f,.85f)},
  new(){Id="orison",Name="ORISON 12",Category="AERO SPORT",Description="Low, wide, and hungry for the next horizon.",Price=11000,Style=3,Mass=1250,Power=37,TopSpeed=305,Grip=11.5f,Steering=.98f,Paint=new Color(.94f,.79f,.29f)}
 };
}
[Serializable] public class MapLandmark { public string Name,Kind; public Vector3 Position; public Color Color; public MapLandmark(string name,string kind,Vector3 p,Color c){Name=name;Kind=kind;Position=p;Color=c;} }
[Serializable] public class RoadPath {
 public string Name; public Vector3[] Points; public float Width,Length; public bool Closed; public Color MapColor; public float[] Distances;
 public RoadPath(string name,IList<Vector3> knots,float width,bool closed=true,int steps=20){Name=name;Width=width;Closed=closed;MapColor=new Color(.81f,.82f,.75f);var points=new List<Vector3>();int n=knots.Count;int seg=closed?n:n-1;
  for(int i=0;i<seg;i++)for(int s=0;s<steps;s++){float t=s/(float)steps;Vector3 a=knots[closed?(i-1+n)%n:Mathf.Max(0,i-1)],b=knots[i],c=knots[(i+1)%n],d=knots[closed?(i+2)%n:Mathf.Min(n-1,i+2)];points.Add(.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t));}
  points.Add(closed?points[0]:knots[n-1]);Points=points.ToArray();Distances=new float[Points.Length];for(int i=1;i<Points.Length;i++){Length+=Vector3.Distance(Points[i-1],Points[i]);Distances[i]=Length;}
 }
 public void Sample(float distance,out Vector3 position,out Vector3 forward){float d=Closed?Mathf.Repeat(distance,Length):Mathf.Clamp(distance,0,Length-.001f);int i=Array.BinarySearch(Distances,d);if(i<0)i=~i-1;i=Mathf.Clamp(i,0,Points.Length-2);float t=Mathf.InverseLerp(Distances[i],Distances[i+1],d);position=Vector3.Lerp(Points[i],Points[i+1],t);forward=(Points[i+1]-Points[i]).normalized;}
 public Vector3 Position(float d){Sample(d,out var p,out _);return p;} public Quaternion Rotation(float d){Sample(d,out _,out var f);return Quaternion.LookRotation(f,Vector3.up);}
 public float ClosestDistance(Vector3 p){float best=float.MaxValue,result=0;for(int i=0;i<Points.Length-1;i++){Vector3 a=Points[i],v=Points[i+1]-a;float t=Mathf.Clamp01(Vector3.Dot(p-a,v)/v.sqrMagnitude);float sq=(p-a-t*v).sqrMagnitude;if(sq<best){best=sq;result=Mathf.Lerp(Distances[i],Distances[i+1],t);}}return result;}
}
public static class DriveInput {
 public static bool Press(Key key)=>Keyboard.current!=null&&Keyboard.current[key].wasPressedThisFrame;
 public static bool Held(Key key)=>Keyboard.current!=null&&Keyboard.current[key].isPressed;
 public static float Throttle=>Mathf.Max(Held(Key.W)||Held(Key.UpArrow)?1:0,Gamepad.current?.rightTrigger.ReadValue()??0);
 public static float Brake=>Mathf.Max(Held(Key.S)||Held(Key.DownArrow)?1:0,Gamepad.current?.leftTrigger.ReadValue()??0);
 public static float Steer=>Mathf.Clamp((Held(Key.D)||Held(Key.RightArrow)?1:0)-(Held(Key.A)||Held(Key.LeftArrow)?1:0)+(Gamepad.current?.leftStick.x.ReadValue()??0),-1,1);
 public static bool Drift=>Held(Key.Space)||(Gamepad.current?.buttonSouth.isPressed??false);
 public static bool Boost=>Held(Key.LeftShift)||(Gamepad.current?.buttonWest.isPressed??false);
 public static bool Confirm=>Press(Key.E)||(Gamepad.current?.buttonNorth.wasPressedThisFrame??false);
}
}
