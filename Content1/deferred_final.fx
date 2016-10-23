float4x4 InvertViewProjection;

texture color_map;
texture normal_map;
texture depth_map;
texture noise_map;

float2 half_pixel;

float random_size = 0.01f;
float g_sample_rad = 16.0f;
float g_intensity = 1.0f;
float g_scale = 0.01f;
float g_bias = 0;

sampler color_sampler = sampler_state
{
	Texture = (color_map);
	AddressU = CLAMP;
	AddressV = CLAMP;
	MagFilter = LINEAR;
	MinFilter = LINEAR;
	Mipfilter = LINEAR;
};

sampler depth_sampler = sampler_state
{
	Texture = (depth_map);
	AddressU = CLAMP;
	AddressV = CLAMP;
	MagFilter = POINT;
	MinFilter = POINT;
	Mipfilter = POINT;
};

sampler normal_sampler = sampler_state
{
	Texture = (normal_map);
	AddressU = CLAMP;
	AddressV = CLAMP;
	MagFilter = POINT;
	MinFilter = POINT;
	Mipfilter = POINT;
};

sampler noise_sampler = sampler_state
{
	Texture = (noise_map);
	AddressU = WRAP;
	AddressV = WRAP;
	MagFilter = LINEAR;
	MinFilter = LINEAR;
	Mipfilter = LINEAR;
};

struct VertexShaderInput
{
	float3 Position : POSITION0;
	float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
	float4 Position : POSITION0;
	float2 TexCoord : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;

	output.Position = float4(input.Position, 1);
	output.TexCoord = input.TexCoord + half_pixel;

	return output;
}

float2 getRandom(in float2 uv)
{
	return normalize(tex2D(noise_sampler, uv / random_size).xy * 2.0f - 1.0f);
}

float3 GetPosition(in float2 uv)
{
	float depthVal = tex2D(depth_sampler, uv).r;
	float4 position;

	position.x = uv.x * 2.0f - 1.0f;
	position.y = -(uv.y * 2.0f - 1.0f);
	position.z = depthVal;
	position.w = 1.0f;

	position = mul(position, InvertViewProjection);

	position /= position.w;

	return position.xyz;
}

float doAmbientOcclusion(in float2 tcoord, in float2 uv, in float3 position, in float3 cnorm)
{
	float3 diff = GetPosition(tcoord + uv) - position;
		const float3 v = normalize(diff);
	const float d = length(diff)*g_scale;
	return max(0.0, dot(cnorm, v) - g_bias)*(1.0 / (1.0 + d))*g_intensity;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	float4 normalData = tex2D(normal_sampler, input.TexCoord);
	float3 normal = 2.0f * normalData.xyz - 1.0f;

	return normalData;

	float specularPower = normalData.a * 255;
	float specularIntensity = tex2D(color_sampler, input.TexCoord).a;

	float3 color = tex2D(color_sampler, input.TexCoord).rgb;

		float depthVal = tex2D(depth_sampler, input.TexCoord).r;
	float4 position;

	position.x = input.TexCoord.x * 2.0f - 1.0f;
	position.y = -(input.TexCoord.y * 2.0f - 1.0f);
	position.z = depthVal;
	position.w = 1.0f;

	position = mul(position, InvertViewProjection);

	position /= position.w;

	float ao = 0.0f;
	float rad = g_sample_rad / position.z;
	float2 rand = getRandom(input.TexCoord);
	const float2 vec[4] = { float2(1, 0), float2(-1, 0),
		float2(0, 1), float2(0, -1) };

	//**SSAO Calculation**//
	int iterations = 4;
	for (int j = 0; j < iterations; ++j)
	{
		float2 coord1 = reflect(vec[j], rand)*rad;
			float2 coord2 = float2(coord1.x*0.707 - coord1.y*0.707,
			coord1.x*0.707 + coord1.y*0.707);

		ao += doAmbientOcclusion(input.TexCoord, coord1*0.25, position, normal);
		ao += doAmbientOcclusion(input.TexCoord, coord2*0.5, position, normal);
		ao += doAmbientOcclusion(input.TexCoord, coord1*0.75, position, normal);
		ao += doAmbientOcclusion(input.TexCoord, coord2, position, normal);
	}
	ao /= (float)iterations;

	return float4(color.r, color.g, color.b, 0) * (1.0f - ao);
}

technique Technique1
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunction();
	}
}
