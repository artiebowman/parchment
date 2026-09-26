Shader "Parchment/Ghost Hand"
{
    Properties
    {
        _FillColor ("Fill (alpha = inside opacity)", Color) = (0, 0, 0, 0.15)
        _OutlineColor ("Outline (alpha = edge opacity)", Color) = (1, 1, 1, 0.9)
        _OutlinePower ("Outline Sharpness", Range(0.5, 8)) = 3
        _OutlineStrength ("Outline Strength", Range(0, 3)) = 1.5
        _FadeStart ("Wrist Fade Start (m past wrist)", Range(0, 0.2)) = 0.02
        _FadeLength ("Wrist Fade Length (m)", Range(0.01, 0.3)) = 0.06
        _WristPos ("Wrist Position (set by script)", Vector) = (0, 0, 0, 0)
        _ForearmDir ("Forearm Direction (set by script)", Vector) = (0, 0, 1, 0)
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    struct appdata
    {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct v2f
    {
        float4 vertex : SV_POSITION;
        float3 normal : NORMAL;
        float3 worldPos : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    fixed4 _FillColor;
    fixed4 _OutlineColor;
    float _OutlinePower;
    float _OutlineStrength;
    float _FadeStart;
    float _FadeLength;
    float4 _WristPos;
    float4 _ForearmDir;

    v2f vert (appdata v)
    {
        v2f o;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_INITIALIZE_OUTPUT(v2f, o);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

        o.vertex = UnityObjectToClipPos(v.vertex);
        o.normal = UnityObjectToWorldNormal(v.normal);
        o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
        return o;
    }

    fixed4 frag (v2f i) : SV_Target
    {
        float3 n = normalize(i.normal);
        float3 v = normalize(_WorldSpaceCameraPos - i.worldPos);

        // Edges you see side-on light up: that is the outline.
        float edge = saturate(pow(1.0 - saturate(dot(n, v)), _OutlinePower) * _OutlineStrength);

        fixed3 color = lerp(_FillColor.rgb, _OutlineColor.rgb, edge);
        float alpha = lerp(_FillColor.a, _OutlineColor.a, edge);

        // Fade out along the forearm, measured from the wrist toward the elbow.
        float along = dot(i.worldPos - _WristPos.xyz, normalize(_ForearmDir.xyz));
        float fade = 1.0 - saturate((along - _FadeStart) / _FadeLength);
        alpha *= fade;

        return fixed4(color, alpha);
    }
    ENDCG

    SubShader
    {
        PackageRequirements { "com.unity.render-pipelines.universal" }
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass { ZWrite On ColorMask 0 }

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha, Zero One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass { ZWrite On ColorMask 0 }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha, Zero One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    }
}