/*********************************************************************************************************
 * Author: Paul Demchuk
 * Date: Jan 17, 2013
 * Purpose: Paint the screen red
 *********************************************************************************************************/

Texture2D InputTexture;
SamplerState Sampler;

float4 PSMain(float2 pos: TEXCOORD, float4 SVP : SV_POSITION) : SV_TARGET {

	float4 pixel = InputTexture.Sample(Sampler, float2(pos.x, 1-pos.y));

	return pixel;

}

technique  {
	pass {
		Profile = 9.3;
		PixelShader = PSMain;
	}
}

