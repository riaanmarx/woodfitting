using System.Collections.Generic;
using System.Linq;

namespace WoodFitting2
{
    public class Part
    {
        public string Name { get; set; }
        public double Width { get; set; }
        public double Length { get; set; }
        public double Area { get; set; }

        public double Offset_Width { get; set; }
        public double Offset_Length { get; set; }

        public Part(string name, double length, double width)
        {
            Name = name;
            Length = length;
            Width = width;
            Area = Length * Width;
        }

        public Part(string name, double offest_length, double offset_width, double length, double width)
        {
            Name = name;
            Offset_Width = offset_width;
            Offset_Length = offest_length;
            Length = length;
            Width = width;
            Area = Length * Width;
        }

        public Part Copy() => new Part(Name, Offset_Length, Offset_Width, Length, Width);

        double kerf = 4;
        public bool SmallerThan(Item board)
        {
            return !(Length + kerf > board.Length || Width + kerf > board.Width);
        }

        public static int CompareByAreaDecending(Part w1, Part w2) => (int)(w2.Area - w1.Area);
                
        public override string ToString()
        {
            if (Offset_Length != 0 || Offset_Width != 0)
                return $"{Name} [{Length} x {Width}] @ [{Offset_Length}, {Offset_Width}]";

            return $"{Name} [{Length} x {Width}]";
        }

    }

    public class PartList : List<Part>
    {
        public PartList() : base() { }
        public PartList(IEnumerable<Part> source) : base(source) { }

        public void Add(string name, double length, double width)
        {
            this.Add(new Part(name, length, width));
        }
        public PartList Copy(bool deep = false)
        {
            if (deep)
                return new PartList(this.Select(p => p.Copy()));
            return new PartList(this);
        }
    }
}
