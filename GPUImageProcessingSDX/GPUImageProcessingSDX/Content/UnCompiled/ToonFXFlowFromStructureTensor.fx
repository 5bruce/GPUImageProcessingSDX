/*********************************************************************************************************
 * Author: Paul Demchuk
 * Date: Jan 17, 2013
 * Purpose: Calculate the flow. I am not 100% sure what this step does, but I think that it
 *          sort of creates a vector field of colors that is used in the whole rest of the pipeline
 *********************************************************************************************************/

Texture2D InputTexture;
SamplerState Sampler;

 float4 PSMain(float2 pos: TEXCOORD, float4 SVP : SV_POSITION) : SV_TARGET {

	float3 g = InputTexture.Sample(Sampler, pos).rgb;
     
     // Structure Tensor    
     float E = g.x;
     float F = (g.y*2.0-1.0); // Transform F back
     float G = g.z;
     
     float delta = sqrt(((E-G)*(E-G))+4.0*F*F);
     float xi = E+G;
     
     // Major eigen vector is in the direction of the greatest change
     // Minor eigen vector is in the direction of the least change     
     
     float l1 = 0.5*(xi+delta); // Major eigenvalue
     //float l2 = 0.5*(xi-delta); // Minor eigenvalue 
     float2 v1 = float2(E-l1,F); // eigenvector corresponding to l1     
     //vec2 v2 = vec2(F,l1-E); // eigenvector corresponding to l2
     
     float A = xi>0.01? delta/xi : 0.0;
     
     
     v1=v1/length(v1);
     v1 = v1/2.0+float2(0.5,0.5); // Convert each component to [0, 1] range from [-1,1] domain    
     
     return float4(v1, l1/2.0, A);     
 }

technique  {
	pass {
		Profile = 9.3;
		PixelShader = PSMain;
	}
}