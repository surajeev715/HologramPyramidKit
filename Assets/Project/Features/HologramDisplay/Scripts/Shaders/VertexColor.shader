//Lit shader that uses the mesh's vertex colors, used for the generated pyramid and ball.
Shader "Hologram/Vertex Color"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        CGPROGRAM
        #pragma surface surf Lambert

        struct Input
        {
            float4 color : COLOR;
        };

        void surf(Input IN, inout SurfaceOutput o)
        {
            o.Albedo = IN.color.rgb;
        }
        ENDCG
    }

    Fallback "Diffuse"
}
