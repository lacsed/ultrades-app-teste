using AutoAVL.Drawables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AutoAVL
{
    public class SvgCanvas
    {
        private double canvasWidth;
        private double canvasHeight;
        private Vector2D canvasOrigin;

        public SvgCanvas()
        {
            canvasOrigin = new Vector2D();
        }

        public Vector2D SVGOrigin()
        {
            return canvasOrigin;
        }

        public void MoveOrigin(Vector2D displacement)
        {
            canvasOrigin += displacement;
        }

        public void ChangeOrigin(Vector2D newOrigin)
        {
            canvasOrigin = newOrigin;
        }

        public void SetUpCanvas(Box canvasBox)
        {
            canvasOrigin = canvasBox.GetTopLeft();
            canvasWidth = canvasBox.Width();
            canvasHeight = canvasBox.Height();
        }

        public string SvgDimensions()
        {
            return "<svg height=\"" + canvasHeight + "\" width=\"" + canvasWidth + "\" version=\"1.1\" xmlns=\"http://www.w3.org/2000/svg\"" + ">" + Environment.NewLine;
        }

        public string SvgSettings()
        {
            return "<defs><marker id=\"arrowhead\" markerWidth=\"10\" markerHeight=\"7\" refX = \"7\" refY = \"3.5\" orient = \"auto\" ><polygon points=\"0 0, 10 3.5, 0 7\" /></marker></defs>" + Environment.NewLine;
        }

        public Vector2D ToSvgCoordinates(Vector2D v)
        {
            return new Vector2D(v.x - canvasOrigin.x, canvasOrigin.y - v.y);
        }
    }
}
