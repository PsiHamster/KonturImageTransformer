using System.Drawing;

namespace Kontur.ImageTransformer.ImageTransformer {
    internal  interface ITransformer {
        /// <param name="original">Original image bitmap</param>
        /// <param name="resultCoords">После преобразования изображение произойдет обрезка по переданным координатам</param>
        /// <param name="dir">Направление поворота</param>
        Bitmap RotateImage(Bitmap original, RectangleCoords resultCoords, RotateDirection dir);

        /// <param name="original">Original image bitmap</param>
        /// <param name="resultCoords">После преобразования изображение произойдет обрезка по переданным координатам</param>
        /// <param name="dir">Направление отражения</param>
        Bitmap FlipImage(Bitmap original, RectangleCoords resultCoords, FlipDirection dir);
    }
    
    internal enum RotateDirection {
        Right, Left
    }

    internal enum FlipDirection {
        Horizontal, Vertical
    }
}