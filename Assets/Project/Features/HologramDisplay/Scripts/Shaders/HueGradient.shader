//Lit shader that colors the mesh with all hues around its vertical axis, like the ball's vertex colors. Used for models without textures, e.g. the cat.
Shader "Hologram/Hue Gradient"
{
    Properties
    {
        _Saturation ("Saturation", Range(0, 1)) = 0.85
        _Value ("Value", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        CGPROGRAM
        #pragma surface surf Lambert vertex:vert

        float _Saturation;
        float _Value;

        struct Input
        {
            float3 objectPos;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.objectPos = v.vertex.xyz;
        }

        float3 HsvToRgb(float h, float s, float v)
        {
            float3 rgb = saturate(abs(frac(h + float3(0, 2.0 / 3.0, 1.0 / 3.0)) * 6 - 3) - 1);
            return v * lerp(float3(1, 1, 1), rgb, s);
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            //Angle around the vertical axis mapped to 0..1, so the hue goes once around the model.
            float hue = atan2(IN.objectPos.z, IN.objectPos.x) / (2 * UNITY_PI) + 0.5;
            o.Albedo = HsvToRgb(hue, _Saturation, _Value);
        }
        ENDCG
    }

    Fallback "Diffuse"
}
