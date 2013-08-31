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
 public class Util
    {
        public static Color LABtoRGB(CIELAB lab)
        {
            double gamma = 2.2;
            double e = 216 / 24389.0;
            double k = 24389 / 27.0;

            double XR = 0.95047;
            double YR = 1.00000;
            double ZR = 1.08883;

            double fy = (lab.L + 16) / 116.0;
            double fx = lab.A / 500.0 + fy;
            double fz = fy - lab.B / 200.0;

            double[,] xyzTorgbMatrix = new double[3, 3] {{3.2404542, -1.5371385, -0.4985314},
                                                        {-0.9692660,  1.8760108,  0.0415560},
                                                        {0.0556434, -0.2040259,  1.0572252}};
            double xR = Math.Pow(fx, 3.0);
            double zR = Math.Pow(fz, 3.0);

            xR = (xR > e) ? xR : (116 * fx - 16) / k;
            double yR = (lab.L > k * e) ? Math.Pow((lab.L + 16) / 116.0, 3.0) : lab.L / k;
            zR = (zR > e) ? zR : (116 * fz - 16) / k;

            double x = xR * XR;
            double y = yR * YR;
            double z = zR * ZR;

            //xyz to rgb
            double r = xyzTorgbMatrix[0, 0] * x + xyzTorgbMatrix[0, 1] * y + xyzTorgbMatrix[0, 2] * z;
            double g = xyzTorgbMatrix[1, 0] * x + xyzTorgbMatrix[1, 1] * y + xyzTorgbMatrix[1, 2] * z;
            double b = xyzTorgbMatrix[2, 0] * x + xyzTorgbMatrix[2, 1] * y + xyzTorgbMatrix[2, 2] * z;

            int red = (int)Math.Round(255 * (Math.Pow(clamp(r), 1.0 / gamma)));
            int green = (int)Math.Round(255 * (Math.Pow(clamp(g), 1.0 / gamma)));
            int blue = (int)Math.Round(255 * (Math.Pow(clamp(b), 1.0 / gamma)));

            return Color.FromArgb(red, green, blue);
        }

        private static double clamp(double value)
        {
            return Math.Min(Math.Max(value, 0.0), 1.0);
        }


        public static CIELAB RGBtoLAB(Color rgb)
        {
            double gamma = 2.2;
            double red = Math.Pow(rgb.R / 255.0, gamma); //range from 0 to 1.0
            double green = Math.Pow(rgb.G / 255.0, gamma);
            double blue = Math.Pow(rgb.B / 255.0, gamma);


            //assume rgb is already linear
            //sRGB to xyz
            //http://www.brucelindbloom.com/
            double[,] rgbToxyzMatrix = new double[3, 3]{
                                            {0.4124564,  0.3575761,  0.1804375},
                                            {0.2126729,  0.7151522,  0.0721750},
                                            {0.0193339,  0.1191920,  0.9503041}};

            double x = rgbToxyzMatrix[0, 0] * red + rgbToxyzMatrix[0, 1] * green + rgbToxyzMatrix[0, 2] * blue;
            double y = rgbToxyzMatrix[1, 0] * red + rgbToxyzMatrix[1, 1] * green + rgbToxyzMatrix[1, 2] * blue;
            double z = rgbToxyzMatrix[2, 0] * red + rgbToxyzMatrix[2, 1] * green + rgbToxyzMatrix[2, 2] * blue;

            double XR = 0.95047;
            double YR = 1.00000;
            double ZR = 1.08883;

            double e = 216 / 24389.0;
            double k = 24389 / 27.0;

            double xR = x / XR;
            double yR = y / YR;
            double zR = z / ZR;

            double fx = (xR > e) ? Math.Pow(xR, 1.0 / 3.0) : (k * xR + 16) / 116.0;
            double fy = (yR > e) ? Math.Pow(yR, 1.0 / 3.0) : (k * yR + 16) / 116.0;
            double fz = (zR > e) ? Math.Pow(zR, 1.0 / 3.0) : (k * zR + 16) / 116.0;

            double cieL = 116 * fy - 16;
            double cieA = 500 * (fx - fy);
            double cieB = 200 * (fy - fz);

            return new CIELAB(cieL, cieA, cieB);

        }

        //Resize the bitmap using nearest neighbors
        public static Bitmap ResizeBitmapNearest(Bitmap b, int nWidth, int nHeight)
        {
            Bitmap result = new Bitmap(nWidth, nHeight);
            using (Graphics g = Graphics.FromImage((Image)result))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.SmoothingMode = SmoothingMode.None;
                g.DrawImage(b, 0, 0, nWidth, nHeight);
            }
            return result;
        }

        public static Bitmap GetImage(String filename, bool resize=false, int maxDim=125)
        {         
            if (resize)
            {
                Bitmap orig = new Bitmap(filename);
                int resizedWidth = orig.Width;
                int resizedHeight = orig.Height;
                if (orig.Width > maxDim || orig.Height > maxDim)
                {
                    if (orig.Width > orig.Height)
                    {
                        resizedWidth = maxDim;
                        resizedHeight = (int)Math.Round((double)orig.Height / orig.Width * maxDim);
                    }
                    else
                    {
                        resizedHeight = maxDim;
                        resizedWidth = (int)Math.Round((double)orig.Width / orig.Height * maxDim);
                    }
                }

                Bitmap image = new Bitmap(orig, resizedWidth, resizedHeight);
                orig.Dispose();
                return image;
            }
            else
                return new Bitmap(filename);       
        }


        public static String ConvertFileName(String basename, String label, String toExt = null)
        {
            FileInfo info = new FileInfo(basename);
            String extension = info.Extension;
            String result = (toExt == null) ? basename.Replace(extension, label + extension) : basename.Replace(extension, label + toExt);
            return result;
        }

        //from: http://stackoverflow.com/questions/1335426/is-there-a-built-in-c-net-system-api-for-hsv-to-rgb
        public static HSV RGBtoHSV(Color color)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));
            double chroma = max - min;

            double huep = 0;
            if (chroma == 0)
                huep = 0;
            else if (max == color.R)
                huep = (color.G - color.B) / chroma % 6;
            else if (max == color.G)
                huep = (color.B - color.R) / chroma + 2;
            else
                huep = (color.R - color.G) / chroma + 4;

            double hue = 60 * huep;//color.GetHue();

            if (hue < 0)
                hue += 360;

           /* double alpha = 0.5 * (2 * color.R - color.G - color.B);
            double beta = Math.Sqrt(3) / 2.0 * (color.G - color.B);
            double hue = Math.Atan2(beta, alpha) * 180 / Math.PI;
            if (hue < 0)
                hue += 360;*/

            //double hue = color.GetHue();

            double saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            double value = max / 255d;


            return new HSV(hue, saturation, value);
        }

        public static Color HSVtoRGB(HSV hsv)
        {
            double hue = hsv.H;
            double value = hsv.V;
            double saturation = hsv.S;

            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(255, t, p, v);
            else
                return Color.FromArgb(255, v, p, q);
        }

        public static Color[,] BitmapToArray(Bitmap image)
        {
            BitmapData lockData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Int32[] imageData = new Int32[image.Width * image.Height];

            System.Runtime.InteropServices.Marshal.Copy(lockData.Scan0, imageData, 0, imageData.Length);
            image.UnlockBits(lockData);

            Color[,] result = new Color[image.Width, image.Height];
            for (int i = 0; i < image.Width; i++)
                for (int j = 0; j < image.Height; j++)
                    result[i, j] = Color.FromArgb(imageData[j * image.Width + i]);

            return result;
        }

        public static Color[] BitmapTo1DArray(Bitmap image)
        {
            BitmapData lockData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Int32[] imageData = new Int32[image.Width * image.Height];

            System.Runtime.InteropServices.Marshal.Copy(lockData.Scan0, imageData, 0, imageData.Length);
            image.UnlockBits(lockData);

            Color[] result = imageData.Select(i => Color.FromArgb(i)).ToArray<Color>();

            return result;
        }

        public static Bitmap ArrayToBitmap(Color[,] image)
        {
            int width = image.GetLength(0);
            int height = image.GetLength(1);
            Bitmap result = new Bitmap(width, height);

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    result.SetPixel(i, j, image[i, j]);
                }
            }

            return result;
        }

        //2D array map function
        public static TDest[,] Map<TSource, TDest>(TSource[,] source, Func<TSource, TDest> func)
        {
            int width = source.GetLength(0);
            int height = source.GetLength(1);

            TDest[,] result = new TDest[width, height];

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    result[i, j] = func(source[i, j]);
                }
            }

            return result;
        }

        //Shuffle function
        public static void Shuffle<T>(List<T> shuffled, Random random=null)
        {
            if (random == null)
                random = new Random();

            for (int j = shuffled.Count() - 1; j >= 0; j--)
            {
                int idx = random.Next(j + 1);
                T temp = shuffled[j];
                shuffled[j] = shuffled[idx];
                shuffled[idx] = temp;
            }
        }


        public static bool InBounds(int x, int y, int width, int height)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

    }
}

