/*********************************************************************************************************
 * Author: Paul Demchuk
 * Date: Jan 17, 2013
 * Purpose: Perform the difference of gaussians, finding the edges
 *********************************************************************************************************/

Texture2D InputTexture1;
Texture2D InputTexture2;

SamplerState Sampler1;
SamplerState Sampler2;

float texelWidthOffset; 
float texelHeightOffset;
 
float sigma_dog; // Spatial gaussian standard deviation


float4 PSMain(float2 pos: TEXCOORD, float4 SVP : SV_POSITION) : SV_TARGET {
	
	//just creating sigma_e so that it is the same as the OSX code, which uses e instead of dog
	float sigma_e = sigma_dog;
	float sigma_r = 1.6f * sigma_e; 
	float halfWidth = max(1.0, 2.0f * sigma_r);
	float twoSigmaESquared = -1.0f/(2.0f*sigma_e*sigma_e);
	float twoSigmaRSquared = -1.0f/(2.0f*sigma_r*sigma_r);
	
	float2 dir = InputTexture2.Sample(Sampler2, pos).xy;
    dir = dir*2.0-float2(1.0,1.0);
   
    float4 c = InputTexture1.Sample(Sampler1, pos);
    float L0 = c.r;
    
    float2 normal = float2(-dir.y*texelWidthOffset,dir.x*texelHeightOffset); // Perpendicular to the local flow
     
    float2 sum = float2(L0, L0); 
     
    float2 weight_sum = float2(1.0,1.0);
     
    float r;
     
	[unroll(4)]
    for(r=1.0;r<=halfWidth;r+=1.0)
    {
        float r2 = r*r;
        
        float2 weight = float2(exp(r2*twoSigmaESquared), exp(r2*twoSigmaRSquared));
         
        weight_sum+=2.0*weight;
         
        float L = InputTexture1.Sample(Sampler1, pos+r*normal).r;
        L += InputTexture1.Sample(Sampler1, pos-r*normal).r;
         
        sum+= L * weight; 
     }
     
     sum/=weight_sum;
    
    // Pack the results into two channels each to maintain accuracy
    float4 result = float4(floor(sum.x*255.0)/255.0, floor(sum.y*255.0)/255.0, 0.0, 0.0);
    result.z = (sum.x-result.x)*255.0;
    result.w = (sum.y-result.y)*255.0;
    
    return result;
    

}

technique  {
	pass {
		Profile = 9.3;
		PixelShader = PSMain;
	}
}