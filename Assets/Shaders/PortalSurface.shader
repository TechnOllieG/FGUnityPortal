Shader "Custom/PortalSurface"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0

        _PortalCurrentlyPlaced ("Portal Currently Placed", Int) = -1
        _PortalLocalScale ("Portal Local Scale", Vector) = (1, 2, 0, 0)
        _PortalLocalPos ("Portal Local Pos", Vector) = (0, 0, 0, 0)
        _PortalLocalUpVector ("Portal Local Up Vector", Vector) = (0, 0, 0, 0)
        _PortalLocalRightVector ("Portal Local Right Vector", Vector) = (0, 0, 0, 0)
        _PortalWorldNormal ("Portal World Normal", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="TransparentCutout"
        }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float4 _PortalLocalScale;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
        int _PortalCurrentlyPlaced;
        float4 _PortalLocalPos;
        float4 _PortalLocalUpVector;
        float4 _PortalLocalRightVector;
        float4 _PortalWorldNormal;
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            if (_PortalCurrentlyPlaced > 0)
            {
                float3 localPos = IN.worldPos - mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                float3 diffNormal = _PortalWorldNormal - IN.worldNormal;
                float3 fragPosPortalSpace = localPos - _PortalLocalPos;
                if (dot(diffNormal, diffNormal) < 0.5f &&
                    abs(dot(_PortalLocalUpVector, fragPosPortalSpace)) < _PortalLocalScale.y * 0.5f &&
                    abs(dot(_PortalLocalRightVector, fragPosPortalSpace)) < _PortalLocalScale.x * 0.5f)
                    clip(-1);
            }

            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}