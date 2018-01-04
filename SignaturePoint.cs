using System;
using System.Collections.Generic;
using System.Text;

namespace Project
{
    public struct SignaturePoint
    {
        public double X;
        public double Y;
        public float Pressure;
        public SignaturePoint(double x, double y, float pressure)
        {
            X = x;
            Y = y;
            Pressure = pressure;
        }
    }
}
