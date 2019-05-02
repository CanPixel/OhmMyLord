Shader "Sprite (Vertex Lit)"
{
	Properties
	{
		[PerRendererData] _MainTex ("Main Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
		
		_BumpScale("Scale", Float) = 1.0
		_BumpMap ("Normal Map", 2D) = "bump" {}
		
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
		[PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
		[PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
		
		_Radius ("_Radius", Range (0, 0.5)) = 0.1
		_scale ("_scale", Vector) = (0.0, 0.0,0.0)
		[MaterialToggle] _TR ("_TopRightCorner", Float) = 1
		[MaterialToggle] _BR ("_BottomRightCorner", Float) = 1
		[MaterialToggle] _BL ("_BottomLeftCorner", Float) = 1
		[MaterialToggle] _TL ("_TopLeftCorner", Float) = 1

		[MaterialToggle] _Invert ("_Invert", Float) = 0

		_CutoffA ("Alpha Cutoff", Range (0,1)) = 0.5


		_EmissionColor("Color", Color) = (0,0,0,0)
		_EmissionMap("Emission", 2D) = "white" {}
		_EmissionPower("Emission Power", Float) = 1.0	
		
		_Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
		_GlossMapScale("Smoothness Scale", Range(0.0, 1.0)) = 1.0
		[Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
		_MetallicGlossMap("Metallic", 2D) = "white" {}
		
		_DiffuseRamp ("Diffuse Ramp Texture", 2D) = "gray" {}
		
		_FixedNormal ("Fixed Normal", Vector) = (0,0,1,1)
		_ZWrite ("Depth Write", Float) = 0.0
		_Cutoff ("Depth alpha cutoff", Range(0,1)) = 0.0
		_ShadowAlphaCutoff ("Shadow alpha cutoff", Range(0,1)) = 0.1
		_CustomRenderQueue ("Custom Render Queue", Float) = 0.0
		
		_OverlayColor ("Overlay Color", Color) = (0,0,0,0)
		_Hue("Hue", Range(-0.5,0.5)) = 0.0
		_Saturation("Saturation", Range(0,2)) = 1.0	
		_Brightness("Brightness", Range(0,2)) = 1.0	
		
		_RimPower("Rim Power", Float) = 2.0	
		_RimColor ("Rim Color", Color) = (1,1,1,1)
		
		_BlendTex ("Blend Texture", 2D) = "white" {}
		_BlendAmount ("Blend", Range(0,1)) = 0.0
		
		[HideInInspector] _SrcBlend ("__src", Float) = 1.0
		[HideInInspector] _DstBlend ("__dst", Float) = 0.0
		[HideInInspector] _RenderQueue ("__queue", Float) = 0.0
		[HideInInspector] _Cull ("__cull", Float) = 0.0
	}
	
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Sprite" "AlphaDepth"="False" "CanUseSpriteAtlas"="True" "IgnoreProjector"="True" }
		LOD 150
		ZWrite Off
		Cull Off
		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile DUMMY PIXELSNAP_ON
			#include "UnityCG.cginc"

			uniform half _TR;
			uniform half _BR;
			uniform half _BL;
			uniform half _TL;

			uniform half2 _scale;
			uniform half _Invert;

			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
			};
 
			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				half2 texcoord  : TEXCOORD0;
				half2 mask : TEXCOORD1; 
			};

			float4 _MainTex_ST;
			fixed4 _Color;
			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = IN.texcoord;

				OUT.color = fixed4(1,1,1,1);

				#ifdef PIXELSNAP_ON
				OUT.vertex = UnityPixelSnap (OUT.vertex);
				#endif

				OUT.mask = TRANSFORM_TEX( IN.texcoord, _MainTex );
				return OUT;
			}
			sampler2D _MainTex;
			float _Radius;
			fixed4 frag(v2f IN) : SV_Target
			{
				half4 m = tex2D(_MainTex, IN.mask) * _Color;
				half4 c = half4(0.0,0.0,0.0,0.0);

				if (_Invert == 1) {}
				else
				{
					c = IN.color;
				}

				half dist = 0;

				half2 center = half2(0,0);

				half2 _TRC = half2(1,1);
				half2 _BRC = half2(1,0);
				half2 _BLC = half2(0,0);
				half2 _TLC = half2(0,1);

				half sfx0 = 1;
				half sfx1 = 1;
				half sfy0 = 1;
				half sfy1 = 1;

				if (_scale.x > _scale.y)
				{
					sfx0  = _scale.y/_scale.x;
					sfx1  = (_scale.y/ pow(_scale.x,2));
				}
				else
				{
					sfy0  = _scale.x/_scale.y;
					sfy1  = (_scale.x/ pow(_scale.y,2));
				}

				center = half2((_TRC.x - (_Radius * sfx0)),(_TRC.y - (_Radius * sfy0) ));
				if (_TR == 1 )
				{

					if (
						center.x < IN.texcoord.x
						&& center.y < IN.texcoord.y
						)
					{
						dist = sqrt( 
									pow( center.x - IN.texcoord.x , 2 ) / sfx1
									+ 
									pow( center.y - IN.texcoord.y , 2 ) / sfy1
									);

						if (dist > _Radius)
						{
							if (_Invert == 1 )
							{
								c = IN.color;
							}
							else
							{
								c = half4(0,0,0,0);
							}
						}
					}
				}

				center = half2((_BRC.x - (_Radius * sfx0) ),(_BRC.y + (_Radius * sfy0) ));
				if (_BR == 1 )
				{
					if (
						center.x < IN.texcoord.x
						&& center.y > IN.texcoord.y
						)
					{
						dist = sqrt( 
									pow( center.x - IN.texcoord.x , 2 ) / sfx1
									+ 
									pow( center.y - IN.texcoord.y , 2 ) / sfy1
									);

						if (dist > _Radius)
						{
							if (_Invert == 1 )
							{
								c = IN.color;
							}
							else
							{
								c = half4(0,0,0,0);
							}
						}
					}
				}

				center = half2((_BLC.x + (_Radius * sfx0) ),(_BLC.y + (_Radius * sfy0) ));
				if (_BL == 1 )
				{
					if (
						center.x > IN.texcoord.x
						&& center.y > IN.texcoord.y
						)
					{
						dist = sqrt( 
									pow( center.x - IN.texcoord.x , 2 ) / sfx1
									+ 
									pow( center.y - IN.texcoord.y , 2 ) / sfy1
									);

						if (dist > _Radius)
						{
							if (_Invert == 1 )
							{
								c = IN.color;
							}
							else
							{
								c = half4(0,0,0,0);
							}
						}
					}
				}

				center = half2((_TLC.x + (_Radius * sfx0) ),(_TLC.y - (_Radius * sfy0) ));
				if (_TL == 1 )
				{
					if (
						center.x > IN.texcoord.x
						&& center.y < IN.texcoord.y
						)
					{
						dist = sqrt( 
									pow( center.x - IN.texcoord.x , 2 ) / sfx1
									+ 
									pow( center.y - IN.texcoord.y , 2 ) / sfy1
									);

						if (dist > _Radius)
						{
							if (_Invert == 1 )
							{
								c = IN.color;
							}
							else
							{
								c = half4(0,0,0,0);
							}
						}
					}
				}

				c = lerp(c,m,c.a);

				return c;
			}
		ENDCG
		}
		Pass
		{
			Name "Vertex" 
			Tags { "LightMode" = "Vertex" }
			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]
			ZTest LEqual
			Cull [_Cull]
			Lighting On
			
			CGPROGRAM
				#pragma target 3.0
				
				#pragma shader_feature _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON _ADDITIVEBLEND _ADDITIVEBLEND_SOFT _MULTIPLYBLEND _MULTIPLYBLEND_X2
				#pragma shader_feature _ _FIXED_NORMALS_VIEWSPACE _FIXED_NORMALS_VIEWSPACE_BACKFACE _FIXED_NORMALS_MODELSPACE  _FIXED_NORMALS_MODELSPACE_BACKFACE
				#pragma shader_feature _ _SPECULAR _SPECULAR_GLOSSMAP
				#pragma shader_feature _NORMALMAP
				#pragma shader_feature _ALPHA_CLIP
				#pragma shader_feature _EMISSION
				#pragma shader_feature _DIFFUSE_RAMP
				#pragma shader_feature _COLOR_ADJUST
				#pragma shader_feature _RIM_LIGHTING
				#pragma shader_feature _TEXTURE_BLEND
				#pragma shader_feature _SPHERICAL_HARMONICS
				#pragma shader_feature _FOG
				
				#pragma fragmentoption ARB_precision_hint_fastest
				#pragma multi_compile_fog
				#pragma multi_compile _ PIXELSNAP_ON
				#pragma multi_compile _ ETC1_EXTERNAL_ALPHA
				
				#pragma vertex vert
				#pragma fragment frag
				
				#include "CGIncludes/SpriteVertexLighting.cginc"	
			ENDCG
		}
	}
	
	FallBack "Sprite (Unlit)"
	CustomEditor "SpriteShaderGUI"
}
