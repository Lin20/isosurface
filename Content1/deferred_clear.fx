struct VertexShaderInput
{
    float4 Position : POSITION0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
};

struct PixelShaderOutput
{
	float4 Color : COLOR0;
	float4 Normal : COLOR1;
	float4 Depth : COLOR2;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

	output.Position = float4(input.Position.xyz, 1);

    return output;
}

PixelShaderOutput PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	PixelShaderOutput output;

	output.Color = 0.0f;
	output.Color.a = 0.0f;

	output.Normal.rgb = 0.5f;
	output.Normal.a = 0.0f;

	output.Depth = 1.0f;

	return output;
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
