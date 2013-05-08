/*********************************************************************************************************
 * Author: Paul Demchuk
 * Date: Jan 17, 2013
 * Purpose: Take a texture, render it
 *********************************************************************************************************/
Texture2D InputTexture;
SamplerState Sampler;


float4 PSMain(float2 pos: TEXCOORD, float4 SVP : SV_POSITION) : SV_TARGET {

	float4 image = InputTexture.Sample(Sampler, pos);

	return image;

}

technique  {
	pass {
		Profile = 10.0;
		PixelShader = PSMain;
	}
}