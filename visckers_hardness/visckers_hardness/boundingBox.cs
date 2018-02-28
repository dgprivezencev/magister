using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace visckers_hardness
{
    class boundingBox
    {
        public int minX, minY, maxX, maxY;

        public boundingBox(int width, int heigth)
        {
            minX = width;
            minY = heigth;
            maxX = 0;
            maxY = 0;
        }

        public Rectangle getRectangle()
        {
            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }
    }
}
