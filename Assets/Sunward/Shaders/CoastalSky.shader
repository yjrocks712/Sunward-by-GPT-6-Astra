Shader "Sunward/CoastalSky" {
Properties {_SunDirection("Sun direction",Vector)=(.4,.5,.5,0)}
SubShader {Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"} Cull Off ZWrite Off
Pass {HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct appdata {float4 vertex:POSITION;};struct v2f {float4 vertex:SV_POSITION;float3 dir:TEXCOORD0;};float4 _SunDirection;
v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.dir=v.vertex.xyz;return o;}
float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
half4 frag(v2f i):SV_Target {float3 d=normalize(i.dir);float h=saturate(d.y);float3 col=lerp(float3(.72,.83,.86),float3(.13,.40,.62),pow(h,.55));float sun=saturate(dot(d,normalize(_SunDirection.xyz)));col+=pow(sun,10)*float3(.22,.15,.06);col+=smoothstep(.9994,.9998,sun)*float3(7,5.2,2.7);float2 uv=d.xz/max(.15,d.y)*1.7+float2(_Time.y*.002,0);float n=noise(uv)*.56+noise(uv*2.12)*.26+noise(uv*4.2)*.14;float cloud=smoothstep(.56,.75,n)*smoothstep(.02,.18,d.y)*(1-smoothstep(.5,.85,d.y));col=lerp(col,float3(.98,.94,.85),cloud*.65);return half4(col,1);}
ENDHLSL}
}Fallback Off }
