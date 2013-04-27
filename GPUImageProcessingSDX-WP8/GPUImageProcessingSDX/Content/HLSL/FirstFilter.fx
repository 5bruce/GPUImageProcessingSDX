/*********************************************************************************************************
 * Author: Paul Demchuk
 * Date: Jan 17, 2013
 * Purpose: Paint the screen red
 *********************************************************************************************************/

float4 PSMain(float2 pos: TEXCOORD, float4 SVP : SV_POSITION) : SV_TARGET {

	return float4(1,0,0,1);

}

technique  {
	pass {
		Profile = 9.3;
		PixelShader = PSMain;
	}
}

