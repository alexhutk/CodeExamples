Shader "Custom/CameraBlitShader" {
    Properties {
        _MainTex ("Screen Texture", 2D) = "white" {}
    }
    SubShader {
    Pass {
       
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #include "UnityCG.cginc"
 
        sampler2D _MainTex;
        float4 _MainTex_TexelSize;
		half _BlurDev;
		float3x3 GM = float3x3
			( 1.0,2.0,1.0,
			  2.0,4.0,2.0,
			  1.0,2.0,1.0
			);
 
        struct v2f {
            float4 vertex : SV_POSITION;
            float2 uv : TEXCOORD0;
        };
      
        v2f vert (appdata_img v) {
            v2f o;
          
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = v.texcoord;
           
            return o;
        }
       
        fixed4 frag (v2f i) : COLOR
        {
            fixed tex_screen = tex2D(_MainTex, i.uv).r;

			fixed4 res = {tex_screen > 0.2 ? 1.0 : 0.0, 0, 0, 1};

            return res;
        }
       
        ENDCG
        }
    }
   
}