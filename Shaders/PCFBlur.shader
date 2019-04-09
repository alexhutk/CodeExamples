Shader "Custom/PCFBlur" {
    Properties {
        _MainTex ("Screen Texture", 2D) = "white" {}
		_BlurDev("Avarage_Koef", Range(0,1000)) = 150
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

		float PCF (sampler2D shadowMap, float2 uv)
		{
			float result = 0.0;

			for(int x=-3; x<=3;x++)
				for(int y=-3;y<=3;y++)
				{
					float2 offset = float2(x,y) * _MainTex_TexelSize;
					result += tex2D(shadowMap,uv+offset).r;
				}

			return result/_BlurDev;
		}

        v2f vert (appdata_img v) {
            v2f o;
          
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = v.texcoord;
           
            return o;
        }
       
        fixed4 frag (v2f i) : COLOR
        {
            fixed tex_screen = tex2D(_MainTex, i.uv).r;

			fixed4 res = {0, 0, 0, 1};

			if(tex_screen > 0.17)
				res.r = PCF(_MainTex, i.uv);

            return res;
        }
       
        ENDCG
        }
    }
   
}