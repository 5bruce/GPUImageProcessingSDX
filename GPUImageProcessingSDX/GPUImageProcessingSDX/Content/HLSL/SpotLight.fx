/*********************************************************************************************************
 * Author: Paul Demchuk
 * Date: Jan 17, 2013
 * Purpose: Paint the screen red
 *********************************************************************************************************/

Texture2D InputTexture;
SamplerState Sampler;

float2 ImageSize;
float2 LightPos;

float4 PSMain(float2 pos: TEXCOORD, float4 SVP : SV_POSITION) : SV_TARGET {

	float4 pixel = InputTexture.Sample(Sampler, float2(pos.x, pos.y));

	float2 ScreenPos = pos * ImageSize;
	float dist = length(LightPos - ScreenPos);
	if(dist < 200){
		if(dist == 0){
			pixel = float4(1,1,1,1);
		}else{
			pixel *= (200.0f / dist);
		}
	}

	return pixel;

}

technique  {
	pass {
		Profile = 9.3;
		PixelShader = PSMain;
	}
}

