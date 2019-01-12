﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WoodFitting2
{

    /// <summary>
    /// Algorithm:
    /// This packer uses a collection of points as a means to place the parts
    /// Every board is packed using all the available parts, the best packed board and the parts used is then removed from the usable lists and the process is repeated using the new subset of boards and parts.
    /// To pack a board, a collection of points is generated, each with an X;Y coordinate and a disabled flag indicating if a part can be placed on the point.
    /// Initially the collection contains only the diagonal corners of the board (0,0) and (length,width)
    /// The (length,width) coord is disabled.
    /// We repeatedly loop through all the enabled points
    ///     check what is the area available at that point
    ///     place the largest part that would fit the area at the point
    ///     and insert two new points at the top right and bottom left corners of the part
    ///     the point at which the part is placed is disabled.
    /// Some other special scenarios is also coverred
    /// When all the points are disabled, the board is complete
    /// 
    /// </summary>

    class Packer2
    {
        private class PointD
        {
            public double dWidth;
            public double dLength;
            public bool disabled;

            public PointD(double dwidth, double dlength)
            { this.dWidth = dwidth; this.dLength = dlength; }

            public override string ToString()
            {
                return $"{(disabled ? "!" : "")}{dLength},{dWidth}";
            }
        }

        private static Bitmap Drawboard_debug(Board board, PointD[] points, int pointcount, Part[] parts, double[] dLengths, double[] dWidths, int partcount, double partsArea, RectangleF lastarea)
        {
            double xMargin = 50;
            double yMargin = 50;

            double imageHeight = board.Width + 2 * yMargin;
            double imageWidth = board.Length + 2 * xMargin;

            // create bitmap
            Bitmap bitmap = new Bitmap((int)imageWidth, (int)imageHeight);
            Graphics g = Graphics.FromImage(bitmap);
            // draw the board
            g.DrawRectangle(Pens.Black, (float)xMargin, (float)yMargin, (float)board.Length, (float)board.Width);



            // draw the parts placed
            for (int i = 0; i < partcount; i++)
            {
                Part iPlacement = parts[i];
                double dLength = dLengths[i];
                double dWidth = dWidths[i];

                // draw the part
                g.FillRectangle(Brushes.Green, (float)(xMargin + dLength), (float)(yMargin + dWidth), (float)iPlacement.Length, (float)iPlacement.Width);

                // print the part text
                string partLabel = $"{iPlacement.ID}";
                Font partFont = new Font(new FontFamily("Microsoft Sans Serif"), 10);
                g.DrawString(partLabel, partFont, Brushes.Black, (float)(xMargin + dLength), (float)(yMargin + dWidth));
            }

            // draw the last area where a part was placed
            if (lastarea != RectangleF.Empty)
            {
                lastarea.Offset((float)xMargin, (float)yMargin);
                g.FillRectangle(new SolidBrush(Color.FromArgb(140, Color.Yellow)), Rectangle.Round(lastarea));
            }

            //draw the placement points
            for (int i = 0; i < pointcount; i++)
            {
                PointD iPoint = points[i];
                if (iPoint.disabled)
                    g.FillEllipse(new SolidBrush(Color.FromArgb(220, Color.Black)), (float)(xMargin + iPoint.dLength - 10), (float)(yMargin + iPoint.dWidth - 10), 20, 20);
                else
                    g.FillEllipse(new SolidBrush(Color.FromArgb(220, Color.Red)), (float)(xMargin + iPoint.dLength - 10), (float)(yMargin + iPoint.dWidth - 10), 20, 20);
            }

            Font aFont = new Font(new FontFamily("Microsoft Sans Serif"), 10);
            g.DrawString((partsArea / board.Area * 100).ToString("0.0") + "%", aFont, Brushes.Black, (float)(xMargin), (float)(bitmap.Height - yMargin + 15));


            g.Flush();
            return bitmap;
        }

        /// <summary>
        ///  do the internal preperation to pack a set of parts onto a set of boards with a collection of options
        /// </summary>
        /// <param name="parts"></param>
        /// <param name="boards"></param>
        /// <param name="sawkerf"></param>
        /// <param name="boardMarginLength"></param>
        /// <param name="boardMarginWidth"></param>
        /// <param name="partLengthPadding"></param>
        /// <param name="partWidthPadding"></param>
        /// <returns></returns>
        static public void Pack(Part[] parts, Board[] boards, double sawkerf = 3.2, double partLengthPadding = 0, double partWidthPadding = 0)
        {
            // order the parts and boards by Area, Ascending
            int partsCount = parts.Length;
            Part[] orderredParts = parts.OrderBy(t => t.Area).ToArray();
            int boardsCount = boards.Length;

            // keep count of the parts and boards used
            int packedPartsCount = 0;
            int packedBoardsCount = 0;

            while (packedPartsCount < partsCount && packedBoardsCount < boardsCount)
            {
                List<Task> threads = new List<Task>();
                for (int i = 0; i < boardsCount; i++)
                    if (!boards[i].isComplete)
                        threads.Add(
                            Task.Factory.StartNew((o) =>
                                {
                                    // reference board[i]
                                    Board iBoard = boards[(int)o];

                                    // clear the solution for the board
                                    iBoard.PackedParts = new Part[partsCount];
                                    iBoard.PackedPartdLengths = new double[partsCount];
                                    iBoard.PackedPartdWidths = new double[partsCount];
                                    iBoard.PackedPartsCount = 0;
                                    iBoard.PackedPartsTotalArea = 0;
                                    iBoard.isComplete = false;

                                    // create the two original points for the board
                                    PointD[] points = new PointD[partsCount * 2 + 2];
                                    points[0] = new PointD(0, 0);
                                    points[1] = new PointD(iBoard.Width + sawkerf, iBoard.Length + sawkerf) { disabled = true };
                                    int pointCount = 2;
                                    int iPointIndex = -1;

                                    // continuously iterate throught the points until we reach the end of the list of points (restart from first point if a part is placed)
                                    while (++iPointIndex < pointCount)
                                    {
                                        PointD iPoint = points[iPointIndex];
                                        if (iPoint.disabled) continue; // ignore disabled points

                                        #region // determine hight and width of available area at the point...
                                        IEnumerable<PointD> limitinpoints = points.Where(q => q?.dLength > iPoint.dLength && q?.dWidth >= iPoint.dWidth);
                                        PointD limitingPoint = limitinpoints.OrderBy(so => so.dWidth).First();
                                        double maxx = Math.Min(limitingPoint.dWidth, iBoard.Width);
                                        double maxy = iBoard.Length;

                                        double maxWidth = maxx - iPoint.dWidth - sawkerf;
                                        double maxLength = maxy - iPoint.dLength - sawkerf;
                                        if (maxWidth <= 0 || maxLength <= 0)
                                        {
                                            iPoint.disabled = true;
                                            continue;
                                        }
                                        #endregion

                                        #region // search for a part that will fit on the area on the board ...
                                        // test each part for fit on the area for the point
                                        bool partplaced = false;
                                        for (int iPartIndex = partsCount - 1; iPartIndex >= 0; iPartIndex--)
                                        {
                                            Part iPart = orderredParts[iPartIndex];
                                            // ignore parts already packed
                                            if (iPart.isPacked || iBoard.PackedParts.Contains(iPart)) continue;

                                            // if the part will fit
                                            if (iPart.Length <= maxLength && iPart.Width <= maxWidth)
                                            {
                                                //Trace.WriteLine($"   placing part {iPart} on point {iPoint}");

                                                #region // place the part onto the board at the point ...
                                                iBoard.PackedPartdLengths[iBoard.PackedPartsCount] = iPoint.dLength;
                                                iBoard.PackedPartdWidths[iBoard.PackedPartsCount] = iPoint.dWidth;
                                                iBoard.PackedParts[iBoard.PackedPartsCount++] = iPart;
                                                iBoard.PackedPartsTotalArea += iPart.Area;
                                                #endregion

                                                #region // create new points for the top-right and bottom left corners of the part ...
                                                PointD newBL = new PointD(iPoint.dWidth, iPoint.dLength + iPart.Length + sawkerf);
                                                PointD newTR = new PointD(iPoint.dWidth + iPart.Width + sawkerf, iPoint.dLength);
                                                // discard the new points if 
                                                //  * there are already points for this location, or
                                                //  * it is outside the board...this may happen if the part stops closer to the edge than thesawkerf
                                                //if (newBL.dLength > iBoard.Length || points.FirstOrDefault(t => t?.dWidth == newBL.dWidth && t?.dLength == newBL.dLength) != null)
                                                //    newBL.disabled = true;
                                                //if (newTR.dWidth > iBoard.Width || points.FirstOrDefault(t => t?.dWidth == newTR.dWidth && t?.dLength == newTR.dLength) != null)
                                                //    newTR.disabled = true;
                                                #endregion

                                                #region // disable the existing points coverred by the new part
                                                //disable all the points that are on the placed part
                                                for (int j = 0; j < pointCount; j++)
                                                {
                                                    PointD jPoint = points[j];
                                                    if (jPoint.dWidth >= iPoint.dWidth && jPoint.dWidth <= iPoint.dWidth + iPart.Width && jPoint.dLength >= iPoint.dLength) jPoint.disabled = true;
                                                }
                                                #endregion

                                                #region // insert new points into orderred array ...
                                                int di = pointCount - 1;
                                                if (newBL != null)
                                                {
                                                    while (points[di].dLength > newBL.dLength || points[di].dLength == newBL.dLength && points[di].dWidth > newBL.dWidth)
                                                        points[1 + di] = points[di--];
                                                    points[1 + di] = newBL;
                                                    pointCount++;
                                                }

                                                if (newTR != null)
                                                {
                                                    di = pointCount - 1;
                                                    while (points[di].dLength > newTR.dLength || points[di].dLength == newTR.dLength && points[di].dWidth > newTR.dWidth)
                                                        points[1 + di] = points[di--];
                                                    points[1 + di] = newTR;
                                                    pointCount++;
                                                }
                                                #endregion

                                                partplaced = true;
                                                break;
                                            }
                                        } 
                                        #endregion

                                        // if no parts fit this point's area
                                        if (!partplaced)
                                        {
                                            // disable this point
                                            iPoint.disabled = true;

                                            // if this part's area was not limited by the board's edge
                                            if (limitingPoint.dLength < iBoard.Length)
                                            {
                                                // create a new point at the same dLength value as the one that limited the width - maybe the extra width will allow a part to be placed there
                                                PointD newPoint = new PointD(iPoint.dWidth, limitingPoint.dLength);
                                                // insert the new point into the orderred array
                                                int di = pointCount - 1;
                                                while (points[di].dLength > newPoint.dLength || points[di].dLength == newPoint.dLength && points[di].dWidth > newPoint.dWidth)
                                                    points[1 + di] = points[di--];

                                                points[1 + di] = newPoint;
                                                pointCount++;
                                            }
                                        }
                                        else
                                            // we placed a part - traverse all the points again
                                            iPointIndex = -1;
                                    }
                                }, i));
                Task.WaitAll(threads.ToArray());

                // Find the best packed board from this iteration
                //IEnumerable<Board> incompleteBoards = boards.Where(q => !q.isComplete);
                Board iBestCoverredBoard =
                    boards.Where(q => !q.isComplete && q.PackedPartsCount > 0)
                    .OrderByDescending(t => t.PackedPartsTotalArea / t.Area)
                    .First();


                // set the complete flag for the board with the best coverage
                iBestCoverredBoard.isComplete = true;
                packedBoardsCount++;

                //Compact the packed parts array of the board
                Array.Resize<Part>(ref iBestCoverredBoard.PackedParts, iBestCoverredBoard.PackedPartsCount);

                // set the packed flag for the packed parts
                iBestCoverredBoard.PackedParts.ToList().ForEach(t => t.isPacked = true);
                packedPartsCount += iBestCoverredBoard.PackedPartsCount;

                // clear the packing for all the unsuccessfull boards
                for (int iPacked = 0; iPacked < boardsCount; iPacked++)
                {
                    Board iBoard = boards[iPacked];
                    // if non of te parts packed on the board have been packed on a previous board
                    if (!iBoard.isComplete)
                    {
                        iBoard.PackedParts = new Part[parts.Length];
                        iBoard.PackedPartdLengths = new double[parts.Length];
                        iBoard.PackedPartdWidths = new double[parts.Length];
                        iBoard.PackedPartsCount = 0;
                        iBoard.PackedPartsTotalArea = 0;
                        iBoard.isComplete = false;
                    }
                }



                Trace.WriteLine($"---------------------------------------------");
                Trace.WriteLine($"best board for iteration:");
                Trace.WriteLine($"{iBestCoverredBoard} ({iBestCoverredBoard.PackedPartsTotalArea / iBestCoverredBoard.Area * 100:0.0}%)");
                for (int j = 0; j < iBestCoverredBoard.PackedPartsCount; j++)
                    Trace.WriteLine($"   {iBestCoverredBoard.PackedParts[j]} @ ({iBestCoverredBoard.PackedPartdLengths[j]}, {iBestCoverredBoard.PackedPartdWidths[j]})");
                Trace.WriteLine($"=============================================");
            }

        }


    }
}
