using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class that I was going to use to try and get around the effects of floating point precision errors in unity, however
/// unity does not allow you to use another type of location positioning for its positions, so I have had to make do with floats.
/// This is still here however because it was a significant step in me understanding some of the shortcomings of doing this 
/// in a proprietary engine.
/// </summary>
public class DVector3
{
    public double x { get; private set; }
    public double y { get; private set; }
    public double z { get; private set; }

    public DVector3(double x, double y, double z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public static DVector3 operator *(DVector3 vec, double x)
    {
        return new DVector3(vec.x * x, vec.y * x, vec.z * x);
    }

    public static DVector3 operator /(DVector3 vec, double x)
    {
        return new DVector3(vec.x / x, vec.y / x, vec.z / x);
    }

    public double Magnitude(DVector3 vec)
    {
        // SQRT(a^2 + b^2 + c^2)

        return Math.Sqrt(vec.x * vec.x + vec.y * vec.y + vec.z + vec.z);
    }

    public double SqrMagnitude(DVector3 vec)
    {
        return vec.x * vec.x + vec.y * vec.y + vec.z + vec.z;
    }

    public DVector3 Normalize(DVector3 vec)
    {
        return vec / vec.Magnitude(vec);
    }
}
