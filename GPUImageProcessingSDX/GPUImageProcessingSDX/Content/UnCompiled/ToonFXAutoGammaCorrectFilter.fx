/*********************************************************************************************************
 * Author: Paul Demchuk
 * Date: Jan 17, 2013
 * Purpose: Makes the picture sharper & brighter
 *********************************************************************************************************/

Texture2D InputTexture;
SamplerState Sampler;

 float4 PSMain(float2 pos: TEXCOORD, float4 SVP : SV_POSITION) : SV_TARGET {

     float4 rgba = InputTexture.Sample(Sampler, pos);     
     
     rgba.r = rgba.r<0.018? 4.5*rgba.r : 1.099*pow(abs(rgba.r),0.45) - 0.099;
     rgba.g = rgba.g<0.018? 4.5*rgba.g : 1.099*pow(abs(rgba.g),0.45) - 0.099;
     rgba.b = rgba.b<0.018? 4.5*rgba.b : 1.099*pow(abs(rgba.b),0.45) - 0.099;
     
     return rgba;
 }

technique  {
	pass {
		Profile = 9.3;
		PixelShader = PSMain;
	}
}