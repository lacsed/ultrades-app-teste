using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoAVL.Drawables;

namespace AutoAVL.Settings
{
    public class DrawingDir
    {
        // Node settings.
        public double nodeRadius = 40.0f;
        public double autoLinkRadius = 20.0f;
        public double autoLinkDistanceRatio = 0.8f;
        public double borderWidth = 2.0f;
        public double markedRatio = 0.8f;
        public double linkRatio = 0.2f;
        public double arcSize = 20.0f;

        // Colors
        public string strokeColor = "black";
        public string textColor = "black";

        // Strokes
        public string strokeFill = "none";

        // Text
        public double textSize = 25.0f;

        public double arrowLength = 10.0f;
        public double overlap = 0.2f;
        public double textDistance = 20.0f;
        public double arrowWidth = 20.0f;
        public string arrowColor = "black";
        public double linkStrokeWidth = 2.0f;
        public double clipRatio = 0.2f;
        public double initialLinkSize = 100.0f;

        public double ticLength = 10.0;

        public double TotalRadius()
        {
            return nodeRadius + borderWidth;
        }

        public double AutoRadius()
        {
            return autoLinkRadius + borderWidth;
        }

    }
}
