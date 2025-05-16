using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoAVL.Drawables
{
    public class Box
    {
        private Vector2D topLeft;
        private Vector2D bottomRight;

        public Box()
        {
            this.topLeft = new Vector2D();
            this.bottomRight = new Vector2D();
        }

        public Box(Vector2D topLeft, Vector2D bottomRight)
        {
            this.topLeft = topLeft;
            this.bottomRight = bottomRight;
        }

        public static Box EncompassingBox(Box a, Box b)
        {
            double minX = (a.topLeft.x < b.topLeft.x) ? a.topLeft.x : b.topLeft.x;
            double maxX = (a.bottomRight.x > b.bottomRight.x) ? a.bottomRight.x : b.bottomRight.x;
            double minY = (a.bottomRight.y < b.bottomRight.y) ? a.bottomRight.y : b.bottomRight.y;
            double maxY = (a.topLeft.y > b.topLeft.y) ? a.topLeft.y : b.topLeft.y;

            Vector2D topLeft = new Vector2D(minX, maxY);
            Vector2D bottomRight = new Vector2D(maxX, minY);

            return new Box(topLeft, bottomRight);
        }

        public static Box EncompassingBox(Vector2D a, Vector2D b)
        {
            double minX = Math.Min(a.x, b.x);
            double maxX = Math.Max(a.x, b.x);

            double minY = Math.Min(a.y, b.y);
            double maxY = Math.Max(a.y, b.y);

            Vector2D topLeft = new Vector2D(minX, maxY);
            Vector2D bottomRight = new Vector2D(maxX, minY);

            return new Box(topLeft, bottomRight);
        }

        public static Box EncompassingBox(Vector2D a, Vector2D b, Vector2D c)
        {
            double minX = Math.Min(Math.Min(a.x, b.x), c.x);
            double maxX = Math.Max(Math.Max(a.x, b.x), c.x);

            double minY = Math.Min(Math.Min(a.y, b.y), c.y);
            double maxY = Math.Max(Math.Max(a.y, b.y), c.y);

            Vector2D topLeft = new Vector2D(minX, maxY);
            Vector2D bottomRight = new Vector2D(maxX, minY);

            return new Box(topLeft, bottomRight);
        }

        public Vector2D GetTopLeft()
        {
            return topLeft;
        }

        public Vector2D GetBottomRight()
        {
            return bottomRight;
        }

        public double Width()
        {
            return  Math.Abs(topLeft.x - bottomRight.x);
        }

        public double Height()
        {
            return  Math.Abs(topLeft.y - bottomRight.y);
        }
    }
}
