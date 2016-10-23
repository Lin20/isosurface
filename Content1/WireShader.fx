float4x4 World;
float4x4 View;
float4x4 Projection;

bool ColorEnabled;

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float3 Color : COLOR0;
};

struct VertexShaderOutput
{
	float4 Position : POSITION0;
	float3 Color : COLOR0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;

	float4 worldPosition = mul(input.Position, World);
		float4 viewPosition = mul(worldPosition, View);
		output.Position = mul(viewPosition, Projection);
	output.Position.z -= 0.0001f; //depth bias

	output.Color = input.Color;

	return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	return float4(0,0,0, 1.0f);
}

technique Technique1
{
	pass Pass1
	{
		CullMode = NONE;
		VertexShader = compile vs_2_0 VertexShaderFunction();
		PixelShader = compile ps_2_0 PixelShaderFunction();
	}
}
