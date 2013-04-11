/*********************************************************************************************************
 * Author: Paul Demchuk
 * Date: Jan 17, 2013
 * Purpose: i don't really know what this one does... :)
 *********************************************************************************************************/

Texture2D InputTexture1;
Texture2D InputTexture2;

SamplerState Sampler1;
SamplerState Sampler2;

float texelWidthOffset; 
float texelHeightOffset;
 
float sigma_s; // Spatial gaussian standard deviation
float sigma_t; // Tonal gaussian standard deviation

bool dir;

float4 PSMain(float2 pos: TEXCOORD, float4 SVP : SV_POSITION) : SV_TARGET {

	float halfWidth = max(1.0f,2.0f*sigma_s);
	float twoSigmaSSquaredInverse = -1.0f/(2.0f*sigma_s*sigma_s);
	float twoSigmaTSquaredInverse = -1.0f/(2.0f*sigma_t*sigma_t);
	
	float2 flow_direction = InputTexture2.Sample(Sampler2, pos).xy;
    flow_direction = flow_direction*2.0-float2(1.0,1.0);
    
    float3 c = InputTexture1.Sample(Sampler1, pos).rgb;
    
    float2 bilateral_direction;
	if(dir) bilateral_direction = float2(-flow_direction.y, flow_direction.x);
	else bilateral_direction = flow_direction;
    
    bilateral_direction *= float2(texelWidthOffset, texelHeightOffset);
    
    float3 sum = c;
    
    float weight_sum = 1.0;
    
    float r;
    
	[unroll(4)]
    for(r=1.0;r<=halfWidth;r+=1.0)
    {
        float r2 = r*r;
        float spatial_weight = exp(r2*twoSigmaSSquaredInverse);
        
        float3 c1 = InputTexture1.Sample(Sampler1, pos+r*bilateral_direction).rgb;
        float3 c2 = InputTexture1.Sample(Sampler1, pos-r*bilateral_direction).rgb;
        float2 tonal_differences = float2(length(c-c1), length(c-c2))*100.0;
        
        float2 tonal_weights = exp(tonal_differences*tonal_differences*twoSigmaTSquaredInverse);
        
        tonal_weights *= spatial_weight;
        weight_sum += tonal_weights.x+tonal_weights.y;
        
        sum += tonal_weights.x*c1+tonal_weights.y*c2;
    }
    
    sum/=weight_sum;
    
    // Pack the results into two channels each to maintain accuracy
   return float4(sum, 1.0);
    
    

}

technique  {
	pass {
		Profile = 9.3;
		PixelShader = PSMain;
	}
}