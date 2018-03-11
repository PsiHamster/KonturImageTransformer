using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer.ImageTransformer
{
    internal class Transformer : ITransformer{
        /// <inheritdoc />
        public Bitmap RotateImage(Bitmap original, RectangleCoords resultCoords, RotateDirection dir) {
            if (dir != RotateDirection.Left && dir != RotateDirection.Right) {
                // если добавляются новые направления поворота, вычисление границ поменяется
                throw new NotImplementedException();
            }

            // При повороте поменяется высота с шириной.
            if (!resultCoords.IntersectWith(original.Height, original.Width)) {
                return null;
            }
            resultCoords.Intersect(original.Height, original.Width);

            switch (dir) {
                case RotateDirection.Left:
                    original.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    break;
                case RotateDirection.Right:
                    original.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    break;
            }

            return original.Clone(
                new Rectangle(
                    resultCoords.X,
                    resultCoords.Y,
                    resultCoords.Width,
                    resultCoords.Height),
                original.PixelFormat);
        }

        /// <inheritdoc />
        public Bitmap FlipImage(Bitmap original, RectangleCoords resultCoords, FlipDirection dir) {
            // Так как при отражении итоговые размеры картинки не меняются, имеем право сразу узнать
            // координаты итоговой области.
            if (!resultCoords.IntersectWith(original.Width,original.Height)) {
                return null;
            }
            resultCoords.Intersect(original.Width, original.Height); 

            switch (dir) {
                case FlipDirection.Horizontal:
                    original.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    break;
                case FlipDirection.Vertical:
                    original.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    break;
                default:
                    throw new ArgumentException("Invalid Flip direction");
            }

            return original.Clone(
                new Rectangle(
                    resultCoords.X,
                    resultCoords.Y,
                    resultCoords.Width,
                    resultCoords.Height),
                original.PixelFormat);
        }
    }
}
