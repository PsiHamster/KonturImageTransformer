using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer.Handlers {
    internal class ProcessImageHandler : IRequestHandler {
        /// <inheritdoc />
        public Task HandleAsync(HttpListenerContext context, string paramsString) {
            return null;
        }

        //public ProcessImageHandler(ITransforemer transforemer) {
        //    
        //}
    }
}