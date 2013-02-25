/*********************************************************************************************************
 * Author: Paul Demchuk
 * Date: Jan 17, 2013
 * Purpose: Take a texture, render it
 *********************************************************************************************************/
Texture2D InputTexture;
SamplerState Sampler;

float4 PSMain(float2 pos: TEXCOORD, float4 SVP : SV_POSITION) : SV_TARGET {


	float3 color = InputTexture.Sample(Sampler, pos).rgb;

	if(pos.x > 0.3 && pos.x < 0.7 && pos.y > 0.3 && pos.y < 0.7)
		return float4(color * 1.1,1);
	
	return float4(color,1);

}

technique  {
	pass {
		Profile = 9.3;
		PixelShader = PSMain;
	}
}

/*
1 0 0   -   0

1 1 0   -   1/4

0 1 0   -   1/2

0 1 1   -   3/4

0 0 1   -   1

1 0 1