/*********************************************************************************************************
 * Author: Paul Demchuk
 * Date: Jan 17, 2013
 * Purpose: Threshold. Make lines thicker and smoother
 *********************************************************************************************************/

Texture2D InputTexture;
SamplerState Sampler;

float edge_offset;
float grey_offset;
float black_offset;

bool Rainbow;
 
float smoothstep_dog(in float v) {
    const float mini=-2.0;
    const float maxi=2.0;
    float r = 0.0;
	if(v<mini)
    {
		r=0.0;
    }
	else if (v>maxi)
    {
		r=1.0;
    }
    else 
    {
        float m = (v-mini)/(maxi-mini);
        float m2 = m*m;
        r = -2.0*m2*m + 3.0*m2;
    }
    return r;
}
 
float L2Grey(in float L) {
	L = (L+16.0)/116.0;
	return L*L*L;
}
 

 float4 PSMain(float2 pos: TEXCOORD, float4 SVP : SV_POSITION) : SV_TARGET {
    const float mini = -2.0;
    const float maxi = 2.0;
     
    float v;
     
    float4 cp = InputTexture.Sample(Sampler, pos);     
    float2 c = float2(cp.x + cp.z/255.0, cp.y + cp.w/255.0);
     
    c*=100.0; // All the values below are optimized for the [0,255] range so we need to do this
     
    // Edge Channel values
    float edge_sensitivity = 0.996; 
    float edge_steepness = 0.74;   
     
    // Grey Channel values
    float grey_sensitivity = 0.901;
    float grey_steepness = 2.0;
     
    // Black Channel values     
    float black_sensitivity = 0.927; 
    float black_steepness = 2.0;     
          
    float src = (c.x - edge_sensitivity*c.y) - edge_offset;
    float edge = (src>0.0)?1.0: 2.0*smoothstep_dog(edge_steepness*src);
    edge = L2Grey(edge*100.0);
     
    src = (c.x - grey_sensitivity*c.y) - grey_offset;
    float grey = (src>0.0)?1.0: 2.0*smoothstep_dog(grey_steepness*src);
    grey = L2Grey(grey*100.0);
     
    src = (c.x - black_sensitivity*c.y) - black_offset;
    float black = (src>0.0)?1.0: 2.0*smoothstep_dog(black_steepness*src);
    black = L2Grey(black*100.0);
     
    float combined = (0.6+0.4*grey) * black * pow(edge,1.0);
     
    float4 pixelColor = float4(combined, combined, combined, 1.0);
	 
	//dont want rainbow
	if(!Rainbow) return pixelColor;

	//This code does linear rainbow
	if(length(pixelColor.xyz) < 1.5) {
		if(pos.y < 0.25f){
		return float4(1,pos.y/0.25f,0,1);
	}else if(pos.y < 0.5f){
		return float4(1.0f-(pos.y-0.25)/0.25f,1,0,1);
	}else if(pos.y < 0.75f){
		return float4(0,1,(pos.y - 0.5f)/0.25f,1);
	}else{
		return float4(0,1.0f-(pos.y - 0.75f)/0.25f,1,1);
	}
	}
	else return float4(0,0,0,1);
	 

	 /* This code does radial gradient/rainbow 
	 int width = 768; 
	 int height = 1280;

	 float2 pixel = pos * float2(width,height);

	 float2 vec = float2(width/2, height/2) - pixel;

	if(length(pixelColor.xyz) < 1.5) {
		if(length(vec) < 200){
			return float4(1,length(vec)/200.0f,0,1);
		}else if(length(vec) < 400){
			return float4(1.0f-(length(vec)-200.0f)/200.0f,1,0,1);
		}else if(length(vec) < 600){
			return float4(0,1,(length(vec) - 400.0f)/200.0f,1);
		}else{
			return float4(0,1.0f-(length(vec) - 600.0f)/200.0f,1,1);
		}
	 }

	return float4(0,0,0,1);
	*/
 }

technique  {
	pass {
		Profile = 9.3;
		PixelShader = PSMain;
	}
}