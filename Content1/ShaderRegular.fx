float4x4 World;
float4x4 View;
float4x4 Projection;

bool ColorEnabled;
float3 light_dir = float3(0.4f, 1.0f, 0.1f);

float3 blue = float3(0.4125f, 0.6625f, 1.0f);
float3 brown = float3(0.4375f, 0.375f, 0.25f);

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
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

	output.Color = input.Color;
	output.Normal = input.Normal;

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	if (!ColorEnabled)
		return float4(0, 0, 0, 1.0f);
	float d = dot(normalize(light_dir), input.Normal);
	d = (d + 1.0f) * 0.5f;
	float m = lerp(0, 1, d);

	float m2 = lerp(-0.1f, 0.15f, d);

	float3 base_color = brown;
		base_color *= m;
	float3 sun_color = float3(1.0f, 1.0f, 0.6f);
		sun_color *= m2;

	return float4(base_color.xyz + sun_color.xyz*0, 1.0f);
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
