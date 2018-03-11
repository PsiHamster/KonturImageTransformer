using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer.ImageTransformer
{
    internal class RectangleCoords {
        public int X { get; private set; }
        public int Y { get; private set; }

        public int Width { get; private set; }
        public int Height { get; private set; }

        /// <summary>
        /// Обрезает этот прямоугольник прямоугольником 0, 0, width, height
        /// </summary>
        public void Intersect(int width, int height) {
            if (width < 0 || height < 0) {
                throw new ArgumentException("Width and height should be >=0");
            }

            var x1 = Math.Max(X, 0);
            var y1 = Math.Max(Y, 0);
            var x2 = Math.Min(X + Width, width);
            var y2 = Math.Min(Y + Height, height);

            X = x1;
            Y = y1;
            Width = x2 - x1;
            Height = y2 - y1;
        }

        /// <summary>
        /// <c>true</c>, если пересечение c прямоугольником 0, 0, width, height - не пустой прямоугольник.
        /// </summary>
        public bool IntersectWith(int width, int height) =>
            X < width && X + Width > 0 && Y < height && Y + Height > 0;

        public RectangleCoords(int x, int y, int width, int height) {
            if (width < 0) {
                y += width;
                width = -width;
            }
            if (height < 0) {
                x += height;
                height = -height;
            }

            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}
