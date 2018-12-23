using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WoodFitting2
{
    class ComboList : List<Combo>
    { }

    class Combo : List<Part>
    {
        public double CumalativeArea { get; set; } = 0;

        public Combo(params Part[] members) : base(members)
        {
            CumalativeArea = members.Sum(t => t.Area);
        }

        public new void Add(Part part)
        {
            CumalativeArea += part.Area;
            Insert(0, part);
        }

        public static int CompareByCumAreaDesc(Combo w1, Combo w2)
        {
            return (int)(w2.CumalativeArea - w1.CumalativeArea);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in this)
                sb.Append($"{item.Name},");
            return sb.ToString();
        }

        public PartList AsPartList()
        {
            return new PartList(this);
        }
    }
}
