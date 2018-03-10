using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Kontur.ImageTransformer.Handlers;
using NLog;
using NLog.Fluent;

namespace Kontur.ImageTransformer
{

    internal class AsyncHttpServer : IDisposable
    {

        public AsyncHttpServer(Dictionary<string, IRequestHandler> routes) {
            listener = new HttpListener();
            this.routes = routes;
            log = LogManager.GetCurrentClassLogger();
        }

        public void Start(string prefix) {
            lock (listener)
            {
                if (!isRunning)
                {
                    listener.Prefixes.Clear();
                    listener.Prefixes.Add(prefix);
                    listener.Start();

                    listenerThread = new Thread(Listen) {
                        IsBackground = true,
                        Priority = ThreadPriority.Highest
                    };
                    listenerThread.Start();

                    isRunning = true;
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

        private void Listen() {
            while (true)
            {
                try
                {
                    if (listener.IsListening)
                    {
                        var context = listener.GetContext();
                        Task.Run(() => HandleContextAsync(context));
                    } else
                        Thread.Sleep(0);
                } catch (ThreadAbortException)
                {
                    return;
                } catch (Exception e) {
                    log.Error(e);
                }
            }
        }

        private async Task HandleContextAsync(HttpListenerContext listenerContext) {
            var uri = listenerContext.Request.Url;
            IRequestHandler handler = null;
            string paramsString = string.Empty;
            foreach (var e in routes) {
                if (uri.AbsolutePath.ToLower().StartsWith(e.Key)) {
                    handler = e.Value;
                    paramsString = uri.AbsolutePath.Substring(e.Key.Length + 1, // e.Key == "params", cutting "params/"
                        uri.AbsolutePath.Length - e.Key.Length - 1);
                }
            }

            if (handler == null) {
                listenerContext.Response.StatusCode = (int) HttpStatusCode.NotFound;
            } else {
                try {
                    await handler.HandleAsync(listenerContext, paramsString);
                } catch (Exception e) {
                    log.Error(e);
                    listenerContext.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
                }
            }

            listenerContext.Response.Close();
        }

        private readonly HttpListener listener;
        private Dictionary<string, IRequestHandler> routes;
        private Logger log;

        private Thread listenerThread;
        private bool disposed;
        private volatile bool isRunning;
    }
}