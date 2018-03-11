using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer.DynamicLeakyBucket
{
    internal class LeakyBucket : ILeakyBucket {
        /// <inheritdoc />
        public bool Check(double averageRequestTime = -1) {

            var delta = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - lastCheckRequest);
            lastCheckRequest = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            if(delta > 0)
                i = Math.Min(bucketSize, i + rps * delta / 1000 );

            if (averageRequestTime > maxAvReqTime) {
                rps = Math.Max(minRps, rps -= averageRequestTime/maxAvReqTime);
            }

            if (i < 1) {
                if (averageRequestTime > 0 && averageRequestTime < minAvReqTime) {
                    rps += bucketSize / 2 / rps;
                    return true;
                }
                return false;
            } else {
                i -= 1;
                return true;
            }
        }

        /// <param name="minAvReqTime">Среднее время обработки запроса,
        /// ниже которого будет происходить увеличение размеров ведра,
        /// если кончаются токены</param>
        /// <param name="maxAvReqTime">Максимальное время обработки запроса,
        /// больше которого начнется урезание токенов.</param>
        /// <param name="bucketSize">Максимальное количество токенов в ведре</param>
        /// <param name="minRps">Минимальное количество токенов добавляемое каждую секунду</param>
        public LeakyBucket(int minAvReqTime = 150, int maxAvReqTime = 500, int bucketSize = 1000, int minRps = 50) {
            this.minAvReqTime = minAvReqTime;
            this.maxAvReqTime = maxAvReqTime;
            this.bucketSize = bucketSize;
            this.minRps = minRps;
            this.rps = bucketSize / 2;
            i = bucketSize;

            lastCheckRequest = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private double i;

        private readonly int minAvReqTime;
        private readonly int maxAvReqTime;
        private readonly double bucketSize;
        private readonly int minRps;
        private double rps;

        private long lastCheckRequest;
    }
}

