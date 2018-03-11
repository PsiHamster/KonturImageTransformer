using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Kontur.ImageTransformer.Handlers;
//using NLog;
//using NLog.Fluent;

namespace Kontur.ImageTransformer
{

    internal class AsyncHttpServer : IDisposable
    {

        public AsyncHttpServer(Dictionary<string, IRequestHandler> routes) {
            listener = new HttpListener();
            this.routes = routes;
        }

        public void Start(string prefix) {
            lock (listener)
            {
                if (!isRunning)
                {
                    listener.Prefixes.Clear();
                    listener.Prefixes.Add(prefix);
                    listener.Start();

                    listener.TimeoutManager.RequestQueue = TimeSpan.FromMilliseconds(1000);

                    listenerThread = new Thread(Listen) {
                        IsBackground = true,
                        Priority = ThreadPriority.Highest
                    };
                    listenerThread.Start();

                    isRunning = true;
                    Console.WriteLine("Started"); // TODO: NLog
                }
            }
        }

        public void Stop() {
            lock (listener)
            {
                if (!isRunning)
                    return;

                listener.Stop();

                listenerThread.Abort();
                listenerThread.Join();

                isRunning = false;
            }
        }

        public void Dispose() {
            if (disposed)
                return;

            disposed = true;

            Stop();

            listener.Close();
        }

        private async void Listen() {
            while (true)
            {
                try {
                    if (listener.IsListening) {
                        var context = await listener.GetContextAsync();
                        
                        Task.Run(() => HandleContextAsync(context));
                        /*new Task(() => {
                            var e = DateTime.Now;
                            var g = HandleContextAsync(context);
                            g.Wait();
                            Console.WriteLine($"{(DateTime.Now - e).Milliseconds} {(DateTime.Now - e).Milliseconds}");
                        });*/
                    } else
                        Thread.Sleep(0);
                } catch (ThreadAbortException) {
                    return;
                } catch (Exception e) {
                    Console.WriteLine(e);
                }
            }
        }

        private async Task HandleContextAsync(HttpListenerContext listenerContext) {
            var uri = listenerContext.Request.Url;
            IRequestHandler handler = null;
            var paramsArr = new List<string>();

            foreach (var e in routes) {
                if (uri.AbsolutePath.ToLower().StartsWith(e.Key)) {
                    handler = e.Value;
                    paramsArr = uri.AbsolutePath.Substring(e.Key.Length + 1, // e.Key == "params", cutting "params/"
                        uri.AbsolutePath.Length - e.Key.Length - 1).Split('/').Where(x => x!=string.Empty).ToList();
                }
            }

            if (handler == null) {
                listenerContext.Response.StatusCode = (int) HttpStatusCode.NotFound;
            } else {
                try {
                    await handler.HandleAsync(listenerContext, paramsArr.ToArray());
                } catch (Exception e) {
                    Console.WriteLine(e);
                    listenerContext.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
                }
            }

            listenerContext.Response.Close();
        }

        private readonly HttpListener listener;
        private readonly Dictionary<string, IRequestHandler> routes;

        private Thread listenerThread;
        private bool disposed;
        private volatile bool isRunning;
    }
}