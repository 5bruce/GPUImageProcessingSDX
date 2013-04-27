/*********************************************************************************************************
 * Author: Paul Demchuk
 * Date: Jan 17, 2013
 * Purpose: Take a texture, render it
 *********************************************************************************************************/
uniform Texture2D InputTexture;
SamplerState Sampler;

//0 = portrait (DEFAULT)
//1 = landscape left (counter clockwise)
//2 = landscape right (clockwise)
uint Orientation;

float4 PSMain(float2 pos: TEXCOORD, float4 SVP : SV_POSITION) : SV_TARGET {

	float2 orientation = pos;
	
	switch(Orientation){
		case 1: orientation = float2(pos.y, 1.0f - pos.x); break;
		case 2: orientation = float2(pos.y, pos.x); break;
	}
	
	
	float4 image = InputTexture.Sample(Sampler, orientation);

	return image;

}

technique  {
	pass {
		Profile = 9.3;
		PixelShader = PSMain;
	}
}