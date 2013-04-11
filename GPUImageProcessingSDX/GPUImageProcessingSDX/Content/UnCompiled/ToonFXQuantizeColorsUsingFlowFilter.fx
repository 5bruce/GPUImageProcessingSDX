/*********************************************************************************************************
 * Author: Paul Demchuk
 * Date: Jan 17, 2013
 * Purpose: Basic "Computer" color. Realistic-ish colors
 *********************************************************************************************************/

Texture2D InputTexture1;
Texture2D InputTexture2;

SamplerState Sampler1;
SamplerState Sampler2;

float ValueBins;
float SaturationBins;
float HueBins;

float3 tanh(in float3 x) {
    float3 y = exp(2.0*x);
	return (y-1.0)/(y+1.0);
}

 // Code based off of HLSL code here: http://chilliant.blogspot.ca/2010/11/rgbhsv-in-hlsl.html
 // Turned into RGB
 float3 hue_to_rgb(in float H)
{
    float R = abs(H * 6.0 - 3.0) - 1.0;
    float G = 2.0 - abs(H * 6.0 - 2.0);
    float B = 2.0 - abs(H * 6.0 - 4.0);
    return clamp(float3(R,G,B), 0.0, 1.0);
}

 float3 HSVtoRGB(float3 HSV) {
     return ((hue_to_rgb(HSV.x) - 1.0) * HSV.y + 1.0) * HSV.z;
}

float3 RGBtoHSV(float3 RGB){
     
	float3 HSV = float3(0.0,0.0,0.0);
    HSV.z = max(RGB.r, max(RGB.g, RGB.b));
    float M = min(RGB.r, min(RGB.g, RGB.b));
    float C = HSV.z - M;
    
    if (C != 0.0)
    {
        // This part can likely be optimized more. Use a matrix?
        HSV.y = C / HSV.z;
        float3 Delta = (HSV.z - RGB) / C;
        Delta.rgb -= Delta.brg;
        Delta.rg += float2(2.0,4.0);
        if (RGB.r >= HSV.z)
            HSV.x = Delta.b;
        else if (RGB.g >= HSV.z)
            HSV.x = Delta.r;
        else
            HSV.x = Delta.g;
        HSV.x = HSV.x / 6.0f;
    }
    return HSV;
     
 }

float4 PSMain(float2 pos: TEXCOORD, float4 SVP : SV_POSITION) : SV_TARGET {

	const float maxStrength = 12.0;
	const float minSteepness = 4.0;
	const float maxSteepness = 8.0;
	const float steepnessRange = maxSteepness - minSteepness;

	float3 binHeights = float3(1.0/HueBins,1.0/SaturationBins,1.0/ValueBins);
	float3 halfBinHeights = binHeights*0.5;

	float4 colorAtPixel = InputTexture1.Sample(Sampler1, pos);

	float3 hsv = RGBtoHSV(colorAtPixel.xyz);

	float strength = InputTexture2.Sample(Sampler2, pos).z * 500.0;

	if(strength >= maxStrength)
		strength = maxStrength;
	else if(strength < 0.0)
		strength = 0.0;
	

	strength = minSteepness + (strength / maxStrength) * steepnessRange;

	float3 closestBorders = min(binHeights*floor(hsv.xyz / binHeights+0.5), 1.0);
	float3 leftovers = (hsv.xyz - closestBorders) / halfBinHeights;
     
	float3 quantizedHSV = float3(closestBorders + halfBinHeights * tanh(leftovers * strength));
	quantizedHSV.y *=1.6;

     
	return float4(HSVtoRGB(quantizedHSV), 1.0);

}

technique  {
	pass {
		Profile = 9.3;
		PixelShader = PSMain;
	}
}