/*********************************************************************************************************
 * Author: Paul Demchuk
 * Date: Jan 17, 2013
 * Purpose: Calculate the intensity (L) of the texture, output in the red channel. If you look at the
 *          output of this pixel shader and it is all red - that is NORMAL. the L is saved in r channel
 *********************************************************************************************************/

Texture2D InputTexture;
SamplerState Sampler;

float4 PSMain(float2 pos: TEXCOORD, float4 SVP : SV_POSITION) : SV_TARGET {

	float3 c = InputTexture.Sample(Sampler, pos).rgb;
     
     float L;
     
     //all aboard the magic number express
     float labT32f = 0.008856;
     float labLScale32f = 116.0;
     float labLShift32f = 16.0;
     float labLScale232f =903.3;
     
     float y;
     
     float3 y_coef = float3(0.212671,0.715160,0.072169);
     
     y = dot(c,y_coef);
     
     
     if(y>labT32f)
     {
		 //for some reason pow doesnt work on negative numbers. So if y is negative, find
		 //abs(y) ^ (1/3), and then multiply it by 1 to get the negative power.
		 if(y >= 0){
			y=pow(abs(y),1.0/3.0);
		 }else{
			 y = pow(abs(y), 1.0/3.0) * -1;
		 }
         L = y*labLScale32f - labLShift32f;
     }
     else 
     {
         L = y*labLScale232f;
         y=y*labLScale232f;
     }
     
     return float4(L/100.0,0.0,0.0, 1.0);

}

technique  {
	pass {
		Profile = 9.3;
		PixelShader = PSMain;
	}
}