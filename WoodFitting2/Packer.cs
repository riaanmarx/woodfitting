using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WoodFitting2.Packer_v1
{
    /// <summary>
    /// Packs rectangles(parts) into other rectangles (boards)
    /// 
    /// Spin up a thread per board
    /// In each thread, pack the board
    /// keep the best packed board and discard the others
    /// repack the remaining boards with the remaining parts
    /// continue until all parts are packed or all boards used up
    /// 
    /// 
    /// Loop through required parts
    ///     if part would push used volume past board's, continue to next part
    ///     if part is the exact size of previous part, continue to next part
    ///     find the first (smallest) board section that will fit the part
    ///     if no board will fit part, continue to next part
    ///     if the board section used has a buddy section (from previous placement)
    ///         adjust buddy section for placement of this part to prevent overlapping parts
    ///     append part to the list of packed parts
    ///     if the current solution has less waste than the current best solution, this becomes the new best solution
    ///     if there are parts left to pack
    ///         Create 2 new overlapping board sections for the part of the board not coverred by the part and set each as the buddy of the other
    ///         Replace the board with the 2 new sub-boards in the list of available boards
    ///         Pack the remaining required parts into the remaining available boards
    ///         undo the replacement
    ///     remove the current part from the list of packed parts
    /// end-loop
    /// return the current best solution  
    /// 
    /// </summary>
    public class Packer
    {
        public static Bitmap Drawboard_debug(Board board, Board[] segments, int segcount, Part[] parts, double[] dLengths, double[] dWidths, int partcount, double partsArea)
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

            //draw the board segments
            for (int i = 0; i < segcount; i++)
            {
                Board iseg = segments[i];
                if (!iseg.isInUse)
                    g.FillRectangle(new SolidBrush(Color.FromArgb(30, Color.Red)), (float)(xMargin + iseg.dLength), (float)(yMargin + iseg.dWidth), (float)iseg.Length, (float)iseg.Width);
            }

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
            // draw the board
            //g.DrawRectangle(Pens.Black, (float)xMargin, (float)yMargin, (float)board.Length, (float)board.Width);
            Font aFont = new Font(new FontFamily("Microsoft Sans Serif"), 10);
            g.DrawString((partsArea / board.Area * 100).ToString("0.0") + "%", aFont, Brushes.Black, (float)(xMargin), (float)(bitmap.Height - yMargin));

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
            Board[] orderredBoards = boards.OrderBy(t => t.Area).ToArray();

            // add padding to all parts
            if (partLengthPadding > 0 || partWidthPadding > 0)
                orderredParts.ToList().ForEach(t => t.Inflate(partWidthPadding, partLengthPadding));

            // keep count of the parts and boards used
            int packedPartsCount = 0;
            int packedBoardsCount = 0;

            // repeat until all parts are placed, or all boards packed
            while (packedPartsCount < partsCount && packedBoardsCount < boardsCount)
            {
                Task[] threads = new Task[boardsCount];
                for (int i = 0; i < boardsCount; i++)
                {
                    threads[i] = Task.Factory.StartNew((o) =>
                    {
                        // reference board[i]
                        Board iBoard = orderredBoards[(int)o];
                        if (iBoard.isComplete) return;

                        // init a packer object
                        Packer_internal iPacker = new Packer_internal()
                        {
                            sawkerf = sawkerf,
                            Board = iBoard,
                            Parts = orderredParts,
                            PartsCount = partsCount,
                            BoardSections = new Board[2 * partsCount + 2],
                            BoardSectionsCount = 1,
                            CurrentSolution = new Part[partsCount],
                            CurrentSolutionDLengths = new double[partsCount],
                            CurrentSolutionDWidths = new double[partsCount]
                        };
                        iPacker.BoardSections[0] = new Board(iBoard.ID, iBoard.Length, iBoard.Width, iBoard.dLength, iBoard.dWidth);

                        // pack the board recursively, starting at the first part and an empty solution
                        iPacker.StartPacking(0);

                        //StringBuilder sb = new StringBuilder();
                        //sb.AppendLine($"board packed: {iBoard}");
                        //for (int j = 0; j < iBoard.PackedPartsCount; j++)
                        //    sb.AppendLine($"   {iBoard.PackedParts[j]} @ ({iBoard.PackedPartdLengths[j]}, {iBoard.PackedPartdWidths[j]})");
                        //Trace.WriteLine(sb.ToString());
                    }, i);
                }
                Task.WaitAll(threads);

                // Find the best packed board from this iteration
                IEnumerable<Board> incompleteBoards = orderredBoards.Where(q => !q.isComplete);
                Board[] PackedBestCoverredBoards = incompleteBoards.OrderByDescending(t => t.PackedPartsTotalArea / t.Area).ToArray();

                Trace.WriteLine($"---------------------------------------------");

                // If no board could be packed, exit
                if (PackedBestCoverredBoards.Length == 0)
                    break;

                // loop through boards
                for (int iPacked = 0; iPacked < PackedBestCoverredBoards.Length; iPacked++)
                {
                    Trace.WriteLine($"best board(s) for iteration:");
                    Board iBestCoverredBoard = PackedBestCoverredBoards[iPacked];
                    // if non of te parts packed on the board have been packed on a previous board
                    if (!iBestCoverredBoard.PackedParts.Any(t => t?.isPacked??false))
                    {
                        // use this packing
                        Trace.WriteLine($"{iBestCoverredBoard} ({iBestCoverredBoard.PackedPartsTotalArea/iBestCoverredBoard.Area*100:0.0}%)");
                        for (int j = 0; j < iBestCoverredBoard.PackedPartsCount; j++)
                            Trace.WriteLine($"   {iBestCoverredBoard.PackedParts[j]} @ ({iBestCoverredBoard.PackedPartdLengths[j]}, {iBestCoverredBoard.PackedPartdWidths[j]})");

                        // set the complete flag for the board with the best coverage
                        iBestCoverredBoard.isComplete = true;
                        packedBoardsCount++;

                        if (iBestCoverredBoard.PackedParts != null)
                        {
                            //Compact the packed parts array of the board
                            Array.Resize<Part>(ref iBestCoverredBoard.PackedParts, iBestCoverredBoard.PackedPartsCount);// = iBestCoverredBoard.PackedParts.Where(t => t != null).ToArray();

                            // set the packed flag for the packed parts
                            iBestCoverredBoard.PackedParts.ToList().ForEach(t => t.isPacked = true);
                        }
                        packedPartsCount += iBestCoverredBoard.PackedPartsCount;
                    }
                    else
                    {
                        // Clear the inferior packings
                        iBestCoverredBoard.PackedParts = null;
                        iBestCoverredBoard.PackedPartdLengths = null;
                        iBestCoverredBoard.PackedPartdWidths = null;
                        iBestCoverredBoard.PackedPartsCount = 0;
                        iBestCoverredBoard.PackedPartsTotalArea = 0;
                    }
                }

            }
        }

        private class Packer_internal
        {
            public Board Board;
            public Part[] Parts;
            public int PartsCount;

            public Board[] BoardSections;
            public int BoardSectionsCount;

            public Part[] CurrentSolution;
            public double[] CurrentSolutionDLengths;
            public double[] CurrentSolutionDWidths;
            public int CurrentSolutionPartCount;
            public double CurrentSolutionTotalArea;

            public double sawkerf;

            public void StartPacking(int iStart)
            {
                double lastPartLength = -1;
                double lastPartWidth = -1;


                // loop through the parts, from big to small
                for (int i = iStart; i < PartsCount; i++)
                {
                    Part iPart = Parts[i];

                    #region // check if the part is a viable candidate ...
                    // ignore parts already packed on a board in a previous iteration
                    if (iPart.isPacked) continue;
                    // ignore parts larger than the largest board section
                    if (iPart.Area > Board.Area) break;
                    // short-circuit repeat parts
                    if (iPart.Length == lastPartLength && iPart.Width == lastPartWidth) continue;
                    // ignore parts already temporarily packed on this board in a previous recursion
                    if (CurrentSolution.Contains(iPart)) continue;

                    lastPartLength = iPart.Length;
                    lastPartWidth = iPart.Width;
                    #endregion

                    #region // Find first board that will fit the part ...
                    // find first board that will accomodate the part
                    int j = 0;
                    while (j < BoardSectionsCount && BoardSections[j].Area < iPart.Area) j++;
                    while (j < BoardSectionsCount && (BoardSections[j].isInUse || iPart.Length > BoardSections[j].Length || iPart.Width > BoardSections[j].Width)) j++;

                    // if no boards will accomodate the part, continue to next part
                    if (j >= BoardSectionsCount) continue;
                    Board iBoardSection = BoardSections[j];
                    #endregion

                    #region // place the part in the current bin ...
                    CurrentSolutionDLengths[CurrentSolutionPartCount] = iBoardSection.dLength;
                    CurrentSolutionDWidths[CurrentSolutionPartCount] = iBoardSection.dWidth;
                    CurrentSolution[CurrentSolutionPartCount++] = iPart;
                    CurrentSolutionTotalArea += iPart.Area;
                    iBoardSection.isInUse = true;
                    #endregion

                    #region // store best solution ...
                    //if this is a better solution than the current best one ... replace the current best one
                    if (CurrentSolutionTotalArea > Board.PackedPartsTotalArea)
                    {
                        Board.PackedParts = CurrentSolution.Clone() as Part[];
                        Board.PackedPartdLengths = CurrentSolutionDLengths.Clone() as double[];
                        Board.PackedPartdWidths = CurrentSolutionDWidths.Clone() as double[];
                        Board.PackedPartsCount = CurrentSolutionPartCount;
                        Board.PackedPartsTotalArea = CurrentSolutionTotalArea;
                    }
                    #endregion

                    #region // Break association and adjust associated boards if a board is used that is associated with another to prevent overlapping placements ...
                    Board iAssocBoardSection = iBoardSection.AssociatedBoard;
                    double oAssocLength = 0,
                        oAssocWidth = 0,
                        oiBoardLength = 0,
                        oiBoardWidth = 0;
                    // if the board section used has a buddy from a previous placement
                    if (iAssocBoardSection != null)
                    {
                        // keep old sizes so we can revert them at the end of the iteration
                        oAssocLength = iAssocBoardSection.Length;
                        oAssocWidth = iAssocBoardSection.Width;
                        oiBoardLength = iBoardSection.Length;
                        oiBoardWidth = iBoardSection.Width;

                        // if the part was placed on rem1 (the left most board section)
                        if (iBoardSection.dWidth < iAssocBoardSection.dWidth)
                        {
                            //if the part overlaps into rem2
                            if (iBoardSection.dWidth + iPart.Width + sawkerf > iAssocBoardSection.dWidth)
                                // adjust the length of rem2 so it does not overlap this part
                                iAssocBoardSection.Length -= (iBoardSection.Length + sawkerf);
                            else
                                // adjust rem1 so it does not overlap rem2
                                iBoardSection.Width -= (iAssocBoardSection.Width + sawkerf);
                        }
                        else
                        {
                            // ...part was placed on rem2 (the right most board section)
                            // if the part overlaps onto rem1
                            if (iBoardSection.dLength + iPart.Length + sawkerf > iAssocBoardSection.dLength)
                                // adjust rem1 so it does not overlap the part
                                iAssocBoardSection.Width -= (iBoardSection.Width + sawkerf);
                            else
                                // adjust rem2 so it does not overlap rem1
                                iBoardSection.Length -= (iAssocBoardSection.Length + sawkerf);
                        }

                        // break the association
                        iAssocBoardSection.AssociatedBoard = null;
                        iBoardSection.AssociatedBoard = null;
                    }
                    #endregion

                    #region // replace the used board with 2 overlapping remainder pieces after subtracting the part ...


                    // create new sections
                    Board boardSection1 = new Board(iBoardSection.ID, iBoardSection.Length - iPart.Length - sawkerf, iBoardSection.Width, iBoardSection.dLength + iPart.Length + sawkerf, iBoardSection.dWidth);
                    Board boardSection2 = new Board(iBoardSection.ID, iBoardSection.Length, iBoardSection.Width - iPart.Width - sawkerf, iBoardSection.dLength, iBoardSection.dWidth + iPart.Width + sawkerf);
                    boardSection1.AssociatedBoard = boardSection2;
                    boardSection2.AssociatedBoard = boardSection1;
                    int boardSection1Index = BoardSectionsCount;
                    int boardSection2Index = BoardSectionsCount;

                    if (boardSection1.Area > Parts[0].Area)
                    {
                        // insert the new rem1 section so the boardsections remain sorted by area
                        for (boardSection1Index = BoardSectionsCount; ; boardSection1Index--)
                            if (boardSection1Index > 0 && BoardSections[boardSection1Index - 1].Area > boardSection1.Area)
                                BoardSections[boardSection1Index] = BoardSections[boardSection1Index - 1];
                            else
                            {
                                BoardSections[boardSection1Index] = boardSection1;
                                break;
                            }
                        BoardSectionsCount++;
                    }
                    else
                    {
                        boardSection1 = null;
                        boardSection1Index = BoardSectionsCount;
                        boardSection2.AssociatedBoard = null;
                    }


                    if (boardSection2.Area > Parts[0].Area)
                    {
                        // insert the new rem2 section so the boardsections remain sorted by area
                        for (boardSection2Index = BoardSectionsCount; ; boardSection2Index--)
                            if (boardSection2Index > 0 && BoardSections[boardSection2Index - 1].Area > boardSection2.Area)
                                BoardSections[boardSection2Index] = BoardSections[boardSection2Index - 1];
                            else
                            {
                                BoardSections[boardSection2Index] = boardSection2;
                                break;
                            }
                        BoardSectionsCount++;
                    }
                    else
                    {
                        boardSection2 = null;
                        boardSection2Index = BoardSectionsCount;

                        if (boardSection1 != null) boardSection1.AssociatedBoard = null;
                    }

                    #endregion

                    //Drawboard_debug(
                    //    Board,
                    //    BoardSections, BoardSectionsCount,
                    //    CurrentSolution, CurrentSolutionDLengths, CurrentSolutionDWidths, CurrentSolutionPartCount, CurrentSolutionTotalArea).Save($"dbgimages\\{Board.ID}_{cn++}.bmp");

                    #region // pack the remaining parts on the remaining boards ...
                    // pack the remaining parts on the remaining boards
                    if (i + 1 < PartsCount)
                        StartPacking(i + 1);
                    #endregion

                    #region // undo the placement so we can iterate to the next part and test with it ...

                    // remove the remainder board sections we added...
                    if (boardSection2Index < BoardSectionsCount)
                    {
                        for (int irem = boardSection2Index; irem < BoardSectionsCount; irem++)
                            BoardSections[irem] = BoardSections[irem + 1];
                        BoardSectionsCount--;
                    }

                    if (boardSection1Index < BoardSectionsCount)
                    {
                        for (int irem = boardSection1Index; irem < BoardSectionsCount; irem++)
                            BoardSections[irem] = BoardSections[irem + 1];
                        BoardSectionsCount--;
                    }

                    // restore associations, and the original associated board sections' sizes
                    if (iAssocBoardSection != null)
                    {
                        iBoardSection.AssociatedBoard = iAssocBoardSection;
                        iAssocBoardSection.AssociatedBoard = iBoardSection;
                        iAssocBoardSection.Length = oAssocLength;
                        iAssocBoardSection.Width = oAssocWidth;
                        iBoardSection.Length = oiBoardLength;
                        iBoardSection.Width = oiBoardWidth;
                    }

                    // place the board back in play
                    iBoardSection.isInUse = false;

                    // remove the part from the temporary solution
                    CurrentSolution[--CurrentSolutionPartCount] = null;
                    CurrentSolutionTotalArea -= iPart.Area;

                    #endregion
                }

            }

        }

    }
}
