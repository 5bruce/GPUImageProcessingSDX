/*********************************************************************************************************
 * Author: Paul Demchuk
 * Date: Jan 17, 2013
 * Purpose: Currently not used
 *********************************************************************************************************/

Texture2D InputTexture1;
Texture2D InputTexture2;

SamplerState Sampler1;
SamplerState Sampler2;

float texelWidthOffset; 
float texelHeightOffset;
 
float sigma_s; // Spatial gaussian standard deviation
float sigma_t; // Tonal gaussian standard deviation
float halfWidth;
float twoSigmaSSquaredInverse;
float twoSigmaTSquaredInverse;

float4 PSMain(float2 pos: TEXCOORD, float4 SVP : SV_POSITION) : SV_TARGET {

	
	float2 texelOffset = float2(texelWidthOffset, texelHeightOffset);
    float2 dir_at_pixel = InputTexture2.Sample(Sampler2, pos).xy;
     
     dir_at_pixel = dir_at_pixel*2.0-float2(1.0,1.0);
     
     float2 current_dir1 = dir_at_pixel; // current direction in the positive movement
     float2 current_dir2 = -dir_at_pixel; // current direction in the opposite movement 
     
     float2 pos1 = pos;
     float2 pos2 = pos;
     
     float3 c = InputTexture1.Sample(Sampler1, pos).xyz;
     
     float3 sum = c;
     float weight_sum = 1.0;
     
     float r; 

     [unroll(4)] //specifies you just want to unroll the loop 4 times. Might be a little less optimized - but it compiles
     for(r=1.0;r<=halfWidth;r+=1.0)
     {         
         pos1 += current_dir1*texelOffset;
         pos2 += current_dir2*texelOffset;
         
         float r2 = r*r;
         float spatial_weight = exp(r2*twoSigmaSSquaredInverse);
         
         float3 c1 = InputTexture1.Sample(Sampler1, pos1).rgb;
         float3 c2 = InputTexture1.Sample(Sampler1, pos2).rgb;
         float2 tonal_differences = float2(length(c-c1), length(c-c2))*100.0;
         
         float2 tonal_weights = exp(tonal_differences*tonal_differences*twoSigmaTSquaredInverse);
         
         tonal_weights *= spatial_weight;
         weight_sum += tonal_weights.x+tonal_weights.y;
         
         sum += tonal_weights.x*c1+tonal_weights.y*c2;     
         
         // Update current directions
         float2 new_dir = InputTexture2.Sample(Sampler2, pos1).xy;
         new_dir = new_dir*2.0-float2(1.0,1.0);
         
         current_dir1 = dot(new_dir, current_dir1)<0.0?-new_dir:new_dir;
         
         new_dir = InputTexture2.Sample(Sampler2, pos2).xy;
         new_dir = new_dir*2.0-float2(1.0,1.0);
         
         current_dir2 = dot(new_dir, current_dir2)<0.0?-new_dir:new_dir;
         
     }     
     
     sum/=weight_sum;     
     
     return float4(sum, 1.0);

}

technique  {
	pass {
		Profile = 9.3;
		PixelShader = PSMain;
	}
}