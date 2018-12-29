using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WoodFitting2
{
    public class Part
    {
        public string ID;
        public double Width;
        public double Length;
        public double Area;

        public bool Packed;

        public Part() { }

        public Part(Part original)
        {
            ID = original.ID;
            Length = original.Length;
            Width = original.Width;
            Area = original.Area;
        }

        public Part(string id, double length, double width, double dlength = 0, double dwidth = 0)
        {
            ID = id;
            Length = length;
            Width = width;
            Area = Length * Width;
        }

        public override string ToString()
        {
            return $"{ID} [{Length,7:0.0} x {Width,5:0.0}]";
        }

        public void Inflate(double deltaWidth, double deltaLength)
        {
            Width += 2 * deltaWidth;
            Length += 2 * deltaLength;
        }
    }

    public class Board
    {
        public string ID;
        public double Width;
        public double Length;
        public double Area;
        public double dWidth;
        public double dLength;
        public Board AssociatedBoard;


        public Part[] PackedParts;
        public double[] PartdLengths;
        public double[] PartdWidths;
        public double PackedArea;
        public int PartsCount;
        public bool Complete;
        public bool Disabled;

        public Board()
        { }

        public Board(Board original)
        {
            ID = original.ID;
            dWidth = original.dWidth;
            dLength = original.dLength;
            Length = original.Length;
            Width = original.Width;
            Area = original.Area;
        }

        public Board(string id, double length, double width, double dlength = 0, double dwidth = 0)
        {
            ID = id;
            dWidth = dwidth;
            dLength = dlength;
            Length = length;
            Width = width;
            Area = Length * Width;
        }

        public override string ToString()
        {
            if (dLength != 0 || dWidth != 0)
                return $"{ID} [{Length,7:0.0} x {Width,5:0.0}] @ ({dLength,7:0.0}, {dWidth,5:0.0})";

            return $"{ID} [{Length,7:0.0} x {Width,5:0.0}]";
        }

    }

}
