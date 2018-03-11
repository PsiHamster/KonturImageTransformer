using Kontur.ImageTransformer.DynamicLeakyBucket;
using Kontur.ImageTransformer.Handlers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer
{

    internal class AsyncHttpServer : IDisposable
    {

        public AsyncHttpServer(Dictionary<string, IRequestHandler> routes) {
            listener = new HttpListener();
            leakyBucket = new LeakyBucket(100, 200, 2000, Environment.ProcessorCount * 10);
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

                        if (leakyBucket.Check(GetRecentTime())) {
                            var startRequestTimer = new Stopwatch();
                            startRequestTimer.Start();

                            ThreadPool.QueueUserWorkItem((x) => {
                                var c = HandleContextAsync((HttpListenerContext) x);
                                c.ContinueWith((y) => {
                                    startRequestTimer.Stop();
                                    recentEllapsedMs.Enqueue(startRequestTimer.ElapsedMilliseconds);
                                });
                            }, context);
                        } else {
                            AbortRequest(context);
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

        private void AbortRequest(HttpListenerContext listenerContext,
            HttpStatusCode code = HttpStatusCode.ServiceUnavailable) {
            listenerContext.Response.StatusCode = (int)code;
            listenerContext.Response.Close();
        }

        private Tuple<int, double> GetRecentTime() {
            long sum = 0;
            int count = 0;
            while (!recentEllapsedMs.IsEmpty) {
                if (recentEllapsedMs.TryDequeue(out long result)) {
                    sum += result;
                    count++;
                }
            }
            if (count == 0)
                return new Tuple<int, double>(0, -1);
            return new Tuple<int, double>(count, sum / count);
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