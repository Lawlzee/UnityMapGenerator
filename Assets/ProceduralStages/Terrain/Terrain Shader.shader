Shader "Custom/Terrain Shader"
{
    Properties
    {
        _MainTex("Wall Texture", 2D) = "white" {}
        _WallNormalTex("Wall Normal Texture", 2D) = "bump" {}
        _WallBias("Wall Bias",  Range(0, 1)) = 1
        _WallColor ("Wall Average Color", Color) = (1,1,1,1)
        _WallScale("Wall Texture Scale",  float) = 1
        _WallBumpScale("Wall Bump Scale",  float) = 1
        _WallContrast("Wall Texture Contrast",  float) = 1
        _WallGlossiness("Wall Glossiness", Range(0, 1)) = 0.5
        [Gamma] _WallMetallic("Wall Metallic", Range(0, 1)) = 0

        _FloorTex("Floor Texture", 2D) = "white" {}
        _FloorNormalTex("Floor Normal Texture", 2D) = "bump" {}
        _FloorBias("Floor Bias",  Range(0, 1)) = 1
        _FloorColor ("Floor Average Color", Color) = (1,1,1,1)
        _FloorScale("Floor Texture Scale",  float) = 1
        _FloorBumpScale("Floor Bump Scale",  float) = 1
        _FloorContrast("Floor Texture Contrast",  float) = 1
        _FloorGlossiness("Floor Glossiness", Range(0, 1)) = 0.5
        [Gamma] _FloorMetallic("Floor Metallic", Range(0, 1)) = 0

        _ColorTex("Color Texture", 2D) = "white" {}

        //_Glossiness("Glossiness", Range(0, 1)) = 0.5
        
        _Intensity("Internsity", Float) = 1



        //_BumpScale("Bump Scale", Float) = 1
        //_BumpMap("Bump Map", 2D) = "bump" {}
        //
        //_OcclusionStrength("Occlusion Strength", Range(0, 1)) = 1
        //_OcclusionMap("Occlusion Map", 2D) = "white" {}

        //_MapScale("Map Scale", Float) = 1
    }
    SubShader
    {
        Tags { "LIGHTMODE" = "DEFERRED" "PreviewType" = "Plane" "RenderType" = "Opaque" }

        CGPROGRAM

        #pragma surface surf Standard vertex:vert fullforwardshadows addshadow

        //#pragma shader_feature _WallNormalTex
        //#pragma shader_feature _FloorTex

        #pragma target 3.5

        sampler2D _MainTex;
        sampler2D _WallNormalTex;
        half _WallBias;
        float4 _WallColor;
        float _WallScale;
        float _WallBumpScale;
        float _WallContrast;
        half _WallGlossiness;
        half _WallMetallic;

        sampler2D _FloorTex;
        sampler2D _FloorNormalTex;
        half _FloorBias;
        float4 _FloorColor;
        float _FloorScale;
        float _FloorBumpScale;
        float _FloorGlossiness;
        half _FloorContrast;
        half _FloorMetallic;

        half _Intensity;

        sampler2D _ColorTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 localCoord;
            float3 localNormal;
        };

        void vert(inout appdata_full v, out Input data)
        {
            UNITY_INITIALIZE_OUTPUT(Input, data);
            data.localCoord = v.vertex.xyz;
            data.localNormal = v.normal.xyz;
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

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Blending factor of triplanar mapping
            float3 blendingFactor = normalize(abs(IN.localNormal));
            blendingFactor /= dot(blendingFactor, (float3)1);

            // Color
            half4 color = tex2D(_ColorTex, IN.uv_MainTex);
            float3 hsv = RGBToHSV(color);

            float2 tx = IN.localCoord.zy;
            float2 ty = IN.localCoord.zx;
            float2 tz = IN.localCoord.xy;

            half4 wallColorX = tex2D(_MainTex, tx * _WallScale) * blendingFactor.x;
            half4 wallColorY = tex2D(_MainTex, ty * _WallScale) * blendingFactor.y;
            half4 wallColorZ = tex2D(_MainTex, tz * _WallScale) * blendingFactor.z;
            half4 wallColor = wallColorX + wallColorY + wallColorZ;
            
            float3 wallColorHsv = RGBToHSV(wallColor);
            float3 averageWallColorHsv = RGBToHSV(_WallColor);

            float wallHue = frac((wallColorHsv.x - averageWallColorHsv.x) + hsv.x);
            float wallSaturation = lerp(saturate((((wallColorHsv.y / averageWallColorHsv.y) - 1) * _WallContrast + 1) * hsv.y), wallColorHsv.y, _WallBias);
            float wallValue = lerp(saturate((((wallColorHsv.z / averageWallColorHsv.z) - 1) * _WallContrast + 1) * hsv.z), wallColorHsv.z, _WallBias);
            //float wallSaturation = hsv.y;
            //float wallValue = hsv.z;
            float3 newWallColorHsv = float3(wallHue, wallSaturation, wallValue);
            float3 newWallColor = HSVToRGB(newWallColorHsv);

            float floorIntensity = saturate(dot(normalize(IN.localNormal), float3(0, 1, 0)));
            half4 floorColorX = tex2D(_FloorTex, tx * _FloorScale) * blendingFactor.x;
            half4 floorColorY = tex2D(_FloorTex, ty * _FloorScale) * blendingFactor.y;
            half4 floorColorZ = tex2D(_FloorTex, tz * _FloorScale) * blendingFactor.z;
            half4 floorColor = floorColorX + floorColorY + floorColorZ;

            float3 floorColorHsv = RGBToHSV(floorColor);
            float3 averageFloorColorHsv = RGBToHSV(_FloorColor);

            float floorHue = frac((floorColorHsv.x - averageFloorColorHsv.x) + hsv.x);
            float floorSaturation = lerp(saturate((((floorColorHsv.y / averageFloorColorHsv.y) - 1) * _FloorContrast + 1) * hsv.y), floorColorHsv.y, _FloorBias);
            float floorValue = lerp(saturate((((floorColorHsv.z / averageFloorColorHsv.z) - 1) * _FloorContrast + 1) * hsv.z), floorColorHsv.z, _FloorBias);
            //float floorSaturation = hsv.y;
            //float floorValue = hsv.z;
            float3 newFloorColorHsv = float3(floorHue, floorSaturation, floorValue);
            float3 newFloorColor = HSVToRGB(newFloorColorHsv);

            float3 finalColor = lerp(newWallColor, newFloorColor, floorIntensity) * _Intensity;
            //float3 finalColor = HSVToRGB(textureColorHsv);

            
            //float3 textureColorHsv = RGBToHSV(textureColor);
            //textureColorHsv.x = hsv.x;
            //textureColorHsv.y *= hsv.y;
            //textureColorHsv.z *= hsv.z;

            //float3 snowColor = tex2D(_SnowTex, IN.uv_MainTex).rgb; // Change this line to tex2D(_SnowTex, IN.uv1).rgb;
            //

            //float3 finalColor = newFloorColorHsv;//HSVToRGB(textureColorHsv);// * _Intensity;
            o.Albedo = finalColor;
            o.Alpha = 1;

            ////#ifdef _WallNormalTex
            //    // Normal map
            //    half4 nx = tex2D(_WallNormalTex, tx * _WallScale) * blendingFactor.x;
            //    half4 ny = tex2D(_WallNormalTex, ty * _WallScale) * blendingFactor.y;
            //    half4 nz = tex2D(_WallNormalTex, tz * _WallScale) * blendingFactor.z;
            //    o.Normal = UnpackScaleNormal(nx + ny + nz, _WallBumpScale);
            ////#endif
        //
        //#ifdef _OCCLUSIONMAP
        //    // Occlusion map
        //    half ox = tex2D(_OcclusionMap, tx).g * bf.x;
        //    half oy = tex2D(_OcclusionMap, ty).g * bf.y;
        //    half oz = tex2D(_OcclusionMap, tz).g * bf.z;
        //    o.Occlusion = lerp((half4)1, ox + oy + oz, _OcclusionStrength);
        //#endif

            // Misc parameters
            o.Metallic = _WallMetallic;
            o.Smoothness = _WallGlossiness;
        }


        ENDCG
    }
    FallBack "Diffuse"
}