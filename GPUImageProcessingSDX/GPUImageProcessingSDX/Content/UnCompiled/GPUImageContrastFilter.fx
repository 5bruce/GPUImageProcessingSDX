/*********************************************************************************************************
 * Author: Paul Demchuk
 * Date: Jan 17, 2013
 * Purpose: Add the specified contrast to a texture
 *********************************************************************************************************/
Texture2D InputTexture;
SamplerState Sampler;

float contrast;

 float4 PSMain(float2 pos: TEXCOORD, float4 SVP : SV_POSITION) : SV_TARGET {

     float4 rgba = InputTexture.Sample(Sampler, pos);     
     
     return float4(((rgba.rgb - float3(0.5,0.5,0.5)) * contrast + float3(0.5,0.5,0.5)), rgba.w);
     
 }

technique  {
	pass {
		Profile = 9.3;
		PixelShader = PSMain;
	}
}