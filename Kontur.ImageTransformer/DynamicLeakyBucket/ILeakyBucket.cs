using System;

namespace Kontur.ImageTransformer.DynamicLeakyBucket
{
    internal interface ILeakyBucket {
        /// <summary>
        /// Проверка на то, можно ли выполнить запрос, или стоит
        /// отклонить его из-за высокой нагрузки.
        /// </summary>
        /// <param name="recentRequestTime">Время выполнения последних N запросов</param>
        bool Check(Tuple<int, double> recentRequestTime);
    }
}
