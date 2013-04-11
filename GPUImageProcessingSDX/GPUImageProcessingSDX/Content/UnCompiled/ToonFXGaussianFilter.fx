/*********************************************************************************************************
 * Author: Paul Demchuk
 * Date: Jan 17, 2013
 * Purpose: Blur the input by radius pixels in either the horiz or vert direction, depending on dir
 *********************************************************************************************************/

Texture2D InputTexture;
SamplerState Sampler;

float texelWidthOffset; 
float texelHeightOffset;

float sigma_flow;
//determines if we are doing x or y position
bool dir;

float4 PSMain(float2 pos: TEXCOORD, float4 SVP : SV_POSITION) : SV_TARGET {

	float radius = ((int)ceil(3.0f*sigma_flow)+1.0f);
	float twoSigmaSSquaredInverse = -0.5f/(sigma_flow*sigma_flow);

	float4 result = InputTexture.Sample(Sampler, pos);
	float weight;
	float weight_sum = 1.0;

	float2 d_v = float2(0.0,0.0);
     
	[unroll(4)]
	for(float d=1.0;d<=radius;d++) {
		weight = exp(d*d*twoSigmaSSquaredInverse);
         
		weight_sum += 2.0*weight;         
        if(dir){
			d_v.x = d*texelWidthOffset;
		}else{
			d_v.y = d*texelHeightOffset;
		}
         
		result += (InputTexture.Sample(Sampler, pos+d_v)+InputTexture.Sample(Sampler, pos-d_v)) * weight;
	}
     
	return result/weight_sum;

}

technique  {
	pass {
		Profile = 9.3;
		PixelShader = PSMain;
	}
}