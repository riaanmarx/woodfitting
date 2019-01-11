using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WoodFitting2
{
    class Packer2
    {

        class PointD
        {
            public double X;
            public double Y;
            public bool disabled;

            public PointD(double X, double Y)
            { this.X = X; this.Y = Y; }

            public override string ToString()
            {
                return $"{(disabled ? "!" : "")}{X},{Y}";
            }
        }

        //points...
        //Algorithm
        //start with 2 points top left and bottom right of board
        // loop through each available point, but the last (which is the bottom right corner of the board)
        //      find the biggest area avail for that point
        //          find the first point to the right, higher than this one
        //      find the largest part that will fit the rectangle
        //      place the part at the point
        //      remove point from the pool
        //      add 2 new points - top right, and bottom left of newly placed part, so that list of points are sorted from low Y to high Y
        // 
        static public void Pack(Part[] parts, Board[] boards, double sawkerf = 3.2, double partLengthPadding = 0, double partWidthPadding = 0)
        {

            Part[] orderredParts = parts.OrderBy(t => t.Area).ToArray();
            Board iBoard = boards[1];
            iBoard.PackedParts = new Part[parts.Length];
            iBoard.PackedPartdLengths = new double[parts.Length];
            iBoard.PackedPartdWidths = new double[parts.Length];
            PointD[] points = new PointD[parts.Length * 2 + 2];
            points[0] = new PointD(0, 0);
            points[1] = new PointD(iBoard.Width, iBoard.Length);
            points[1].disabled = true;
            int pointCount = 2;

            Part[] packedparts = new Part[parts.Length];

            int iterationpartscount = 0;
            int iPointIndex = -1;
            while (++iPointIndex < pointCount)
            {
                PointD iPoint = points[iPointIndex];
                if (iPoint.disabled) continue;

                #region // detect maximum hight/width part that will fit the point ...
                //if there is a point with larger y and x >= my x....there will always be one - the board limit
                //  maxx = x of other point
                double maxx = points.First(t => t.Y > iPoint.Y && t.X >= iPoint.X).X;

                //if there is a point with larger X and y >= my y
                //  maxy = y of other point
                double maxy = iBoard.Length;
                //double maxy = points.FirstOrDefault(t => t.X > iPoint.X && t.Y >= iPoint.Y).Y;


                double maxWidth = maxx - iPoint.X;
                double maxLength = maxy - iPoint.Y;
                if (maxWidth * maxLength == 0)
                {
                    iPoint.disabled = true;
                    continue;
                }
                #endregion


                Trace.WriteLine($" inspecting point {iPoint}, [{maxLength} x {maxWidth}]");

                for (int iPartIndex = parts.Length - 1; iPartIndex >= 0; iPartIndex--)
                {
                    Part iPart = orderredParts[iPartIndex];
                    if (iPart.isPacked) continue;

                    //Trace.WriteLine($"   testing part {iPart}");

                    if (iPart.Length <= maxLength && iPart.Width <= maxWidth)
                    {
                        //Trace.WriteLine($"   putting part {iPart} on point {iPoint}");

                        #region // place the part onto the point ...
                        // note that part is now in use
                        iPart.isPacked = true;

                        // add the part to the board's collection
                        iBoard.PackedPartdLengths[iBoard.PackedPartsCount] = iPoint.Y;
                        iBoard.PackedPartdWidths[iBoard.PackedPartsCount] = iPoint.X;
                        iBoard.PackedParts[iBoard.PackedPartsCount++] = iPart;
                        iBoard.PackedPartsTotalArea += iPart.Area;
                        #endregion

                        #region // create two new points for the top-right and bottom left corners of the part ...
                        // add two new points...one just after this one, the other we need to check where
                        PointD newBL = new PointD(iPoint.X, iPoint.Y + iPart.Length + sawkerf);
                        PointD newTR = new PointD(iPoint.X + iPart.Width + sawkerf, iPoint.Y);
                        #endregion

                        //Trace.WriteLine($"    adding two new points: TR({newTR.X},{newTR.Y}), BL({newBL.X},{newBL.Y})");

                        #region // disable the points no longer available due to the placement ...
                        // disable the point where the part was placed
                        iPoint.disabled = true;

                        // disable the new points if there are already points for this location
                        if (points.FirstOrDefault(t => t?.X == newBL.X && t?.Y == newBL.Y) != null) newBL.disabled = true;
                        if (points.FirstOrDefault(t => t?.X == newTR.X && t?.Y == newTR.Y) != null) newTR.disabled = true;
                        // disable the point if it is outside the board...this may happen if the part stops closer to the edge than thesawkerf
                        if (newBL.Y > iBoard.Length) newBL.disabled = true;
                        if (newTR.X > iBoard.Width) newTR.disabled = true;

                        #endregion

                        #region // insert new points into orderred array ...
                        int di = pointCount - 1;
                        if (newBL != null)
                        {
                            while (points[di].Y > newBL.Y || points[di].Y == newBL.Y && points[di].X < newBL.Y)
                                points[1 + di] = points[di--];
                            points[1 + di] = newBL;
                            pointCount++;
                        }

                        if (newTR != null)
                        {
                            di = pointCount - 1;
                            while (points[di].Y > newTR.Y || points[di].Y == newTR.Y && points[di].X < newTR.Y)
                                points[1 + di] = points[di--];
                            points[1 + di] = newTR;
                            pointCount++;
                        }
                        #endregion

                        iterationpartscount++;
                        break;
                    }
                }
                if (iterationpartscount > 0)
                {
                    iPointIndex = -1;
                    iterationpartscount = 0;
                }
                else
                    iPoint.disabled = true;
            }
        }
    }
}
