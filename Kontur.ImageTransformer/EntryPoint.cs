using System;
using System.Collections.Generic;
using System.Drawing;
using Kontur.ImageTransformer.Handlers;
using Kontur.ImageTransformer.ImageTransformer;

namespace Kontur.ImageTransformer
{
    public class EntryPoint
    {
        public static void Main(string[] args) {
            var rect = new RectangleCoords(5, 5, -10, -10);

            Console.WriteLine(rect.X);
            Console.WriteLine(rect.Y);
            Console.WriteLine(rect.Width);
            Console.WriteLine(rect.Height);

            rect.Intersect(6,3);

            Console.WriteLine(rect.X);
            Console.WriteLine(rect.Y);
            Console.WriteLine(rect.Width);
            Console.WriteLine(rect.Height);
            /*
            using (var server = new AsyncHttpServer(new Dictionary<string, IRequestHandler>()))
            {
                server.Start("http://+:8080/");

                Console.ReadKey(true);
            }*/
        }
    }
}
