using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Kontur.ImageTransformer.DynamicLeakyBucket;
using Kontur.ImageTransformer.Handlers;
//using NLog;
//using NLog.Fluent;

namespace Kontur.ImageTransformer
{

    internal class AsyncHttpServer : IDisposable
    {

        public AsyncHttpServer(Dictionary<string, IRequestHandler> routes) {
           // ServicePointManager.DefaultConnectionLimit = 100;
            listener = new HttpListener();
            leakyBucket = new LeakyBucket(150, 400, 2000, Environment.ProcessorCount * 10);
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
                        var startRequestTimer = new Stopwatch();
                        startRequestTimer.Start();

                        if (leakyBucket.Check(GetRecentTime())) {
                            ThreadPool.UnsafeQueueUserWorkItem((x) => {
                                var c = HandleContextAsync((HttpListenerContext) x, startRequestTimer);
                            }, context);
                        } else {
                            AbortRequestAsync(context);
                        }
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
            await HandleContextAsync(listenerContext, new Stopwatch());
        }

        private async Task HandleContextAsync(HttpListenerContext listenerContext, Stopwatch startRequestTimer) {
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
            startRequestTimer.Stop();
            recentEllapsedMs.Enqueue(startRequestTimer.ElapsedMilliseconds);
        }

        private async Task AbortRequestAsync(HttpListenerContext listenerContext,
            HttpStatusCode code = HttpStatusCode.ServiceUnavailable) {
            listenerContext.Response.StatusCode = (int)code;
            listenerContext.Response.Close();
        }

        private long GetRecentTime() {
            long sum = 0;
            long count = 0;
            while (!recentEllapsedMs.IsEmpty) {
                if (recentEllapsedMs.TryDequeue(out long result)) {
                    sum += result;
                    count++;
                }
            }
            if (count == 0)
                return -1;
            return sum / count;
        }

        private readonly HttpListener listener;
        private readonly ILeakyBucket leakyBucket;
        private readonly Dictionary<string, IRequestHandler> routes;
        private ConcurrentQueue<long> recentEllapsedMs = new ConcurrentQueue<long>(); 

        private Thread listenerThread;
        private bool disposed;
        private volatile bool isRunning;
    }
}