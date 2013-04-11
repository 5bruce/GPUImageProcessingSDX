/*********************************************************************************************************
 * Author: Paul Demchuk
 * Date: Jan 17, 2013
 * Purpose: Blend the color and B&W Images by multiplication. 
 *********************************************************************************************************/

Texture2D InputTexture1;
SamplerState Sampler;

Texture2D InputTexture2;
SamplerState Sampler2;

float4 PSMain(float2 pos: TEXCOORD, float4 SVP : SV_POSITION) : SV_TARGET {

	float4 base = InputTexture1.Sample(Sampler, pos);
	float4 overlayer = InputTexture2.Sample(Sampler2, pos);
	
	return overlayer * base + overlayer * (1.0 - base.a) + base * (1.0 - overlayer.a);

}

technique  {
	pass {
		Profile = 9.3;
		PixelShader = PSMain;
	}
}