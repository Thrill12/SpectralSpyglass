using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
