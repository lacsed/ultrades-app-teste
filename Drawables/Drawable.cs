using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoAVL.Settings;

namespace AutoAVL.Drawables
{
    public interface Drawable
    {
        public Box GetBox(DrawingDir drawingDir);
        public string ToSvg(DrawingDir drawingDir, SvgCanvas canvas);
        public string ToLatex(DrawingDir drawingDir);
    }
}
