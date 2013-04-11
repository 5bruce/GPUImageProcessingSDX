/*********************************************************************************************************
 * Author: Paul Demchuk
 * Date: Jan 17, 2013
 * Purpose: Not currently used. But will skew/stretch an image to fit a shape
 *********************************************************************************************************/

Texture2D InputTexture;

SamplerState Sampler;

float hue_tx_r;
float hue_tx_g;
float hue_tx_b;
float hue_rt;
float hue_blend;

float3 tanh(in float3 x) {
    float3 y = exp(2.0*x);
	return (y-1.0)/(y+1.0);
}

 float3 HSVtoRGB(float3 HSV) {
    float3 RGB = float3(HSV.z,HSV.z,HSV.z);
    if ( HSV.y != 0.0 ) {
        float var_h = HSV.x * 6.0;
        float var_i = floor(var_h);   // Or ... var_i = floor( var_h )
        float var_1 = HSV.z * (1.0 - HSV.y);
        float var_2 = HSV.z * (1.0 - HSV.y * (var_h-var_i));
        float var_3 = HSV.z * (1.0 - HSV.y * (1.0-(var_h-var_i)));
        if      (var_i == 0.0) { RGB = float3(HSV.z, var_3, var_1); }
        else if (var_i == 1.0) { RGB = float3(var_2, HSV.z, var_1); }
        else if (var_i == 2.0) { RGB = float3(var_1, HSV.z, var_3); }
        else if (var_i == 3.0) { RGB = float3(var_1, var_2, HSV.z); }
        else if (var_i == 4.0) { RGB = float3(var_3, var_1, HSV.z); }
        else                   { RGB = float3(HSV.z, var_1, var_2); }
    }
    
	return RGB;
}

float3 RGBtoHSV(float3 RGB){
     
     float3 HSV = float3(0.0,0.0,0.0);
     float minVal =min(RGB.r, min(RGB.g, RGB.b));
     float maxVal = max(RGB.r, max(RGB.g, RGB.b));
     float delta = maxVal - minVal;             //Delta RGB value
     HSV.z = maxVal;
     if (delta != 0.0) {                    // If gray, leave H & S at zero
         HSV.y = delta / maxVal;
         float3 delRGB;
         delRGB = ( ( ( maxVal  - RGB ) / 6.0 ) + ( delta / 2.0 ) ) / delta;
         if      ( RGB.x == maxVal ) HSV.x = delRGB.z - delRGB.y;
         else if ( RGB.y == maxVal ) HSV.x = ( 1.0/3.0) + delRGB.x - delRGB.z;
         else if ( RGB.z == maxVal ) HSV.x = ( 2.0/3.0) + delRGB.y - delRGB.x;
         if ( HSV.x < 0.0 ) { HSV.x += 1.0; }
         if ( HSV.x > 1.0 ) { HSV.x -= 1.0; }
         
     }

	 return HSV;
     
 }

float4 PSMain(float2 pos: TEXCOORD, float4 SVP : SV_POSITION) : SV_TARGET {

	float4 colorAtPixel = InputTexture.Sample(Sampler, pos);

	float3 hsv = RGBtoHSV(colorAtPixel.xyz);
     
    hsv.x = hsv.x+ hue_rt;
    if (hsv.x > 1.0)
		hsv.x =  hsv.x-1.0;
	else if (hsv.x < 0.0)
		hsv.x = 1.0+hsv.x; 
	float3 newrgb = HSVtoRGB(hsv);
	//blend between the original color and a color scale on each of the channels (ie hue_tx *newrgb )
	newrgb.x = ((1.0-hue_blend)*newrgb.x)+((hue_blend)* hue_tx_r*newrgb.x);
	newrgb.y = ((1.0-hue_blend)*newrgb.y)+((hue_blend)*hue_tx_g*newrgb.y);
	newrgb.z = ((1.0-hue_blend)*newrgb.z)+((hue_blend)*hue_tx_b*newrgb.z);

	return float4(newrgb,1);

}

technique  {
	pass {
		Profile = 9.3;
		PixelShader = PSMain;
	}
}