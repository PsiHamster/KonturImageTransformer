using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer.Handlers {
    internal interface IRequestHandler {
        /// <param name="context">Содержит контекст запроса</param>
        /// <param name="paramsArr">Часть пути, оставшаяся после
        /// обрезания URI. (Например от запроса /process/blabla к обработчику process,
        /// останется [blabla])</param>
        Task HandleAsync(HttpListenerContext context, string[] paramsArr);
    }
}
