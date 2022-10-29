#ifndef PREVIEW_CGINC_INCLUDED
#define PREVIEW_CGINC_INCLUDED


inline half3 colorize(float val, bool pro)
{
	half4 color0 = half4(0.666, 0.2, 0.000, 1);	//(0.000, 0.426, 0.000, 1) //0, 109, 0
	half4 color1 = half4(0.95, 0.65, 0.1, 1);		//(0.664, 0.793, 0.476, 1)  //170, 203, 122
	half4 color2 = half4(1, 1, 0.75, 1);			//(0.949, 0.925, 0.695, 1)  //243, 237, 178
	half4 color3 = half4(0.55, 0.85, 0.2, 1);		//(0.902, 0.578, 0.171, 1)  //231, 148, 44
	half4 color4 = half4(0.000, 0.666, 0.2, 1);	//(0.726, 0.000, 0.000, 1)  //186, 0, 0 

	if (pro) { color0 *= color0; color1=pow(color1,1.4); color3=pow(color3,1.4); color4*=color4; }

	half3 col = 0;

	if (val < 0.25)
	{
		float percent = val / 0.25;
		col = color0 * (1 - percent) + color1 * percent;
	}
	else if (val < 0.5f)
	{
		float percent = (val - 0.25) / 0.25;
		col = color1 * (1 - percent) + color2 * percent;
	}
	else if (val < 0.75f)
	{
		float percent = (val - 0.5f) / 0.25;
		col = color2 * (1 - percent) + color3 * percent;
	}
	else if (val < 1)
	{
		float percent = (val - 0.75f) / 0.25;
		col = color3 * (1 - percent) + color4 * percent;
	}
	else col = color4;

	return pow(col, 2.2);
}


inline float normal(float prev, float next)
{
	float delta = prev - next;
	float height = 1;
	return delta;
}


inline half relief(sampler2D tex, float2 texelSize, float2 uv)
{
	half4 prevX = tex2Dlod(tex, float4(uv.x + texelSize.x, uv.y, 0, 0));
	half4 nextX = tex2Dlod(tex, float4(uv.x - texelSize.x, uv.y, 0, 0));
	half4 prevY = tex2Dlod(tex, float4(uv.x, uv.y - texelSize.y, 0, 0));
	half4 nextY = tex2Dlod(tex, float4(uv.x, uv.y + texelSize.y, 0, 0));

	float normX = normal(prevX, nextX);
	float normY = normal(prevY, nextY);

	float norm = normY + -normX;

	norm = norm / (texelSize.x * 512); //shader values set up for resolution 512
	return (norm + 1) / 2; //in 0-1 range, 0.5 is midpoint
}


inline half3 saturation(half3 color, half saturation)
{
	float P = sqrt(color.r*color.r*0.3 + color.g*color.g*0.5 + color.b*color.b*0.2);

	color.r = P + (color.r - P) * saturation;
	color.g = P + (color.g - P) * saturation;
	color.b = P + (color.b - P) * saturation;

	return color;
}


inline half overlay(half src, half dst)
{
	half multiply = 2 * src*dst;
	half screen = (1 - 2 * (1 - src)*(1 - dst));

	int d = step(src, 1);
	return multiply * d + screen * (1 - d);
}

inline half3 overlay(half3 src, half3 dst)
{
	return half3(overlay(src.r, dst.r), overlay(src.g, dst.g), overlay(src.b, dst.b));
}


#endif