/*********************************************************************************************************
 * Author: Paul Demchuk
 * Date: Jan 17, 2013
 * Purpose: Run the line integral convolution. This is where the edge detection really happens
 *********************************************************************************************************/

Texture2D InputTexture1;
Texture2D InputTexture2;

SamplerState Sampler1;
SamplerState Sampler2;

float texelWidthOffset; 
float texelHeightOffset;
 
float sigma_c;

 float2 lookup_vector_field(in float2 x)
 {
     float2 v = InputTexture2.Sample(Sampler2, x).xy;
     v = v * 2.0f - float2(1.0,1.0);
     
     return v;
 }
 
 // Look up vector from the structure tensor
 float2 lookup_vector_field_from_struture_tensor(in float2 x)
 {
     float3 g = InputTexture2.Sample(Sampler2, x).rgb;
     
     // Structure Tensor    
     float E = g.x;
     float F = (g.y*2.0f-1.0f); // Transform F back
     float G = g.z;
     
     float delta = sqrt(((E-G)*(E-G))+4.0f*F*F);
     float xi = E+G;
     
     float l1 = 0.5f*(xi+delta); // Major eigenvalue
     float2 v1 = float2(E-l1,F); // eigenvector corresponding to l1     
     v1=v1/length(v1);
     
     return v1;
 }
 
 float2 lookup_dog_value(in float2 x)
 {
     float4 cp = InputTexture1.Sample(Sampler1, x);
     return float2(cp.x + cp.z/255.0f, cp.y + cp.w/255.0f);
 }
 
 // Euler's midpoint method (second order Runge-Kutta with a2 = 1):
 // x_n+1 = x_n + v(x_n + 0.5 * v(v_n))
 float2 move_midpoint_euler(in float2 x, in float2 v, float2 scaleFactor)
 {
     float2 r = lookup_vector_field(x + 0.5f * v * scaleFactor);
     r = dot(r, v)<0.0?-r:r;
     return x + r * scaleFactor;
 }

           
 float4 PSMain(float2 pos: TEXCOORD, float4 SVP : SV_POSITION) : SV_TARGET {

     float4 flow_information = InputTexture2.Sample(Sampler2, pos);
     float A = flow_information.w; // Anisotropy measure
     float2 dir_at_pixel = flow_information.xy;
     dir_at_pixel = dir_at_pixel*2.0f-float2(1.0,1.0);
     
     //float2 dir_at_pixel = lookup_vector_field(textureCoordinate);
     
     float2 v_positive = dir_at_pixel; // current direction in the positive movement
     float2 v_negative = -dir_at_pixel; // current direction in the opposite movement 
     
     float2 x_positive = pos;
     float2 x_negative = pos;
     
     float2 sum = lookup_dog_value(pos);
     
     float weight_sum = 1.0f;
     
     float r;
     
     //float sigma_s = sigma_c*(.5+A);
     float sigma_s = sigma_c;
     float radius = 2.0f*sigma_s + 1.0f; 
     
     [unroll(12)]
     for(r=1.0;r<=radius;r+=1.0)
     {         
         float weight = exp(-r*r*0.5f/(sigma_s*sigma_s));
         weight_sum+=2.0*weight;
         
         x_positive = move_midpoint_euler(x_positive, v_positive, float2(texelWidthOffset, texelHeightOffset));
         x_negative = move_midpoint_euler(x_negative, v_negative, float2(texelWidthOffset, texelHeightOffset));
         
         sum+= (lookup_dog_value(x_positive) + lookup_dog_value(x_negative)) * weight;
         
         float2 new_positive = lookup_vector_field(x_positive);
         float2 new_negative = lookup_vector_field(x_negative);
         
         v_positive = dot(v_positive, new_positive)<0.0?-new_positive:new_positive;
         v_negative = dot(v_negative, new_negative)<0.0?-new_negative:new_negative;
     }
     
     sum/=weight_sum;
     
     float4 result = float4(floor(sum.x*255.0f)/255.0f, floor(sum.y*255.0f)/255.0f, 0.0f, 0.0f);
     result.z = (sum.x-result.x)*255.0f;
     result.w = (sum.y-result.y)*255.0f;
     
    return result;
 }

technique  {
	pass {
		Profile = 9.3;
		PixelShader = PSMain;
	}
}