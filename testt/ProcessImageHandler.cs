using Kontur.ImageTransformer.ImageTransformer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer.Handlers {
    internal class ProcessImageHandler : IRequestHandler {
        /// <inheritdoc />
        public async void HandleAsync(HttpListenerContext context, string[] paramsArr) {
            var request = context.Request;
            var response = context.Response;

            Func<Bitmap, Bitmap> transFunc;

            RectangleCoords rectangleCoords;

            if (paramsArr.Length != 2 || request.ContentLength64 > 100 * 8 * 1024 || request.ContentLength64 == 0 || !request.HasEntityBody) {
                response.StatusCode = 400;
                return;
            }

            try {
                rectangleCoords = ParseCoords(paramsArr[1]);
            } catch (Exception e) {
                response.StatusCode = 400;
                return;
            }

            switch (paramsArr[0]) {
                case "rotate-cw":
                    transFunc = x => transformer.RotateImage(x, rectangleCoords, RotateDirection.Right);
                    break;
                case "rotate-ccw":
                    transFunc = x => transformer.RotateImage(x, rectangleCoords, RotateDirection.Left);
                    break;
                case "flip-h":
                    transFunc = x => transformer.FlipImage(x, rectangleCoords, FlipDirection.Horizontal);
                    break;
                case "flip-v":
                    transFunc = x => transformer.FlipImage(x, rectangleCoords, FlipDirection.Vertical);
                    break;
                default:
                    response.StatusCode = 400;
                    return;
            }

            var image = await LoadImage(request.InputStream, (int)request.ContentLength64);

            if (image == null) {
                response.StatusCode = 400;
                return;
            }

            var resultImage = transFunc(image);

            if (resultImage == null) {
                response.StatusCode = 204;
            } else {
                response.StatusCode = 200;
                await WriteImageAsync(response, resultImage);
            }
        }

        public ProcessImageHandler(ITransformer transforemer) {
            this.transformer = transforemer;
        }

        private async Task<Bitmap> LoadImage(Stream stream, int length) {
            Byte[] bytes;
            
            using (BinaryReader r = new BinaryReader(stream)) {
                bytes = r.ReadBytes(length);
            }
            stream.Close();
            Bitmap picture;

            using (var e = new MemoryStream(bytes))
            {
               
                //await stream.CopyToAsync(e);
                //e.Position = 0;
                picture = new Bitmap(e);
            }

            return picture;
        }

        private async Task WriteImageAsync(HttpListenerResponse response, Bitmap image) {         
            using (var e = new MemoryStream()) {
                var responseStream = response.OutputStream;
                image.Save(e, ImageFormat.Png);
                image.Save("test.png");
                image.Save("test2.png", ImageFormat.Png);

                //await e.CopyToAsync(responseStream);
                byte[] bytes = new byte[e.Length];
                await e.ReadAsync(bytes, 0, bytes.Length);
                response.ContentLength64 = bytes.Length;
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

        private ITransformer transformer;
    }
}