using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace WoodFitting2
{

    public class Solution : List<Placement>
    {
        public Solution()
        {
        }
        public Solution(Part part, Item stock)
        {
            this.Add(new Placement(part, stock));
            PlacedArea += part.Area;
        }


        public double Waste => UsedStockArea - PlacedArea;
        public double UsedStockArea { get; set; }
        public double TotalStockArea { get; set; }
        public double PlacedArea { get; set; }

        public void Add(Part part, Item stock)
        {
            base.Add(new Placement(part, stock));
            PlacedArea += part.Area;
        }
        public void AddRange(Solution solution)
        {
            base.AddRange(solution);
            UsedStockArea += solution.UsedStockArea;
            PlacedArea += solution.PlacedArea;
        }
        
        public void Print(BoardList stock, long durationms)
        {
            Trace.WriteLine($"Solution summary");
            Trace.WriteLine($"----------------");
            Trace.WriteLine($"   Processing time: {durationms / 1000} s");
            Trace.WriteLine($"   #Boards        : {stock.Count}");
            Trace.WriteLine($"   #Parts         : {this.Count}");
            Trace.WriteLine($"   Total Stock    : {TotalStockArea / 1000000} m\u00b2");
            Trace.WriteLine($"   Used Stock     : {UsedStockArea / 1000000} m\u00b2");
            Trace.WriteLine($"   Parts placed   : {PlacedArea / 1000000} m\u00b2");
            Trace.WriteLine($"   Waste          : {(Waste / 1000000)} m\u00b2 ({(Waste / UsedStockArea):0.0 %})");
            Trace.WriteLine($"Part placements:");
            Trace.WriteLine($"----------------");
            foreach (var iBoard in stock)
            {
                Trace.WriteLine($"   Board {iBoard} ({(this.Sum(t=>t.Stock.Name == iBoard.Name? t.Part.Area : 0) / iBoard.Area * 100):0.0} %):");
                foreach (var iPlcmnt in this.Where(t => t.Stock.Name.StartsWith(iBoard.Name)).OrderBy(t => t.Part.Name))
                    Trace.WriteLine($"     {iPlcmnt.Part.Name} [{iPlcmnt.Part.Length} x {iPlcmnt.Part.Width}] @ [{iPlcmnt.Stock.dLength}, {iPlcmnt.Stock.dWidth}]");
            }
        }
        public Bitmap Draw(BoardList stock)
        {
            double xOffset = 0;
            double imageHeight = 0;
            double boardSpacing = 70;
            double xMargin = 50;
            double yMargin = 50;
            double imageWidth = 2 * xMargin - boardSpacing;
            Font font = new Font(new FontFamily("Consolas"), 20.0f);

            foreach (var iBoard in stock)
            {
                if (iBoard.Length > imageHeight) imageHeight = iBoard.Length;
                imageWidth += iBoard.Width + boardSpacing;
            }
            imageHeight += 2 * yMargin;

            Bitmap bitmap = new Bitmap((int)imageWidth, (int)imageHeight);
            Graphics g = Graphics.FromImage(bitmap);

            g.FillRectangle(Brushes.Black, 0, 0, (int)imageWidth, (int)imageHeight);
            xOffset = xMargin;
            foreach (var iBoard in stock)
            {
                g.FillRectangle(Brushes.DarkRed, (float)(xOffset), (float)yMargin, (float)iBoard.Width, (float)iBoard.Length);
                string boardheader = $"{iBoard.Name} [{iBoard.Length}x{iBoard.Width}]";
                SizeF textSizeBoard = g.MeasureString(boardheader, font);
                g.DrawString(boardheader, font, Brushes.White, (float)(xOffset + iBoard.Width/2 -textSizeBoard.Width/2), (float)(yMargin/2 - textSizeBoard.Height/2));
                foreach (var iPlacement in this.Where(t => t.Stock.Name.StartsWith(iBoard.Name)))
                {
                    g.FillRectangle(Brushes.Green, (float)(xOffset + iPlacement.Stock.dWidth), (float)(iPlacement.Stock.dLength + yMargin), (float)iPlacement.Part.Width, (float)iPlacement.Part.Length);
                    g.TranslateTransform((float)(xOffset + iPlacement.Stock.dWidth + iPlacement.Part.Width / 2), (float)(iPlacement.Stock.dLength + iPlacement.Part.Length / 2 + yMargin));
                    g.RotateTransform(-90);
                    string text = $"{iPlacement.Part.Name} [{iPlacement.Part.Length}x{iPlacement.Part.Width}]";
                    SizeF textSize = g.MeasureString(text, font);
                    g.DrawString(text, font, Brushes.White, -(textSize.Width / 2), -(textSize.Height / 2));
                    g.RotateTransform(90);
                    g.TranslateTransform(-((float)xOffset + (float)(iPlacement.Stock.dWidth + iPlacement.Part.Width / 2)), -((float)(iPlacement.Stock.dLength + iPlacement.Part.Length / 2 + yMargin)));
                }
                xOffset += iBoard.Width + boardSpacing;
            }

            g.Flush();
            return bitmap;
        }

        public static int CompareByWasteAscending(Solution w1, Solution w2)
        {
            return (int)(w1.Waste - w2.Waste);
        }
    }

    public class Placement
    {
        public Placement(Part part, Item stock)
        {
            Part = part;
            Stock = stock;

        }
        public Part Part { get; set; }
        public Item Stock { get; set; }
    }


}