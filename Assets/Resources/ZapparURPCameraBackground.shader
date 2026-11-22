// Shader background kamera Zappar yang kompatibel dengan URP (Unity 6 / RenderGraph).
// Digambar oleh ZapparURPBackground.cs sebagai quad layar penuh di antrean "Background",
// sehingga gambar kamera HP tampil di belakang semua objek 3D.
Shader "Zappar/URPCameraBackground"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
        _LinearizeInput ("Linearize Input", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" "IgnoreProjector"="True" "PreviewType"="Plane" }

        Pass
        {
            Name "ZapparCameraBackground"
            Cull Off
            ZWrite Off
            ZTest Always

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4x4 _nativeTextureMatrix;
            float _LinearizeInput;

            v2f vert (appdata v)
            {
                v2f o;
                // Abaikan matriks kamera: quad (0..1) langsung dipetakan ke seluruh layar.
                float2 ndc = v.uv * 2.0 - 1.0;
                ndc.y *= _ProjectionParams.x; // ikuti flip render target (D3D/Vulkan/Metal)
                o.vertex = float4(ndc, 0.5, 1.0);
                o.uv = mul(_nativeTextureMatrix, float4(v.uv, 0, 1)).xy;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                // Tekstur kamera berisi warna sRGB; di Linear color space perlu dikonversi
                // agar tidak terlihat pucat/terlalu terang.
                if (_LinearizeInput > 0.5)
                    col.rgb = GammaToLinearSpace(col.rgb);
                col.a = 1.0;
                return col;
            }
            ENDCG
        }
    }
}
