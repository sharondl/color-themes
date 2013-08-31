using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;

namespace Engine
{
    public class HSV
    {
        public double H;
        public double S;
        public double V;

        public HSV()
        {
            H = 0;
            S = 0;
            V = 0;
        }

        public HSV(double h, double s, double v)
        {
            H = h;
            S = s;
            V = v;
        }

        public static HSV operator +(HSV a, HSV b)
        {
            return new HSV(a.H + b.H, a.S + b.S, a.V + b.V);
        }

        public static HSV operator *(HSV a, HSV b)
        {
            return new HSV(a.H * b.H, a.S * b.S, a.V * b.V);
        }

        public static HSV operator /(HSV a, HSV b)
        {
            return new HSV(a.H / b.H, a.S / b.S, a.V / b.V);
        }

        public static HSV operator -(HSV a, HSV b)
        {
            return new HSV(a.H - b.H, a.S - b.S, a.V - b.V);
        }

        public static HSV operator /(HSV a, double s)
        {
            return new HSV(a.H / s, a.S / s, a.V / s);
        }

        public static HSV operator *(HSV a, double s)
        {
            return new HSV(a.H * s, a.S * s, a.V * s);
        }

    }

    public class CIELAB : IEquatable<CIELAB>
    {
        public double L;
        public double A;
        public double B;

        public CIELAB()
        {
            L = 0;
            A = 0;
            B = 0;
        }

        public CIELAB(double l, double a, double b)
        {
            L = l;
            A = a;
            B = b;
        }

        public double SqDist(CIELAB lab)
        {
            double ldiff = this.L - lab.L;
            double adiff = this.A - lab.A;
            double bdiff = this.B - lab.B;
            return Math.Pow(ldiff, 2.0) + Math.Pow(adiff, 2.0) + Math.Pow(bdiff, 2.0);
        }

        //ported from https://github.com/StanfordHCI/c3/blob/master/lib/d3/d3.color.js
        public double CIEDE2000Dist(CIELAB lab)
        {
            // adapted from Sharma et al's MATLAB implementation at
            //  http://www.ece.rochester.edu/~gsharma/ciede2000/

            CIELAB x = this;
            CIELAB y = lab;

            // parametric factors, use defaults
            double kl = 1, kc = 1, kh = 1;

            // compute terms
            double pi = Math.PI,
                L1 = x.L, a1 = x.A, b1 = x.B, Cab1 = Math.Sqrt(a1 * a1 + b1 * b1),
                L2 = y.L, a2 = y.A, b2 = y.B, Cab2 = Math.Sqrt(a2 * a2 + b2 * b2),
                Cab = 0.5 * (Cab1 + Cab2),
                G = 0.5 * (1 - Math.Sqrt(Math.Pow(Cab, 7) / (Math.Pow(Cab, 7) + Math.Pow(25, 7)))),
                ap1 = (1 + G) * a1,
                ap2 = (1 + G) * a2,
                Cp1 = Math.Sqrt(ap1 * ap1 + b1 * b1),
                Cp2 = Math.Sqrt(ap2 * ap2 + b2 * b2),
                Cpp = Cp1 * Cp2;

            // ensure hue is between 0 and 2pi
            double hp1 = Math.Atan2(b1, ap1); if (hp1 < 0) hp1 += 2 * pi;
            double hp2 = Math.Atan2(b2, ap2); if (hp2 < 0) hp2 += 2 * pi;

            double dL = L2 - L1,
                dC = Cp2 - Cp1,
                dhp = hp2 - hp1;

            if (dhp > +pi) dhp -= 2 * pi;
            if (dhp < -pi) dhp += 2 * pi;
            if (Cpp == 0) dhp = 0;

            // Note that the defining equations actually need
            // signed Hue and chroma differences which is different
            // from prior color difference formulae
            double dH = 2 * Math.Sqrt(Cpp) * Math.Sin(dhp / 2);

            // Weighting functions
            double Lp = 0.5 * (L1 + L2),
                Cp = 0.5 * (Cp1 + Cp2);

            // Average Hue Computation
            // This is equivalent to that in the paper but simpler programmatically.
            // Average hue is computed in radians and converted to degrees where needed
            double hp = 0.5 * (hp1 + hp2);
            // Identify positions for which abs hue diff exceeds 180 degrees 
            if (Math.Abs(hp1 - hp2) > pi) hp -= pi;
            if (hp < 0) hp += 2 * pi;

            // Check if one of the chroma values is zero, in which case set 
            // mean hue to the sum which is equivalent to other value
            if (Cpp == 0) hp = hp1 + hp2;

            double Lpm502 = (Lp - 50) * (Lp - 50),
                Sl = 1 + 0.015 * Lpm502 / Math.Sqrt(20 + Lpm502),
                Sc = 1 + 0.045 * Cp,
                T = 1 - 0.17 * Math.Cos(hp - pi / 6)
                      + 0.24 * Math.Cos(2 * hp)
                      + 0.32 * Math.Cos(3 * hp + pi / 30)
                      - 0.20 * Math.Cos(4 * hp - 63 * pi / 180),
                Sh = 1 + 0.015 * Cp * T,
                ex = (180 / pi * hp - 275) / 25,
                delthetarad = (30 * pi / 180) * Math.Exp(-1 * (ex * ex)),
                Rc = 2 * Math.Sqrt(Math.Pow(Cp, 7) / (Math.Pow(Cp, 7) + Math.Pow(25, 7))),
                RT = -1 * Math.Sin(2 * delthetarad) * Rc;

            dL = dL / (kl * Sl);
            dC = dC / (kc * Sc);
            dH = dH / (kh * Sh);

            // The CIE 00 color difference
            return Math.Sqrt(dL * dL + dC * dC + dH * dH + RT * dC * dH);

        }


        public bool Equals(CIELAB other)
        {
            return L == other.L && A == other.A && B == other.B;
        }


        public override int GetHashCode()
        {
            //convert to RGB and see
            //CIELAB lab = new CIELAB(L, A, B);
            //Color c = Util.LABtoRGB(lab);
            int hCode = (int)Math.Round(L) ^ (int)Math.Round(A) ^ (int)Math.Round(B);
            //int hCode = c.R ^ c.G ^ c.B;
            return hCode.GetHashCode();
        }

        public static CIELAB operator +(CIELAB a, CIELAB b)
        {
            return new CIELAB(a.L + b.L, a.A + b.A, a.B + b.B);
        }

        public static CIELAB operator *(CIELAB a, CIELAB b)
        {
            return new CIELAB(a.L * b.L, a.A * b.A, a.B * b.B);
        }

        public static CIELAB operator /(CIELAB a, CIELAB b)
        {
            return new CIELAB(a.L / b.L, a.A / b.A, a.B / b.B);
        }

        public static CIELAB operator -(CIELAB a, CIELAB b)
        {
            return new CIELAB(a.L - b.L, a.A - b.A, a.B - b.B);
        }

        public static CIELAB operator /(CIELAB a, double s)
        {
            return new CIELAB(a.L / s, a.A / s, a.B / s);
        }

        public static CIELAB operator *(CIELAB a, double s)
        {
            return new CIELAB(a.L * s, a.A * s, a.B * s);
        }

        public override string ToString()
        {
            //return base.ToString();
            return "(" + L + ", " + A + ", " + B + ")";
        }
    }

}
