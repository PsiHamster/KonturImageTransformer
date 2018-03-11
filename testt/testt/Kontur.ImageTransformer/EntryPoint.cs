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
            using (var server = new AsyncHttpServer(new Dictionary<string, IRequestHandler>() {
                { "/process", new ProcessImageHandler(new Transformer()) }
            }))
            {
                server.Start("http://+:8080/");

                Console.ReadKey(true);
            }
        }
    }
}
