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
                boardsToDraw = boards.Where(t => t.PackedParts != null).ToList();

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
                for (int i = 0; i < iBoard.PartsCount; i++)
                {
                    Part iPlacement = iBoard.PackedParts[i];
                    double dLength = iBoard.PartdLengths[i];
                    double dWidth = iBoard.PartdWidths[i];

                    // draw the part
                    g.FillRectangle(Brushes.Green, (float)(xOffset + dWidth), (float)(dLength + yMargin), (float)iPlacement.Width, (float)iPlacement.Length);

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
            bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
            return bitmap;
        }

        static void Main(string[] args)
        {
            #region // Gather the inputs to the solution ...
            double PartPadding_Length = 0;
            double PartPadding_Width = 0;
            double SawKerf = 3.2;
            Board[] boards = new Board[] {
                new Board("A", 2100, 193),
                new Board("B", 2100, 150),
                new Board("C", 2100, 143),
                new Board("D", 2100, 170),
                new Board("E", 2100, 185),
                new Board("F", 2100, 210),
                new Board("G", 2100, 135),
                new Board("H", 2100, 225),
            };
            Part[] parts = new Part[] {
                new Part("001", 1721.7, 100.0),
                new Part("002", 284.5, 100.0),
                new Part("003", 1721.7, 100.0),
                new Part("004", 284.5, 100.0),
                new Part("005", 955.0, 69.3),
                new Part("006", 955.0, 60.0),
                new Part("007", 955.0, 69.6),
                new Part("008", 955.0, 80.0),
                new Part("009", 955.0, 60.0),
                new Part("010", 955.0, 60.0),
                new Part("011", 310.0, 100.0),
                new Part("012", 310.0, 100.0),
                new Part("013", 310.0, 36.0),
                new Part("014", 310.0, 36.0),
                new Part("015", 354.5, 36.0),
                new Part("016", 354.5, 36.0),
                new Part("017", 299.0, 20.0),
                new Part("018", 299.0, 20.0),
                new Part("019", 299.0, 20.0),
                new Part("020", 299.0, 20.0),
                new Part("021", 327.5, 20.0),
                new Part("022", 327.5, 20.0),
                new Part("023", 955.0, 80.0),
                new Part("024", 310.0, 100.0),
                new Part("025", 310.0, 100.0),
                new Part("026", 310.0, 36.0),
                new Part("027", 310.0, 36.0),
                new Part("028", 354.5, 36.0),
                new Part("029", 354.5, 36.0),
                new Part("030", 299.0, 20.0),
                new Part("031", 327.5, 20.0),
                new Part("032", 327.5, 20.0),
                new Part("033", 955.0, 80.0),
                new Part("034", 310.0, 100.0),
                new Part("035", 310.0, 100.0),
                new Part("036", 310.0, 36.0),
                new Part("037", 310.0, 36.0),
                new Part("038", 354.5, 36.0),
                new Part("039", 354.5, 36.0),
                new Part("040", 299.0, 20.0),
                new Part("041", 327.5, 20.0),
                new Part("042", 327.5, 20.0),
                new Part("043", 955.0, 80.0),
                new Part("044", 310.0, 100.0),
                new Part("045", 310.0, 100.0),
                new Part("046", 310.0, 36.0),
                new Part("047", 310.0, 36.0),
                new Part("048", 354.5, 36.0),
                new Part("049", 354.5, 36.0),
                new Part("050", 299.0, 20.0),
            };

            if (args[0].StartsWith("-clp:"))
            {
                string path = args[0].Replace("-clp:", "");
                if (System.IO.File.Exists(path))
                    Import.FromCutlistPlusCSV(path, out parts, out boards);
            }
            if (args[0].StartsWith("-csv:"))
            {
                string path = args[0].Replace("-csv:", "");
                if (System.IO.File.Exists(path))
                    Import.FromCSV(path, out parts, out boards);
            }
            #endregion

            #region // Print starting parameters ...
            Trace.WriteLine($"Packing started @ {DateTime.Now} with the following:");
            Trace.WriteLine($"-----------------------------------------------------");
            Trace.WriteLine($"  Part padding  (w x l) : {PartPadding_Width} mm x {PartPadding_Length} mm");
            Trace.WriteLine($"  Saw blade kerf        : {SawKerf} mm");
            Trace.WriteLine($"  {boards.Length} Boards:");
            for (int i = 0; i < boards.Length; i++)
                Trace.WriteLine($"{boards[i].ID,6} [{boards[i].Length,7:0.0} x {boards[i].Width,5:0.0}]");
            Trace.WriteLine($"  {parts.Length} Parts:");
            for (int i = 0; i < parts.Length; i++)
                Trace.WriteLine($"{parts[i].ID,6} [{parts[i].Length,7:0.0} x {parts[i].Width,5:0.0}]");
            #endregion

            #region // Find the solution ...
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Packer.Pack(parts, boards,
                sawkerf: SawKerf,
                partPaddingLength: PartPadding_Length,
                partPaddingWidth: PartPadding_Width);
            sw.Stop();
            #endregion

            #region // Print the solution ...
            Trace.WriteLine("Solution:");
            Trace.WriteLine("----------------");
            if (parts.Any(t=>!t.Packed))
                Trace.WriteLine("WARNING: All parts could not be placed!\r\n");

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

                if (iBoard.PackedParts == null)
                    Trace.WriteLine($"   Board {iBoard.ID} [{iBoard.Length,6:0.0} x {iBoard.Width,5:0.0}] : not used.");
                else
                {
                    UsedStockArea += iBoard.Area;
                    UsedStockCount++;
                    Trace.WriteLine($"   Board {iBoard.ID} [{iBoard.Length,6:0.0} x {iBoard.Width,5:0.0}] ({(iBoard.PackedParts == null ? 0 : iBoard.PackedArea / iBoard.Area):00.0 %}) :");
                    UsedPartsArea += iBoard.PackedArea;
                    TotalPackedPartsCount += iBoard.PartsCount;
                    for (int j = 0; j < iBoard.PartsCount; j++)
                        Trace.WriteLine($"{iBoard.PackedParts?[j].ID,10} [{iBoard.PackedParts?[j].Length,7:0.0} x {iBoard.PackedParts?[j].Width,5:0.0}] @ ({iBoard.PartdLengths[j],7:0.0},{iBoard.PartdWidths[j],7:0.0})");
                }
            }

            double TotalPartsArea = 0;
            for (int i = 0; i < parts.Length; i++)
                TotalPartsArea += parts[i].Area;
            
            Trace.WriteLine("===========================================================");
            Trace.WriteLine("Solution summary");
            Trace.WriteLine("----------------");
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
