using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Metadata;

namespace MetaBallGIFtest
{
    class Program
    {
        static void Main()
        {
            Chamber chamber = new Chamber(240, 180);
            chamber.AddRandomCharges(4000, 6000, 24);
            Console.WriteLine("there are {0} charges", chamber.pointCharges.Count);
            Directory.CreateDirectory("output");
            int totalFrames = 10;
            for (int t = 0; t < totalFrames; t++)
            {
                Console.Write("rendering frame {0}/{1}\r", t + 1, totalFrames);
                chamber.RenderFrame();
            }
            //Console.WriteLine("The GIF has {0} frames", chamber.gif.Frames.Count);
            using(var fileStream = new FileStream("output\\result.gif", FileMode.Create))
            {
                chamber.gif.SaveAsGif(fileStream);
            }
            //Console.ReadKey();
        }
    }
    class PointCharge
    {
        public int x, y;
        public int vx, vy;
        public float charge;
        public PointCharge(int _x, int _y, int _vx, int _vy, float _charge)
        {
            x = _x;
            y = _y;
            vx = _vx;
            vy = _vy;
            charge = _charge;
        }
    }
    class Chamber
    {
        public int width, height;
        public int maxSpeed;
        public int time;
        public List<PointCharge> pointCharges;
        public Random random;
        public Image gif;
        public Chamber(int _w, int _h)
        {
            width = _w;
            height = _h;
            time = 0;
            maxSpeed = (int)Math.Sqrt(_w * _w + _h * _h) / 64;
            pointCharges = new List<PointCharge>();
            random = new Random();
            gif = new Image<Rgba32>(_w, _h);
        }
        public void AddCharge(PointCharge charge)
        {
            if (charge.x < 0 || charge.y < 0 || !(charge.x < width) || !(charge.y < height)) throw new Exception("Charge must be inside the box");
            pointCharges.Add(charge);
        }
        public void AddRandomCharges(float minCharge, float maxCharge, int amount = 1)
        {
            for (int i = 0; i < amount; i++)
            {
                int speed = random.Next( maxSpeed / 2 , maxSpeed );
                double angle = 2 * Math.PI * random.NextDouble();
                int vx = (int)(speed * Math.Cos(angle));
                int vy = (int)(speed * Math.Sin(angle));
                float charge = minCharge + (maxCharge - minCharge) * (float)random.NextDouble();
                Console.WriteLine("Charge:( v:({0},{1}), charge:{2}", vx, vy, (int)charge);
                AddCharge(new PointCharge(random.Next(width), random.Next(height), vx, vy, charge));
            }
        }
        public float Value(int x, int y)
        {
            float totalStrenth = 0;
            foreach(PointCharge charge in pointCharges)
            {
                float dx = (float)x - charge.x;
                float dy = (float)y - charge.y;
                totalStrenth += charge.charge / (dx * dx + dy * dy); 
            } 
            return totalStrenth;
        }
        public void CalculateTimeStep()
        {
            foreach(PointCharge charge in pointCharges)
            {
                charge.x += charge.vx;
                if (charge.x < 0)
                {
                    charge.x *= -1;
                    charge.vx *= -1;
                }
                else if (charge.x >= width)
                {
                    charge.x = 2 * width - charge.x;
                    charge.vx *= -1;
                }
                charge.y += charge.vy;
                if (charge.y < 0)
                {
                    charge.y = 0 - charge.y;
                    charge.vy *= -1;
                }
                else if (charge.y >= height)
                {
                    charge.y = 2 * height - charge.y;
                    charge.vy *= -1;
                }
            }
            time++;
        }
        public void RenderFrame()
        {
            using (Image<Rgba32> image = new Image<Rgba32>(width, height))
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Span<Rgba32> pixelRowSpan = image.GetPixelRowSpan(y);
                    for (int x = 0; x < image.Width; x++)
                    {
                        pixelRowSpan[x] = Color(Value(x, y));
                    }
                }
                image.SaveAsPng("output\\frame_" + time + ".png");
                image.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = 4;
                gif.Frames.InsertFrame(time, image.Frames.RootFrame);
            }
            CalculateTimeStep();
        }
        Rgba32 Color(float value)
        {
            byte r, g, b;
            r = Clamp(Math.Sqrt(40 * value));
            b = Clamp(1020 / Math.Pow(value, 1.0 / 3.0 ));
            g = (byte)((65536 - r * r - b * b > 0) ? Math.Sqrt(65536 - r * r - b * b) : 0);
            /*
            b = Math.Min((int)(256 / value), 255);
            r = Math.Min((int)(value * value), 256);
            g = (1.02 < value && value < 15.9) ? (int)Math.Sqrt(65536 - r * r - b * b) : 0;
            /**/
            return new Rgba32((byte)r, (byte)g, (byte)b, (byte)255);
        }
        byte Clamp(double val)
        {
            if (val > 255) return 255;
            if (val < 0) return 0;
            return (byte)val;
        }
    }
}
