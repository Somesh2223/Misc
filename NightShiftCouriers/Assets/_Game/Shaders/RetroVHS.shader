// Retro / VHS camera look (plan 4.17 and M4 art pass): chunky pixels, few colours, scanlines,
// grain, colour fringing, a slight tape wobble and a dark vignette. Used by RetroCameraEffect.
Shader "Hidden/NightShift/RetroVHS"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _PixelSize;
            float _ColorSteps;
            float _Scanlines;
            float _Grain;
            float _Chroma;
            float _Wobble;
            float _Vignette;

            float Random(float2 seed)
            {
                return frac(sin(dot(seed, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;

                // Tape wobble: rows drift sideways a little.
                uv.x += sin(uv.y * 60.0 + _Time.y * 4.0) * _Wobble * 0.0008;

                // Chunky pixels.
                float2 pixels = _ScreenParams.xy / max(_PixelSize, 1.0);
                uv = (floor(uv * pixels) + 0.5) / pixels;

                // Colour fringing: red and blue sampled slightly apart.
                float offset = _Chroma * 0.001;
                float3 colour;
                colour.r = tex2D(_MainTex, uv + float2(offset, 0)).r;
                colour.g = tex2D(_MainTex, uv).g;
                colour.b = tex2D(_MainTex, uv - float2(offset, 0)).b;

                // Few colours.
                colour = floor(colour * _ColorSteps + 0.5) / _ColorSteps;

                // Scanlines.
                float line = 0.5 + 0.5 * sin(i.uv.y * _ScreenParams.y * 3.14159);
                colour *= 1.0 - _Scanlines * line;

                // Film grain.
                colour += (Random(i.uv + frac(_Time.y)) - 0.5) * _Grain;

                // Vignette.
                float2 fromCentre = i.uv - 0.5;
                colour *= 1.0 - dot(fromCentre, fromCentre) * _Vignette;

                return fixed4(colour, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
