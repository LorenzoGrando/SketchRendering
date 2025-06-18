#pragma once

inline float2 VoronoiRandomVector (float2 UV, float offset)
{
    float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
    UV = frac(sin(mul(UV, m)) * 46839.32);
    return float2(sin(UV.y*+offset)*0.5+0.5, cos(UV.x*offset)*0.5+0.5);
}

float Voronoi(float2 UV, float AngleOffset, float CellDensity)
{
    float Out;
    float2 g = floor(UV * CellDensity);
    float2 f = frac(UV * CellDensity);
    float3 res = float3(8.0, 0.0, 0.0);

    for(int y=-1; y<=1; y++)
    {
        for(int x=-1; x<=1; x++)
        {
            float2 lattice = float2(x,y);
            float2 offset = VoronoiRandomVector(lattice + g, AngleOffset);
            float d = distance(lattice + offset, f);
            if(d < res.x)
            {
                res = float3(d, offset.x, offset.y);
                Out = res.x;
            }
        }
    }

    return Out;
}

float Voronoi_Chebyshev(float2 UV, float AngleOffset, float CellDensity)
{
    float Out;
    float2 g = floor(UV * CellDensity);
    float2 f = frac(UV * CellDensity);
    float3 res = float3(8.0, 0.0, 0.0);

    for(int y=-1; y<=1; y++)
    {
        for(int x=-1; x<=1; x++)
        {
            float2 lattice = float2(x,y);
            float2 offset = VoronoiRandomVector(lattice + g, AngleOffset);
            float2 r = lattice - f + offset;
            float d = max(abs(r.x), abs(r.y));
            if(d < res.x)
            {
                res = float3(d, offset.x, offset.y);
                Out = res.x;
            }
        }
    }

    return Out;
}