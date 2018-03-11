using Kontur.ImageTransformer.ImageTransformer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer.Handlers {
    internal class ProcessImageHandler : IRequestHandler {
        /// <inheritdoc />
        public async Task HandleAsync(HttpListenerContext context, string[] paramsArr) {
            var request = context.Request;
            var response = context.Response;

            if (paramsArr.Length != 2 || !IsRequestValid(request)) {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            try {
                var rectangleCoords = ParseCoords(paramsArr[1]);
                var transFunc = GetTransformFunction(paramsArr[0], rectangleCoords);
                var image = await LoadImage(request.InputStream, (int) request.ContentLength64);

                if (image == null) {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return;
                }

                var resultImage = transFunc(image);

                if (resultImage == null) {
                    response.StatusCode = (int)HttpStatusCode.NoContent;
                } else {
                    response.StatusCode = (int)HttpStatusCode.OK;
                    await WriteImageAsync(response, resultImage);
                }
            } catch (ArgumentException e) {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
            } catch (Exception e) {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                Console.WriteLine(e);
            }
        }

        public ProcessImageHandler(ITransformer transformer) {
            this.transformer = transformer;
        }

        private bool IsRequestValid(HttpListenerRequest request) =>
            !(request.ContentLength64 > 100 * 8 * 1024 || request.ContentLength64 == 0 || !request.HasEntityBody);

        private Func<Bitmap, Bitmap> GetTransformFunction(string name, RectangleCoords rectangleCoords) {
            switch (name)
            {
                case "rotate-cw":
                    return x => transformer.RotateImage(x, rectangleCoords, RotateDirection.Right);
                case "rotate-ccw":
                    return x => transformer.RotateImage(x, rectangleCoords, RotateDirection.Left);
                case "flip-h":
                    return x => transformer.FlipImage(x, rectangleCoords, FlipDirection.Horizontal);
                case "flip-v":
                    return x => transformer.FlipImage(x, rectangleCoords, FlipDirection.Vertical);
                default:
                    throw new ArgumentException("Invalid Transform Name");
            }
        }

        private async Task<Bitmap> LoadImage(Stream stream, int length) {
            var bytes = new byte[length];
            await stream.ReadAsync(bytes, 0, length);
            stream.Close();

            Bitmap picture;

            using (var e = new MemoryStream(bytes))
            {
                picture = new Bitmap(e);
            }

            return picture;
        }

        private async Task WriteImageAsync(HttpListenerResponse response, Bitmap image) {         
            using (var e = new MemoryStream()) {
                var responseStream = response.OutputStream;
                image.Save(e, ImageFormat.Png);
                e.Position = 0;
                // await e.CopyToAsync(responseStream);
                byte[] bytes = new byte[e.Length];

                await e.ReadAsync(bytes, 0, bytes.Length);
                response.ContentLength64 = bytes.Length;
                response.ContentType = "img/png";
                await responseStream.WriteAsync(bytes, 0, bytes.Length);
                await responseStream.FlushAsync();
                
                responseStream.Close();
            }
        }

        private RectangleCoords ParseCoords(string s) {
            var g = s.Split(',');
            if (g.Length != 4) throw new ArgumentException("String should be like x,y,w,h");
            var x = int.Parse(g[0]);
            var y = int.Parse(g[1]);
            var width = int.Parse(g[2]);
            var height = int.Parse(g[3]);

            return new RectangleCoords(x,y,width,height);
        }

        private readonly ITransformer transformer;
    }
}