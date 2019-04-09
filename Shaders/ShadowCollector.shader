Shader "Custom/ShadowCollector" {
	Properties {
		_ShadowTex ("ShadowMap", 2D) = "White"
		_ShadowStrength("Shadow Strength", float) = 0.8
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
        #include "UnityCG.cginc"
		#pragma surface surf Lambert fullforwardshadows vertex:vert
        
        sampler2D _ShadowTex;
		fixed _ShadowStrength;
        float4x4 _ViewMatrix;
		float4x4 _ProjectionMatrix;

        
        struct Input {
            float4 shadowCoords;
			float3 vertColor;
		};

        void vert(inout appdata_full v, out Input o) {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            float3 worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz;

			float4x4 biasMatrix = {0.5, 0, 0, 0.5,
										   0, 0.5, 0, 0.5,
										   0, 0, 0.5, 0.5,
										   0, 0, 0, 1};

            o.shadowCoords = mul( mul(mul(biasMatrix, _ProjectionMatrix), _ViewMatrix), float4(worldPos, 1.0));
			o.vertColor = v.color;
        }

		void surf(Input IN, inout SurfaceOutput o)
        {	
			//float shadowStrength = clamp(1.0 - tex2Dproj(_ShadowTex, IN.shadowCoords).r, _ShadowStrength, 1.0);
			float shadowStrength = 1.0 - tex2Dproj(_ShadowTex, IN.shadowCoords).r;

			o.Albedo = IN.vertColor * shadowStrength;
		}
		ENDCG
	}
	FallBack "Diffuse"
}