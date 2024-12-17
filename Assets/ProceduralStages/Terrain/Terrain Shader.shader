Shader "Custom/Terrain Shader"
{
    //https://github.com/LucianoZadikian/EtherealForest/blob/main/WaffleHouse/Assets/WaffleHouse/WaffleHouseAssets/Materials/RoR2Triplanar.shader
    Properties
    {
        _MainTex("Wall Texture", 2D) = "white" {}

        //_WallNormalTex("Wall Normal Texture", 2D) = "bump" {}
        _WallBias("Wall Bias",  Range(0, 1)) = 1
        _WallColor ("Wall Average Color", Color) = (1,1,1,1)
        _WallScale("Wall Texture Scale",  float) = 1
        //_WallBumpScale("Wall Bump Scale",  float) = 1
        _WallContrast("Wall Texture Contrast",  float) = 1
        
        _WallGlossiness("Wall Glossiness", Range(0, 1)) = 0.5
        [Gamma] _WallMetallic("Wall Metallic", Range(0, 1)) = 0

        _FloorTex("Floor Texture", 2D) = "white" {}
        //_FloorNormalTex("Floor Normal Texture", 2D) = "bump" {}
        _FloorBias("Floor Bias",  Range(0, 1)) = 1
        _FloorColor ("Floor Average Color", Color) = (1,1,1,1)
        _FloorScale("Floor Texture Scale",  float) = 1
        //_FloorBumpScale("Floor Bump Scale",  float) = 1
        _FloorContrast("Floor Texture Contrast",  float) = 1
        //_FloorGlossiness("Floor Glossiness", Range(0, 1)) = 0.5
        //[Gamma] _FloorMetallic("Floor Metallic", Range(0, 1)) = 0

        _DetailTex("Detail Texture", 2D) = "white" {}
        _DetailBias("Detail Bias",  Range(0, 1)) = 1
        _DetailColor ("Detail Average Color", Color) = (1,1,1,1)
        _DetailScale("Detail Texture Scale",  float) = 1
        _DetailContrast("Detail Texture Contrast",  float) = 1

        _DetailScaleCoefficient("Detail Scale Coefficient",  float) = 1
        _DetailIntensity("Detail Intensity",  float) = 1

        _ColorTex("Color Texture", 2D) = "white" {}

        //_Glossiness("Glossiness", Range(0, 1)) = 0.5
        
        _Intensity("Intensity", Float) = 1
        _HeightblendFactor("Height Blend Factor", Float) = 1

        //_BumpScale("Bump Scale", Float) = 1
        //_BumpMap("Bump Map", 2D) = "bump" {}
        //
        //_OcclusionStrength("Occlusion Strength", Range(0, 1)) = 1
        //_OcclusionMap("Occlusion Map", 2D) = "white" {}

        //_MapScale("Map Scale", Float) = 1
    }
    SubShader
    {
        Tags { 
            "LIGHTMODE" = "DEFERRED" 
            "PreviewType" = "Plane" 
            "RenderType" = "Opaque" 
        }

        CGPROGRAM

        #pragma surface surf Standard vertex:vert// fullforwardshadows addshadow

        //#pragma shader_feature _WallNormalTex
        //#pragma shader_feature _FloorTex

        #pragma target 3.5

        sampler2D _MainTex;
        //sampler2D _WallNormalTex;
        half _WallBias;
        float4 _WallColor;
        float _WallScale;
        //float _WallBumpScale;
        float _WallContrast;
        half _WallGlossiness;
        half _WallMetallic;

        sampler2D _FloorTex;
        //sampler2D _FloorNormalTex;
        half _FloorBias;
        float4 _FloorColor;
        float _FloorScale;
        //float _FloorBumpScale;
        half _FloorContrast;
        //float _FloorGlossiness;
        //half _FloorMetallic;

        sampler2D _DetailTex;
        //sampler2D _FloorNormalTex;
        half _DetailBias;
        float4 _DetailColor;
        float _DetailScale;
        //float _FloorBumpScale;
        half _DetailContrast;
        half _DetailScaleCoefficient;
        half _DetailIntensity;
        //float _FloorGlossiness;
        //half _FloorMetallic;

        half _Intensity;
        half _HeightblendFactor;

        sampler2D _ColorTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 localCoord;
            float3 worldNormal;
            float3 worldUp;
        };

        void vert(inout appdata_full v, out Input data)
        {
            UNITY_INITIALIZE_OUTPUT(Input, data);
            float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
            data.localCoord = worldPos.xyz;
            data.worldNormal = UnityObjectToWorldNormal(v.normal);
        }

        float3 RGBToHSV(float3 c)
        {
            float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
            float4 p = lerp( float4( c.bg, K.wz ), float4( c.gb, K.xy ), step( c.b, c.g ) );
            float4 q = lerp( float4( p.xyw, c.r ), float4( c.r, p.yzx ), step( p.x, c.r ) );
            float d = q.x - min( q.w, q.y );
            float e = 1.0e-10;
            return float3( abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
        }

        float3 HSVToRGB(float3 c)
        {
            float4 K = float4( 1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0 );
            float3 p = abs( frac( c.xxx + K.xyz ) * 6.0 - K.www );
            return c.z * lerp( K.xxx, saturate( p - K.xxx ), c.y );
        }

        float4 heightblend(float4 input1, float4 input2)
        {
            float height_start = max(input1.a, input2.a) - _HeightblendFactor;
            float level1 = max(input1.a - height_start, 0);
            float level2 = max(input2.a - height_start, 0);
            return ((input1 * level1) + (input2 * level2)) / (level1 + level2);
        }

        //http://untitledgam.es/2017/01/height-blending-shader/
        float4 heightlerp(float4 input1, float4 input2, float t)
        {
            t = clamp(t, 0, 1);
            input1.a *= (1 - t); 
            input2.a *= t; 
            return heightblend(input1, input2);
        }

        float4 getColor(float2 tx, float2 ty, float2 tz, float4 averageColor, float3 blendingFactor, float3 hsv, sampler2D tex, float scale, float contrast, half bias)
        {
            float4 colorX = tex2D(tex, tx * scale) * blendingFactor.x;
            float4 colorY = tex2D(tex, ty * scale) * blendingFactor.y;
            float4 colorZ = tex2D(tex, tz * scale) * blendingFactor.z;
            float4 color = colorX + colorY + colorZ;

            float3 colorHsv = RGBToHSV(color);
            
            float3 averageColorHsv = RGBToHSV(averageColor);
            float hue = frac((colorHsv.x - averageColorHsv.x) + hsv.x);
            float saturation = lerp(saturate((((colorHsv.y / averageColorHsv.y) - 1) * contrast + 1) * hsv.y), colorHsv.y, bias);
            float value = lerp(saturate((((colorHsv.z / averageColorHsv.z) - 1) * contrast + 1) * hsv.z), colorHsv.z, bias);
            
            float3 newColorHsv = float3(hue, saturation, value);
            float3 newColor = HSVToRGB(newColorHsv);
            return float4(newColor.x, newColor.y, newColor.z, color.a);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Blending factor of triplanar mapping
            float3 blendingFactor = normalize(abs(IN.worldNormal));
            float dotL = dot(blendingFactor, (float3)1);
            blendingFactor /= dotL;

            half4 color = tex2D(_ColorTex, IN.uv_MainTex);

            float3 hsv = RGBToHSV(color);
            
            float2 tx = IN.localCoord.zy;
            float2 ty = IN.localCoord.zx;
            float2 tz = IN.localCoord.xy;
            
            float4 wallColor = getColor(tx, ty, tz, _WallColor, blendingFactor, hsv, _MainTex, _WallScale, _WallContrast, _WallBias); 
            float4 detailColor = getColor(tx, ty, tz, _DetailColor, blendingFactor, hsv, _DetailTex, _DetailScaleCoefficient * _DetailScale, _DetailContrast, _DetailBias); 
            float4 floorColor = getColor(tx, ty, tz, _FloorColor, blendingFactor, hsv, _FloorTex, _FloorScale, _FloorContrast, _FloorBias);
            
            float floorIntensity = saturate(dot(normalize(IN.worldNormal), float3(0, 1, 0)));
            float4 surfaceColor = heightlerp(wallColor, floorColor, floorIntensity) * _Intensity;
            
            float4 finalColor = saturate(heightlerp(surfaceColor, detailColor, _DetailIntensity));
            finalColor.r = floorIntensity;
            //finalColor.g = 0;
            //finalColor.b = 0;


            o.Albedo = surfaceColor.rgb;//(surfaceColor + detailColor.xyz * _DetailIntensity) / (1 + _DetailIntensity);
            o.Alpha = 1;
            o.Metallic = _WallMetallic;
            o.Smoothness = _WallGlossiness;
        }


        ENDCG
    }
    FallBack "Diffuse"
}