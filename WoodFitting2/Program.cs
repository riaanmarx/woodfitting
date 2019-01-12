using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WoodFitting2.Packer_v1;

namespace WoodFitting2
{
    class program
    {

        public static Bitmap Draw(Board[] boards, bool usedstockonly = true)
        {
            double xOffset = 0;
            double imageHeight = 0;
            double boardSpacing = 70;
            double xMargin = 50;
            double yMargin = 50;
            double imageWidth = 2 * xMargin - boardSpacing;
            Font boardFont = new Font(new FontFamily("Consolas"), 15.0f);

            // create list of boards to draw
            List<Board> boardsToDraw = new List<Board>(boards);
            if (usedstockonly)
                boardsToDraw = boards.Where(t =>t.PackedPartsCount>0).ToList();

            // calculate width & height required for the bitmap
            foreach (var iBoard in boardsToDraw)
            {
                if (iBoard.Length > imageHeight) imageHeight = iBoard.Length;
                imageWidth += iBoard.Width + boardSpacing;
            }
            imageHeight += 2 * yMargin;

            // create bitmap
            Bitmap bitmap = new Bitmap((int)imageWidth, (int)imageHeight);
            Graphics g = Graphics.FromImage(bitmap);

            // fill the background with black
            g.FillRectangle(Brushes.Black, 0, 0, (int)imageWidth, (int)imageHeight);

            // loop through all the boards to be drawn
            xOffset = xMargin;
            foreach (var iBoard in boardsToDraw)
            {
                // draw the board
                g.FillRectangle(Brushes.DarkRed, (float)(xOffset), (float)yMargin, (float)iBoard.Width, (float)iBoard.Length);
                string boardheader = $"{iBoard.ID} [{iBoard.Length}x{iBoard.Width}]";
                SizeF textSizeBoard = g.MeasureString(boardheader, boardFont);
                g.DrawString(boardheader, boardFont, Brushes.White, (float)(xOffset + iBoard.Width / 2 - textSizeBoard.Width / 2), (float)(yMargin / 2 - textSizeBoard.Height / 2));

                // loop through all the parts and draw the ones on the current board
                string overflowtext = "";
                //for (Part iPlacement = parts.Head; iPlacement != null; iPlacement = iPlacement.Next)
                for (int i = 0; i < iBoard.PackedPartsCount; i++)
                {
                    Part iPlacement = iBoard.PackedParts[i];
                    double dLength = iBoard.PackedPartdLengths[i];
                    double dWidth = iBoard.PackedPartdWidths[i];

                    // draw the part
                    g.FillRectangle(new SolidBrush(Color.FromArgb(255 ,Color.Green)), (float)(xOffset + dWidth), (float)(dLength + yMargin), (float)iPlacement.Width, (float)iPlacement.Length);

                    // print the part text
                    string text1 = $"{iPlacement.ID} [{iPlacement.Length} x {iPlacement.Width}]";
                    string text2a = $"{iPlacement.ID}";
                    string text2b = $"[{iPlacement.Length} x {iPlacement.Width}]";
                    g.TranslateTransform((float)(xOffset + dWidth + iPlacement.Width / 2), (float)(dLength + iPlacement.Length / 2 + yMargin));
                    g.RotateTransform(-90);

                    int sz = 16;
                    do
                    {
                        Font partFont = new Font(new FontFamily("Consolas"), --sz);
                        SizeF textSize = g.MeasureString(text1, partFont);
                        if (textSize.Width < iPlacement.Length && textSize.Height < iPlacement.Width)
                        {
                            g.DrawString(text1, partFont, Brushes.White, -(textSize.Width / 2), -(textSize.Height / 2));
                            break;
                        }
                        textSize = g.MeasureString(text2a, partFont);
                        SizeF textSize2 = g.MeasureString(text2b, partFont);
                        if (Math.Max(textSize.Width, textSize2.Width) < iPlacement.Length && textSize.Height + textSize2.Height < iPlacement.Width)
                        {
                            g.DrawString(text2a, partFont, Brushes.White, -(textSize.Width / 2), -textSize.Height);
                            g.DrawString(text2b, partFont, Brushes.White, -(textSize2.Width / 2), 0);
                            break;
                        }
                        if (textSize.Width < iPlacement.Length && textSize.Height < iPlacement.Width)
                        {
                            g.DrawString(text2a, partFont, Brushes.White, -(textSize.Width / 2), -(textSize.Height / 2));
                            overflowtext += text1 + ", ";
                            break;
                        }
                    } while (sz > 1);


                    g.RotateTransform(90);
                    g.TranslateTransform(-((float)xOffset + (float)(dWidth + iPlacement.Width / 2)), -((float)(dLength + iPlacement.Length / 2 + yMargin)));
                }

                g.TranslateTransform((float)(xOffset + iBoard.Width), (float)(iBoard.Length + yMargin));
                g.RotateTransform(-90);
                g.DrawString(overflowtext.TrimEnd(',', ' '), boardFont, Brushes.White, 0, 0);
                g.RotateTransform(90);
                g.TranslateTransform(-(float)(xOffset + iBoard.Width), -(float)(iBoard.Length + yMargin));

                xOffset += iBoard.Width + boardSpacing;
            }

            g.Flush();
           // bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
            return bitmap;
        }

        static void Main(string[] args)
        {
            #region // Gather the inputs to the solution ...
            double partLengthPadding = 0;
            double partWidthPadding = 0;
            double SawKerf = 3.2;

            Board[] boards = new Board[] { };
            Part[] parts = new Part[] { };
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-clp:"))
                {
                    string path = args[i].Replace("-clp:", "");
                    if (System.IO.File.Exists(path))
                        Import.FromCutlistPlusCSV(path, out parts, out boards);
                }
                if (args[i].StartsWith("-csv:"))
                {
                    string path = args[i].Replace("-csv:", "");
                    if (System.IO.File.Exists(path))
                        Import.FromCSV(path, out parts, out boards);
                }
                if (args[i].StartsWith("-kerf:"))
                {
                    string kerfarg = args[i].Replace("-kerf:", "");
                    SawKerf = double.Parse(kerfarg);
                }
                if (args[i].StartsWith("-pad:"))
                {
                    string[] padding = args[i].Replace("-pad:", "").Split(',');
                    partLengthPadding = double.Parse(padding[0]);
                    partWidthPadding = double.Parse(padding[1]);
                }
            }



            #endregion

            Array.Resize<Part>(ref parts, 25);
            //parts.First(t => t.ID == "002").Width = 999;
            //Array.Resize<Board>(ref boards, 1);

            #region // Print starting parameters ...
            Trace.WriteLine($"Packing started with the following:");
            Trace.WriteLine($"-----------------------------------");
            Trace.WriteLine($"  Part padding  (w x l) : {partWidthPadding} mm x {partLengthPadding} mm");
            Trace.WriteLine($"  Saw blade kerf        : {SawKerf} mm");
            Trace.WriteLine($"  {boards.Length} Boards:");
            for (int i = 0; i < boards.Length; i++)
                Trace.WriteLine($"{boards[i].ID,6} [{boards[i].Length,7:0.0} x {boards[i].Width,5:0.0}]");
            Trace.WriteLine($"  {parts.Length} Parts:");
            for (int i = 0; i < parts.Length; i++)
                Trace.WriteLine($"{parts[i].ID,6} [{parts[i].Length,7:0.0} x {parts[i].Width,5:0.0}]");
            Trace.WriteLine("===========================================================");
            #endregion

            #region // Find the solution ...
            Trace.WriteLine($"Processing ... \r\n");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Packer2.Pack(parts, boards,
                sawkerf: SawKerf,
                partLengthPadding: partLengthPadding,
                partWidthPadding: partWidthPadding);

            //Packer.Pack(parts, boards,
            //    sawkerf: SawKerf,
            //    partLengthPadding: partLengthPadding,
            //    partWidthPadding: partWidthPadding);

            sw.Stop();
            if (parts.Any(t => !t.isPacked))
                Trace.WriteLine("Processing completed with WARNING: All parts could not be placed!\r\n");
            else
                Trace.WriteLine($"Processing completed succesfully.\r\n");
            Trace.WriteLine("===========================================================");
            #endregion

            #region // Print the solution ...
            Trace.WriteLine("Solution detail:");
            Trace.WriteLine("----------------");

            // loop through all boards and print packed parts, while colating some totals
            double UsedStockArea = 0;
            int UsedStockCount = 0;
            double TotalStockArea = 0;
            int TotalPackedPartsCount = 0;
            double UsedPartsArea = 0;
            for (int i = 0; i < boards.Length; i++)
            {
                Board iBoard = boards[i];
                TotalStockArea += iBoard.Area;

                if (iBoard.PackedPartsCount == 0)
                    Trace.WriteLine($"   Board {iBoard.ID} [{iBoard.Length,6:0.0} x {iBoard.Width,5:0.0}] : not used.");
                else
                {
                    UsedStockArea += iBoard.Area;
                    UsedStockCount++;
                    Trace.WriteLine($"   Board {iBoard.ID} [{iBoard.Length,6:0.0} x {iBoard.Width,5:0.0}] ({(iBoard.PackedParts == null ? 0 : iBoard.PackedPartsTotalArea / iBoard.Area):00.0 %}) :");
                    UsedPartsArea += iBoard.PackedPartsTotalArea;
                    TotalPackedPartsCount += iBoard.PackedPartsCount;
                    for (int j = 0; j < iBoard.PackedPartsCount; j++)
                        Trace.WriteLine($"{iBoard.PackedParts?[j].ID,10} [{iBoard.PackedParts?[j].Length,7:0.0} x {iBoard.PackedParts?[j].Width,5:0.0}] @ ({iBoard.PackedPartdLengths[j],7:0.0},{iBoard.PackedPartdWidths[j],7:0.0})");
                }
            }

            double TotalPartsArea = 0;
            for (int i = 0; i < parts.Length; i++)
                TotalPartsArea += parts[i].Area;

            Trace.WriteLine("===========================================================");
            Trace.WriteLine("Solution summary:");
            Trace.WriteLine("----------------_");
            Trace.WriteLine($"   Processing time: {sw.ElapsedMilliseconds,5:0} ms");
            Trace.WriteLine($"   Boards         : {boards.Length,5:0}    ({TotalStockArea / 1000000,6:0.000} m\u00b2)");
            Trace.WriteLine($"   Used boards    : {UsedStockCount,5:0}    ({UsedStockArea / 1000000,6:0.000} m\u00b2)");
            Trace.WriteLine($"   Parts          : {parts.Length,5:0}    ({TotalPartsArea / 1000000,6:0.000} m\u00b2)");
            Trace.WriteLine($"   Placed parts   : {TotalPackedPartsCount,5:0}    ({UsedPartsArea / 1000000,6:0.000} m\u00b2)");
            Trace.WriteLine($"   Waste          : {(UsedStockArea - UsedPartsArea) / UsedStockArea,7:0.0 %}  ({(UsedStockArea - UsedPartsArea) / 1000000,6:0.000} m\u00b2)");
            Trace.WriteLine($"   Coverage       : {UsedPartsArea / UsedStockArea,7:0.0 %}  ({UsedPartsArea / 1000000,6:0.000} m\u00b2)");
            #endregion

            #region // Draw solution to an image ...
            Bitmap bmp = Draw(boards);
            bmp.Save("out.bmp");
            Console.WriteLine("Launch output image (Y/N):");
            string s = Console.ReadLine();

            if (s.ToLower() == "y")
            Process.Start("out.bmp");
            #endregion
        }
    }

}
