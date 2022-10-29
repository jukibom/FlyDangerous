

Shader "Unlit/Abstract_Shader"
{
    Properties
    {
        _SeethPow ("Seething Power", float) = 0.1
        _SeethSpeed ("Seething Speed", float) = 0.1
        _Noisescale ("Seething Noise Scale", float) = 1.0
        _NoiseDetail ("Noise Detail", float) = 3.0
        _NoiseRough ("Noise Roughness", float) = 0.5
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "webgl-noise-master/src/noise3d.glsl"
            #include "webgl-noise-master/src/colormath.glsl"
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };



            float snoisedetail2(float3 v, float detail, float roughness)
{

    float c = 0.0;
    float Scale = 0.0;
    for (int i = 1; i <= ceil(detail); i++)
    {
        if (i <= detail)
        {

            c += snoise(v * pow(i, 2.0)) * (1 / pow(i, 1 / roughness));
            Scale += (1 / pow(i, 1 / roughness));

        }
        else
        {
            float detfac = (clamp(1+detail-i, 0, 1));
            c += snoise(v * pow(i, 2.0)) * (1 / pow(i, 1 / roughness) * detfac);
            Scale += (1 / pow(i, 1 / roughness)) * detfac;
        }
    }
    return c / Scale;
}


            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float3 position : TEXCOORD1;

            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _SeethPow;

            float _SeethSpeed;
            float _Noisescale;
            float _NoiseDetail;
            float _NoiseRough;
            float normal;

            float4 mixcol1 (float4 col1, float4 col2, float fac)
{
   fac = 0.5+ -0.5 * cos(fac * 3.14159);
   return col1*fac + col2*(1.0-fac);
   //sas
}

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.pos + _SeethPow*v.normal*snoisedetail2(v.pos*_Noisescale+float3(0,_SeethSpeed*_Time.z,0),_NoiseDetail,_NoiseRough));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.position = v.pos;
                

                return o;
            }
            float4 frag (v2f i) : SV_Target
            {
                float3 viewDir = normalize(ObjSpaceViewDir(float4(i.position.xyz,0.0)));
                float noisefloat = snoisedetail2((i.position*_Noisescale)+float3(0.0,_SeethSpeed*_Time.z,0.0),_NoiseDetail,_NoiseRough);
                // sample the texture
                float3 noiseder = float3(ddx(noisefloat),ddy(noisefloat),0.0);
                float3 coordder = float3(length(ddx(i.position)),length(ddy(i.position)),0.0);
                //noiseder.z =sqrt(1.0 - pow(length(noiseder),2.0));


                float4 col = tex2D(_MainTex, i.uv);
                col = noisefloat;
                col = (col+1.0)/2;
                col = pow(col,0.07);
                
               // col *= 1-pow(dot(viewDir,normalize(i.normal)),0.1);
               // col *= 10.0; 
                
               col = 1.0;
                float fresnelcol = 0.1*length(noiseder)/length(coordder);
                fresnelcol = pow(fresnelcol,5);
                col = col*fresnelcol;
                float fac = col;
                fac = clamp(0.0,1.0,fac);
                col = mixcol1(float4(0.01,0.05,0.05,1.0), float4(25,20,10,1.0),1-fac);

                col = mixcol1(0.0,col, pow(1-fac,50));

                return col;
            }

           
            ENDCG
        }
    }
}
