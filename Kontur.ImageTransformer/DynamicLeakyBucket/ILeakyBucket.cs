namespace Kontur.ImageTransformer.DynamicLeakyBucket
{
    internal interface ILeakyBucket {
        /// <summary>
        /// Проверка на то, можно ли выполнить запрос, или стоит
        /// отклонить его из-за высокой нагрузки.
        /// </summary>
        /// <param name="averageRequestTime">Среднее время выполнения последних N запросов</param>
        bool Check(double averageRequestTime = -1);
    }
}
