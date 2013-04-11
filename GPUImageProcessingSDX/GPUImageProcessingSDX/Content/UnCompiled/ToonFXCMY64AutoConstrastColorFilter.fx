﻿/*********************************************************************************************************
 * Author: Paul Demchuk
 * Date: Jan 17, 2013
 * Purpose: Makes all the color super bright and in your face
 *********************************************************************************************************/

Texture2D InputTexture;
SamplerState Sampler;


 float tanh(in float x) {
    float y = exp(2.0*x);
	return (y-1.0)/(y+1.0);
}

  void offsetTanH (in float ci, out float val){
     //given ci in [0, .25], generate output value in [0,.25]
     ci= ci*400.0; //now ci in [1,100]
     //for ci=1:100
     float cx=((ci-50.0)/100.0) ;   //for ci in 1 to 100, make cx = -.5 to .5
     float alpha = 0.125;  //half of interval of tanh for offsettting
     float c=  alpha*tanh(cx/alpha);
     
     
     val = c +alpha;
     // end
 }

 float myQuarterClamp(in float c){
     
     float cx = c;
     /*Smooth Quantization*/
     if (cx>= 0.0 && cx <= 0.25 ){
         offsetTanH(cx,c);
     }
     else if (cx > 0.25 && cx <= 0.5){
         offsetTanH(cx-0.25,c);
         c = 0.25+c;
     }
     else if (cx > 0.5 && cx <= 0.75){
         offsetTanH(cx-0.5, c);
         c =0.5  +c;
     }
     else if  (cx > 0.75 && cx <= 1.0){
         offsetTanH(cx-0.75,c); c =0.75+ c;
     }
     else c = 1.0; //errror state really
     
     return c;
     
     
 }

 float3 CMYBin(float3 rgbIn){
	 float cmy_c = 1.0 - rgbIn.x;
	 float cmy_m = 1.0 - rgbIn.y;
	 float cmy_y = 1.0 - rgbIn.z;

	 float minCMY = min(min(cmy_c,cmy_m),cmy_y);

	 float maxCMY = 1.0;

	 cmy_c = (cmy_c - minCMY) / (maxCMY - minCMY) ;
     cmy_m = (cmy_m - minCMY) / (maxCMY - minCMY) ;
     cmy_y = (cmy_y - minCMY) / (maxCMY - minCMY) ;

	 cmy_c = myQuarterClamp(cmy_c);
     cmy_m = myQuarterClamp(cmy_m);
     cmy_y = myQuarterClamp(cmy_y);

	 return float3(1.0-cmy_c, 1.0-cmy_m, 1.0-cmy_y);

 }

float4 PSMain(float2 pos: TEXCOORD, float4 SVP : SV_POSITION) : SV_TARGET {

	float4 finalRGBQuantized = InputTexture.Sample(Sampler, pos);

	float3 newRGB = CMYBin(finalRGBQuantized.xyz);

	return float4(newRGB,1);

}

technique  {
	pass {
		Profile = 9.3;
		PixelShader = PSMain;
	}
}