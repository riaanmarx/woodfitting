﻿using System;
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

        public static Bitmap Draw(BoardList boards, PartList parts, bool usedstockonly = true)
        {
            double xOffset = 0;
            double imageHeight = 0;
            double boardSpacing = 70;
            double xMargin = 50;
            double yMargin = 50;
            double imageWidth = 2 * xMargin - boardSpacing;
            Font boardFont = new Font(new FontFamily("Consolas"), 15.0f);

            // create list of boards to draw
            List<BoardNode> boardsToDraw = new List<BoardNode>(boards.ToArray);
            if (usedstockonly)
            {
                List<string> usedboardnames = parts.ToArray.Select(t => t.Container).Distinct().ToList();
                boardsToDraw.RemoveAll(t => !usedboardnames.Contains(t.ID));
            }

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
                for (PartNode iPlacement = parts.Head; iPlacement != null; iPlacement = iPlacement.Next)
                {
                    // continue with next part if this part was placed on another board
                    if (iPlacement.Container != iBoard.ID) continue;

                    // draw the part
                    g.FillRectangle(Brushes.Green, (float)(xOffset + iPlacement.dWidth), (float)(iPlacement.dLength + yMargin), (float)iPlacement.Width, (float)iPlacement.Length);

                    // print the part text
                    string text1 = $"{iPlacement.ID} [{iPlacement.Length} x {iPlacement.Width}]";
                    string text2a = $"{iPlacement.ID}";
                    string text2b = $"[{iPlacement.Length} x {iPlacement.Width}]";
                    g.TranslateTransform((float)(xOffset + iPlacement.dWidth + iPlacement.Width / 2), (float)(iPlacement.dLength + iPlacement.Length / 2 + yMargin));
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
                    g.TranslateTransform(-((float)xOffset + (float)(iPlacement.dWidth + iPlacement.Width / 2)), -((float)(iPlacement.dLength + iPlacement.Length / 2 + yMargin)));
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
            double boardMargins_length = 25;
            double boardMargins_Width = 5;
            double PartPadding_Length = 0;
            double PartPadding_Width = 1;
            double SawKerf = 3.2;


            BoardList boards = new BoardList(
                new BoardNode("A", 2100, 193),
                new BoardNode("B", 2100, 150),
                new BoardNode("C", 2100, 143),
                new BoardNode("D", 2100, 170),
                new BoardNode("E", 2100, 185),
                new BoardNode("F", 2100, 210),
                new BoardNode("G", 2100, 135),
                new BoardNode("H", 2100, 225)
            );

            PartList parts = new PartList(
                new PartNode("001", 1721.7, 100.0),
                new PartNode("002", 284.5, 100.0),
                new PartNode("003", 1721.7, 100.0),
                new PartNode("004", 284.5, 100.0),
                new PartNode("005", 955.0, 69.3),
                new PartNode("006", 955.0, 60.0),
                new PartNode("007", 955.0, 69.6),
                new PartNode("008", 955.0, 80.0),
                new PartNode("009", 955.0, 60.0),
                new PartNode("010", 955.0, 60.0),
                new PartNode("011", 310.0, 100.0),
                new PartNode("012", 310.0, 100.0),
                new PartNode("013", 310.0, 36.0),
                new PartNode("014", 310.0, 36.0),
                new PartNode("015", 354.5, 36.0),
                new PartNode("016", 354.5, 36.0),
                //new PartNode("017", 299.0, 20.0),
                new PartNode("018", 299.0, 20.0),
                new PartNode("019", 299.0, 20.0),
                new PartNode("020", 299.0, 20.0)//,
                //new PartNode("021", 327.5, 20.0),
                //new PartNode("022", 327.5, 20.0),
                //new PartNode("023", 955.0, 80.0),
                //new PartNode("024", 310.0, 100.0),
                //new PartNode("025", 310.0, 100.0),
                //new PartNode("026", 310.0, 36.0),
                //new PartNode("027", 310.0, 36.0),
                //new PartNode("028", 354.5, 36.0),
                //new PartNode("029", 354.5, 36.0),
                //new PartNode("030", 299.0, 20.0),
                //new PartNode("031", 327.5, 20.0),
                //new PartNode("032", 327.5, 20.0),
                //new PartNode("033", 955.0, 80.0),
                //new PartNode("034", 310.0, 100.0),
                //new PartNode("035", 310.0, 100.0),
                //new PartNode("036", 310.0, 36.0),
                //new PartNode("037", 310.0, 36.0),
                //new PartNode("038", 354.5, 36.0),
                //new PartNode("039", 354.5, 36.0),
                //new PartNode("040", 299.0, 20.0),
                //new PartNode("041", 327.5, 20.0),
                //new PartNode("042", 327.5, 20.0),
                //new PartNode("043", 955.0, 80.0),
                //new PartNode("044", 310.0, 100.0),
                //new PartNode("045", 310.0, 100.0),
                //new PartNode("046", 310.0, 36.0),
                //new PartNode("047", 310.0, 36.0),
                //new PartNode("048", 354.5, 36.0),
                //new PartNode("049", 354.5, 36.0),
                //new PartNode("050", 299.0, 20.0)
            );
            //#if !DEBUG
            //            if (args[0].StartsWith("-l:"))
            //            {
            //                string path = args[0].Replace("-l:", "");
            //                if (System.IO.File.Exists(path))
            //                {
            //                    Import.FromCutlistPlusCSV(path, out List<Item> lstParts, out List<Item> lstBoards);
            //                    parts = lstParts.Where(t => "009,007,019,022".Split(',').Contains(t.Name)).ToArray();
            //                    //parts = lstParts.ToArray();
            //                    boards = lstBoards.Where(t => new string[] { "E" }.Contains(t.Name)).ToArray();
            //                    //boards = lstBoards.ToArray();
            //                }
            //            }
            //#endif 
            #endregion

            #region // Print starting parameters ...
            Trace.WriteLine($"Packing started @ {DateTime.Now} with the following:");
            Trace.WriteLine($"------------------------------------------------");
            Trace.WriteLine($"  Board margins (w x l) : {boardMargins_Width} mm x {boardMargins_length} mm");
            Trace.WriteLine($"  Part padding  (w x l) : {PartPadding_Width} mm x {PartPadding_Length} mm");
            Trace.WriteLine($"  Saw blade kerf        : {SawKerf} mm");
            Trace.WriteLine($"  {boards.Count} Boards:");
            for (BoardNode iBoard = boards.Head; iBoard != null; iBoard = iBoard.Next)
                Trace.WriteLine($"    {iBoard.ID} [{iBoard.Length,7:0.0} x {iBoard.Width,5:0.0}]");
            Trace.WriteLine($"  {parts.Count} Parts:");
            for (PartNode iPart = parts.Head; iPart != null; iPart = iPart.Next)
                Trace.WriteLine($"    {iPart.ID} [{iPart.Length,7:0.0} x {iPart.Width,5:0.0}]");
            #endregion

            #region // Find the solution ...
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var solution = Packer.Pack(parts, boards,
                sawkerf: SawKerf,
                boardMarginLength: boardMargins_length,
                boardMarginWidth: boardMargins_Width,
                partPaddingLength: PartPadding_Length,
                partPaddingWidth: PartPadding_Width);
            sw.Stop();
            #endregion

            #region // Print the solution ...
            // calculate the total area of all used boards
            List<string> usedBoardIDs = solution.ToArray.Select(t => t.Container).Distinct().ToList();
            double UsedStockArea = boards.ToArray.Where(q => usedBoardIDs.Contains(q.ID)).Sum(t => t.Area);

            Trace.WriteLine("Solution:");
            Trace.WriteLine("----------------");
            for (var iBoard = boards.Head; iBoard != null; iBoard = iBoard.Next)
            {
                Trace.WriteLine($"   Board {iBoard.ID} [{iBoard.Length,6:0.0} x {iBoard.Width,5:0.0}] :");
                for (PartNode iPlacement = solution.Head; iPlacement != null; iPlacement = iPlacement.Next)
                    if (iPlacement.Container == iBoard.ID)
                        Trace.WriteLine($"     {iPlacement.ID} [{iPlacement.Length,6:0.0} x {iPlacement.Width,5:0.0}] @ ({iPlacement.dLength,6:0.0}, {iPlacement.dWidth,5:0.0})");
            }
            Trace.WriteLine("===========================================================");
            Trace.WriteLine("Solution summary");
            Trace.WriteLine("----------------");
            Trace.WriteLine($"   Processing time: {sw.ElapsedMilliseconds,9:0} ms");
            Trace.WriteLine($"   #Boards        : {boards.Count,9:0}");
            Trace.WriteLine($"   #Parts         : {parts.Count,9:0}");
            Trace.WriteLine($"   Total Stock    : {boards.TotalArea / 1000000,9:0.000} m\u00b2");
            Trace.WriteLine($"   Used Stock     : {UsedStockArea / 1000000,9:0.000} m\u00b2");
            Trace.WriteLine($"   Total Parts    : {parts.TotalArea / 1000000,9:0.000} m\u00b2");
            Trace.WriteLine($"   Parts placed   : {solution.TotalArea / 1000000,9:0.000} m\u00b2");
            Trace.WriteLine($"   Waste          : {(UsedStockArea - solution.TotalArea) / 1000000,9:0.000} m\u00b2 ({(UsedStockArea - solution.TotalArea) / UsedStockArea:0.0 %})");
            Trace.WriteLine($"   Coverage       : {solution.TotalArea / UsedStockArea,11:0.0 %}");
            #endregion

            #region // Draw solution to an image ...
            Bitmap bmp = Draw(boards, solution);
            bmp.Save("out.bmp");
            Console.WriteLine("Launch output image (Y/N):");
            string s = Console.ReadLine();
            if (s.ToLower() == "y")
                Process.Start("out.bmp");
            #endregion
        }
    }

}