// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Projector' with 'unity_Projector'

Shader "FogOfWar" {

    Properties{
       _MainTex("MainTex", 2D) = "white" {}
       _Color("Color", Color) = (1,1,1,1)
    }
        SubShader{

           Pass {
              Blend One OneMinusSrcAlpha
              // add color of _MainTex to the color in the framebuffer 
           ZWrite Off // don't change depths
           Offset -1, -1 // avoid depth fighting 

           CGPROGRAM

           #pragma vertex vert  
           #pragma fragment frag 

            float4 _Color;
        float _Opacity;

        // User-specified properties
        uniform sampler2D _MainTex;

        // Projector-specific uniforms
        uniform float4x4 unity_Projector; // transformation matrix 
        // from object space to projector space 

         uniform float _PointAlpha;

      struct vertexInput {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
     };
     struct vertexOutput {
        float4 pos : SV_POSITION;
        float4 posProj : TEXCOORD0;
         float3 norm : NORMAL;
         // position in projector space
   };

   vertexOutput vert(vertexInput input)
   {
      vertexOutput output;

      output.posProj = mul(unity_Projector, input.vertex);
      output.pos = UnityObjectToClipPos(input.vertex);
      output.norm = UnityObjectToClipPos(input.normal);
      return output;
   }


   float4 frag(vertexOutput input) : COLOR
   {

    float4 textureColor = tex2D(_MainTex, input.posProj.xy / input.posProj.w);
           textureColor.r = 1.0 - textureColor.r;
           textureColor.g = 1.0 - textureColor.g;
           textureColor.b = 1.0 - textureColor.b;
           textureColor.a = textureColor.a;


      if (input.posProj.w > 0.0)// in front of projector?
      {
          // if (input.norm.y <= 0)
          //     textureColor = float4(0.0, 0.0, 0.0, 0.0);

           textureColor *= _Color;

         return textureColor;

       }
       else // behind projector
       {
          return float4(0.0, 0.0, 0.0, 0.0);
       }
    }

    ENDCG
 }
       }
           // The definition of a fallback shader should be commented out 
           // during development:
           // Fallback "Projector/Light"
}