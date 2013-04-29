/*********************************************************************************************************
 * Author: Paul Demchuk
 * Date: Jan 17, 2013
 * Purpose: Take a texture, render it
 *********************************************************************************************************/
Texture2D InputTexture;
SamplerState Sampler;

float fractionalWidthOfPixel = 0.01f;
float aspectRatio = 1.0f;
float dotScaling = 0.9f;

float4 PSMain(float2 pos: TEXCOORD, float4 SVP : SV_POSITION) : SV_TARGET {

	float2 sampleDivisor = float2(fractionalWidthOfPixel, fractionalWidthOfPixel / aspectRatio);
     
	float2 samplePos = pos - fmod(pos, sampleDivisor) + 0.5 * sampleDivisor;
	float2 textureCoordinateToUse = float2(pos.x, (pos.y * aspectRatio + 0.5 - 0.5 * aspectRatio));
	float2 adjustedSamplePos = float2(samplePos.x, (samplePos.y * aspectRatio + 0.5 - 0.5 * aspectRatio));
	float distanceFromSamplePoint = distance(adjustedSamplePos, textureCoordinateToUse);
	float checkForPresenceWithinDot = step(distanceFromSamplePoint, (fractionalWidthOfPixel * 0.5) * dotScaling);

	return float4(InputTexture.Sample(Sampler, samplePos ).rgb * checkForPresenceWithinDot, 1.0);

}

technique  {
	pass {
		Profile = 10.0;
		PixelShader = PSMain;
	}
}