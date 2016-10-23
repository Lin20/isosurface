float4x4 World;
float4x4 View;
float4x4 Projection;

bool ColorEnabled;
float3 light_dir = float3(0.4f, 1.0f, 0.1f);

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float3 Color : COLOR0;
	float3 Normal : NORMAL0;
};

struct VertexShaderOutput
{
	float4 Position : POSITION0;
	float3 Color : COLOR0;
	float3 Normal : TEXCOORD0;
	float2 Depth : TEXCOORD1;
};

struct PixelShaderOutput
{
	half4 Color : COLOR0;
	half4 Normal : COLOR1;
	half4 Depth : COLOR2;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;

	float4 worldPosition = mul(input.Position, World);
		float4 viewPosition = mul(worldPosition, View);
		output.Position = mul(viewPosition, Projection);
	//if (!ColorEnabled)
	//	output.Position.z -= 0.005f;

	output.Color = input.Color;
	output.Normal = input.Normal;
	output.Depth.x = output.Position.z;
	output.Depth.y = output.Position.w;

	return output;
}

PixelShaderOutput PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	PixelShaderOutput output;
	float d = dot(normalize(light_dir), input.Normal);
	d = (d + 1.0f) * 0.5f;
	float m = lerp(0.6f, 1.0f, d);

	output.Color =  float4(m, m, m, 1.0f);
	output.Normal.rgb = 0.5f * (normalize(input.Normal) + 1.0f);
	output.Normal.a = 1.0f;
	output.Depth = input.Depth.x / input.Depth.y;

	return output;
}

technique Technique1
{
	pass Pass1
	{
		// TODO: set renderstates here.

		VertexShader = compile vs_2_0 VertexShaderFunction();
		PixelShader = compile ps_2_0 PixelShaderFunction();
	}
}
