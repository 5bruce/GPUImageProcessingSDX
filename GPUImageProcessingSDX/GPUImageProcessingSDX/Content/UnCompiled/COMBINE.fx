/*********************************************************************************************************
 * Author: Paul Demchuk
 * Date: Jan 17, 2013
 * Purpose: Simply take two textures and combine them
 *********************************************************************************************************/

Texture2D InputTexture;
SamplerState Sampler;

Texture2D InputTexture2;
SamplerState Sampler2;

float4 PSMain(float2 pos: TEXCOORD, float4 SVP : SV_POSITION) : SV_TARGET {

	float4 image = InputTexture.Sample(Sampler, pos);
	float4 image2 = InputTexture2.Sample(Sampler2, pos);
	if(length(image.rgb) < 0.02) return float4(image.rgb,1);
	return float4((image + image2).rgb,1);

}

technique  {
	pass {
		Profile = 9.3;
		PixelShader = PSMain;
	}
}