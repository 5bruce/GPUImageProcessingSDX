/*********************************************************************************************************
 * Author: Paul Demchuk
 * Date: Jan 17, 2013
 * Purpose: Take a texture, render it
 *********************************************************************************************************/
Texture2D InputTexture;
SamplerState Sampler;

float frequency;
float phase;
float amplitude;
float spread;
float2 center;

float4 PSMain(float4 pos : SV_POSITION,
    float4 posScene : SCENE_POSITION,
    float4 uv0      : TEXCOORD0
    ) : SV_Target
{
    float2 wave;

    float2 toPixel = posScene.xy - center; 

    float distance = length(toPixel) * uv0.z;
    float2 direction = normalize(toPixel);

    sincos(frequency * distance + phase, wave.x, wave.y);

    // Clamps the distance between 0 and 1 and squares the value.
    float falloff = saturate(1 - distance);
    falloff = pow(falloff, 1.0f / spread);

    // Calculates new mapping coordinates based on the frequency, center, and amplitude.
    float2 uv2 = uv0.xy + (wave.x * falloff * amplitude) * direction * uv0.zw;

    float lighting = lerp(1.0f, 1.0f + wave.x * falloff * 0.2f, saturate(amplitude / 20.0f));
	float4 color;

    // Resamples the image based on the new coordinates.
    color = InputTexture.Sample(Sampler, uv2);
    color.rgb *= lighting;
    
    return color;
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