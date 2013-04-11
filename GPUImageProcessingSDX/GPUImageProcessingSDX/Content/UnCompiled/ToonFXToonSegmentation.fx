/*********************************************************************************************************
 * Author: Paul Demchuk
 * Date: Jan 17, 2013
 * Purpose: Not used
 *********************************************************************************************************/

#define vec float
//#define toVec(x) x.rgb
  
 //#define vec vec4
 //#define toVec(x) x.rgba
 
#define s2(a, b)				temp = a; a = min(a, b); b = max(temp, b);
#define mn3(a, b, c)			s2(a, b); s2(a, c);
#define mx3(a, b, c)			s2(b, c); s2(a, c);
 
#define mnmx3(a, b, c)			mx3(a, b, c); s2(a, b);                                   // 3 exchanges
#define mnmx4(a, b, c, d)		s2(a, b); s2(c, d); s2(a, c); s2(b, d);                   // 4 exchanges
#define mnmx5(a, b, c, d, e)	s2(a, b); s2(c, d); mn3(a, c, e); mx3(b, d, e);           // 6 exchanges
#define mnmx6(a, b, c, d, e, f) s2(a, d); s2(b, e); s2(c, f); mn3(a, b, c); mx3(d, e, f); // 7 exchanges

Texture2D InputTexture;
SamplerState Sampler;

float2 textureCoordinate;
float2 leftTextureCoordinate;
float2 rightTextureCoordinate;
 
float2 topTextureCoordinate;
float2 topLeftTextureCoordinate;
float2 topRightTextureCoordinate;
 
float2 bottomTextureCoordinate;
float2 bottomLeftTextureCoordinate;
float2 bottomRightTextureCoordinate;



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

	float3 vrgb[9];
	float3 vhsv[9];

	vrgb[0] = InputTexture.Sample(Sampler, bottomLeftTextureCoordinate).rgb;
	vrgb[1] = InputTexture.Sample(Sampler, topRightTextureCoordinate).rgb;
	vrgb[2] = InputTexture.Sample(Sampler, topLeftTextureCoordinate).rgb;
	vrgb[3] = InputTexture.Sample(Sampler, bottomRightTextureCoordinate).rgb;
	vrgb[4] = InputTexture.Sample(Sampler, leftTextureCoordinate).rgb;
	vrgb[5] = InputTexture.Sample(Sampler, rightTextureCoordinate).rgb;
	vrgb[6] = InputTexture.Sample(Sampler, bottomTextureCoordinate).rgb;
	vrgb[7] = InputTexture.Sample(Sampler, topTextureCoordinate).rgb;
	vrgb[8] = InputTexture.Sample(Sampler, textureCoordinate).rgb;

	vhsv[8] = RGBtoHSV(vrgb[8]);
	vhsv[7] = RGBtoHSV(vrgb[7]);
	vhsv[6] = RGBtoHSV(vrgb[6]);


	vhsv[0] = RGBtoHSV(vrgb[0]);
	vhsv[1] = RGBtoHSV(vrgb[1]);
	vhsv[2] = RGBtoHSV(vrgb[2]);
	vhsv[3] = RGBtoHSV(vrgb[3]);
	vhsv[4] = RGBtoHSV(vrgb[4]);
	vhsv[5] = RGBtoHSV(vrgb[5]);

	vec temp;

	mnmx6(vhsv[0].x, vhsv[1].x, vhsv[2].x, vhsv[3].x, vhsv[4].x, vhsv[5].x);
	mnmx5(vhsv[1].x, vhsv[2].x, vhsv[3].x, vhsv[4].x, vhsv[6].x);
	mnmx4(vhsv[2].x, vhsv[3].x, vhsv[4].x, vhsv[7].x);
	mnmx3(vhsv[3].x, vhsv[4].x, vhsv[8].x);
	float3 finalHSV = vhsv[4];
	//finalHSV.x = vhsv[4].x;
     
	/*Saturation*/
	//median in saturation
	mnmx6(vhsv[0].y, vhsv[1].y, vhsv[2].y, vhsv[3].y, vhsv[4].y, vhsv[5].y);
	mnmx5(vhsv[1].y, vhsv[2].y, vhsv[3].y, vhsv[4].y, vhsv[6].y);   
	mnmx4(vhsv[2].y, vhsv[3].y, vhsv[4].y, vhsv[7].y);     
	mnmx3(vhsv[3].y, vhsv[4].y, vhsv[8].y);
	finalHSV.y = vhsv[4].y;  
	/* if (vhsv[4].y > 0.75)
		finalHSV.y = 1.0;
	else if (vhsv[4].y < .20)
		finalHSV.y = 0.0;
	else finalHSV.y = 0.5; */
     
	/*Value*/
	//median in value
	mnmx6(vhsv[0].z, vhsv[1].z, vhsv[2].z, vhsv[3].z, vhsv[4].z, vhsv[5].z);
	mnmx5(vhsv[1].z, vhsv[2].z, vhsv[3].z, vhsv[4].z, vhsv[6].z);     
	mnmx4(vhsv[2].z, vhsv[3].z, vhsv[4].z, vhsv[7].z);     
	mnmx3(vhsv[3].z, vhsv[4].z, vhsv[8].z);
	finalHSV.z = vhsv[4].z; 


	float3 finalRGB = HSVtoRGB(finalHSV);

	return float4(finalRGB,1);

}

technique  {
	pass {
		Profile = 9.3;
		PixelShader = PSMain;
	}
}