/*********************************************************************************************************
 * Author: Paul Demchuk
 * Date: Jan 17, 2013
 * Purpose: Find the edges based on contrast to surrounding pixels
 *********************************************************************************************************/

Texture2D InputTexture;
SamplerState Sampler;

float2 ImageSize;

SamplerState s
{
	Filter = ANISOTROPIC;
	MaxAnisotropy = 4;
	AddressU = WRAP;
	AddressV = WRAP;
};

 float4 PSMain(float2 pos: TEXCOORD, float4 SVP : SV_POSITION) : SV_TARGET {

	const float p1 = 1.0; 
    const float p2 = 2.0;
    
    float3 rgb_to_L = float3(0.212671,0.715160,0.072169);     
     
    float3 c00 = InputTexture.Sample(s, (pos *ImageSize + float2(-1,-1))/ImageSize).rgb;
    float3 c01 = InputTexture.Sample(s, (pos *ImageSize + float2(0,-1))/ImageSize).rgb;
    float3 c02 = InputTexture.Sample(s, (pos *ImageSize + float2(1,-1))/ImageSize).rgb;
     
    float3 c10 = InputTexture.Sample(s, (pos *ImageSize + float2(-1,0))/ImageSize).rgb;
    float3 c11 = InputTexture.Sample(s, (pos *ImageSize + float2(0,0))/ImageSize).rgb;
    float3 c12 = InputTexture.Sample(s, (pos *ImageSize + float2(1,0))/ImageSize).rgb;
     
    float3 c20 = InputTexture.Sample(s, (pos *ImageSize + float2(-1,1))/ImageSize).rgb;
    float3 c21 = InputTexture.Sample(s, (pos *ImageSize + float2(0,1))/ImageSize).rgb;
    float3 c22 = InputTexture.Sample(s, (pos *ImageSize + float2(1,1))/ImageSize).rgb;
     

     
    float3 sobel_coef = float3(p1,p2,p1);
      
    float3 top_row = float3(dot(c00,rgb_to_L),dot(c01,rgb_to_L),dot(c02,rgb_to_L));
    float3 bottom_row = float3(dot(c20,rgb_to_L),dot(c21,rgb_to_L),dot(c22,rgb_to_L));
      
    float3 left_column = float3(dot(c00,rgb_to_L),dot(c10,rgb_to_L),dot(c20,rgb_to_L));
    float3 right_column = float3(dot(c02,rgb_to_L),dot(c12,rgb_to_L),dot(c22,rgb_to_L));
      
      
    float g_x = dot(sobel_coef,left_column) - dot(sobel_coef,right_column);
      
    float g_y = dot(sobel_coef,top_row) - dot(sobel_coef,bottom_row);      
     
    return float4(g_x*g_x, (g_x*g_y)/2.0f+0.5f, g_y*g_y, 1.0f);     
 }

technique  {
	pass {
		Profile = 9.3;
		PixelShader = PSMain;
	}
}