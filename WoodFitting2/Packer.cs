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
        /// <summary>
        ///  do the internal preperation to pack a set of parts onto a set of boards with a collection of options
        /// </summary>
        /// <param name="parts"></param>
        /// <param name="boards"></param>
        /// <param name="sawkerf"></param>
        /// <param name="boardMarginLength"></param>
        /// <param name="boardMarginWidth"></param>
        /// <param name="partPaddingLength"></param>
        /// <param name="partPaddingWidth"></param>
        /// <returns></returns>
        static public void Pack(Part[] parts, Board[] boards, double sawkerf = 4, double partPaddingLength = 0, double partPaddingWidth = 0)
        {
            // order the parts and boards by Area, Ascending
            int partcount = parts.Length;
            Part[] orderredParts = parts.OrderBy(t => t.Area).ToArray();
            int boardcount = boards.Length;
            Board[] orderredBoards = boards.OrderBy(t => t.Area).ToArray();

            // add padding to all parts
            if (partPaddingLength > 0 || partPaddingWidth > 0)
                for (int i = 0; i < partcount; i++)
                    orderredParts[i].Inflate(partPaddingWidth, partPaddingLength);

            // keep count of the parts and boards used
            int packedPartsCount = 0;
            int packedBoardsCount = 0;

            // repeat until all parts are placed, or all boards packed
            while (packedPartsCount < partcount && packedBoardsCount < boardcount)
            {
                Task[] threads = new Task[boardcount];
                for (int i = 0; i < boardcount; i++)
                {
                    threads[i] = Task.Factory.StartNew((o) =>
                    {
                        // reference board[i]
                        int boardIndex = (int)o;
                        Board tiBoard = orderredBoards[boardIndex];
                        if (tiBoard.Complete) return;

                        tiBoard.PackedParts = new Part[partcount];
                        tiBoard.PartdLengths = new double[partcount];
                        tiBoard.PartdWidths = new double[partcount];
                        tiBoard.PackedArea = 0;
                        tiBoard.PartsCount = 0;

                        // init a packer object
                        Packer_internal iPacker = new Packer_internal()
                        {
                            sawkerf = sawkerf,
                            MainBoard = tiBoard,
                            Parts = orderredParts,
                            PartsCount = partcount,
                            BoardSections = new Board[2 * partcount + 2],
                            BoardSectionsCount = 1,
                            ActiveBoardSectionsCount = 1,
                            iSolution = new Part[partcount],
                            iSolLocLength = new double[partcount],
                            iSolLocWidth = new double[partcount]
                        };
                        iPacker.BoardSections[0] = new Board(tiBoard);

                        // pack the board recursively, starting at the first part and an empty solution
                        iPacker.StartPacking();
                    }, i);
                }
                Task.WaitAll(threads);

                // set the complete flag for the board with the best coverage
                IEnumerable<Board> incompleteBoards = orderredBoards.Where(q => !q.Complete);

                Board BestCoverredBoard = incompleteBoards.OrderByDescending(t => t.PackedArea/t.Area).FirstOrDefault();
                if (BestCoverredBoard == null)
                    break;  // if we could not place any parts, break out of iterations loop
                else
                {
                    incompleteBoards.Where(t => t != BestCoverredBoard).ToList().ForEach(t =>
                    {
                        t.PackedParts = null;
                        t.PartsCount = 0;
                        t.PackedArea = 0;
                    });
                    Trace.WriteLine($"...Board {BestCoverredBoard.ID} solved ({BestCoverredBoard.PackedArea/BestCoverredBoard.Area:0.0%})");
                    for (int j = 0; j < BestCoverredBoard.PartsCount; j++)
                        Trace.WriteLine($"{BestCoverredBoard.PackedParts?[j].ID,10} [{BestCoverredBoard.PackedParts?[j].Length,7:0.0} x {BestCoverredBoard.PackedParts?[j].Width,5:0.0}] @ ({BestCoverredBoard.PartdLengths[j],7:0.0},{BestCoverredBoard.PartdWidths[j],7:0.0})");

                    BestCoverredBoard.Complete = true; // if at least one board was packed, set the best packed board as complete
                    packedBoardsCount++;
                    packedPartsCount += BestCoverredBoard.PartsCount;
                    BestCoverredBoard.PackedParts = BestCoverredBoard.PackedParts.Where(t => t != null).ToArray();
                    for (int i = 0; i < BestCoverredBoard.PartsCount; i++)
                        BestCoverredBoard.PackedParts[i].Packed = true;
                }
            }


            #region // comments ...
            // repeat until all parts are placed, or boards used up
            //int iteration = 0;
            //while (orderredParts.Count > 0 && oderredBoards.Count > 0)
            //{
            //    // for this iteration, prepare to hold the best board's packing solution
            //    PartList bestsolution = null;
            //    double bestsolutioncoverage = 0;

            //    // we will pack each board in its own thread, so we need to track the threads
            //    List<Task> threads = new List<Task>();

            //    // loop through the available board sections
            //    for (Board iBoard = oderredBoards.Head; iBoard != null; iBoard = iBoard.Next)
            //        threads.Add(
            //            Task.Factory.StartNew((o) =>
            //            {
            //                Thread.CurrentThread.Priority = ThreadPriority.Highest;
            //                // for every board
            //                Board tiBoard = new Board((Board)o);
            //                // subtract the margin from the board
            //                tiBoard.Inflate(-boardMarginWidth, -boardMarginLength);

            //                // init a packer object
            //                Packer iPacker = new Packer()
            //                {
            //                    boardArea = tiBoard.Area,
            //                    sawkerf = sawkerf
            //                };

            //                // pack the board recursively, starting at the first part and an empty solution
            //                iPacker.Pack_recursive(new PartList(orderredParts), new BoardList(tiBoard), new PartList(), 0);

            //                //Trace.WriteLine($"......in iteration {iteration+1}: Board {iPacker.currentSolution.Head.Container} packed to {iPacker.currentSolutionArea/iPacker.boardArea:0 %} :\r\n{iPacker.currentSolution.ToString()}");

            //                // replace the best solution if this one is better
            //                lock (lck)
            //                    if (iPacker.currentSolutionArea / iPacker.boardArea > bestsolutioncoverage)
            //                    {
            //                        bestsolutioncoverage = iPacker.currentSolutionArea / iPacker.boardArea;
            //                        bestsolution = iPacker.currentSolution;
            //                    }
            //            }, iBoard));
            //    Task.WaitAll(threads.ToArray());

            //    // if no board could be packed, stop
            //    if (bestsolutioncoverage == 0)
            //    {
            //        Trace.WriteLine("STOPPING: Non of the parts left to place would fit any of the available boards...");
            //        break;
            //    }

            //    boards[bestsolution.Head.Container].Solution = new PartList(bestsolution);
            //    // report the best packking for this iteration
            //    Trace.WriteLine($"Best solution for iteration {++iteration}: Board {bestsolution.Head.Container} packed to {bestsolutioncoverage:0 %} :\r\n{bestsolution.ToString()}");

            //    // remove best packed board from the list of available boards
            //    oderredBoards.Remove(bestsolution.Head.Container);

            //    // remove the parts packed from the list of required parts
            //    for (Part iPart = bestsolution.Head; iPart != null; iPart = iPart.Next)
            //        orderredParts.Remove(iPart.ID);

            //    // add this partial solution to the complete solution...
            //    completeSolution.Append(bestsolution);
            //}

            //// return the solution
            //return completeSolution; 
            #endregion

        }

        private class Packer_internal
        {
            public Board MainBoard;
            public Part[] Parts;
            public int PartsCount;

            public Board[] BoardSections;
            public int BoardSectionsCount;
            public int ActiveBoardSectionsCount;

            public Part[] iSolution;
            public int iSolCount;
            public double iSolArea;
            public double[] iSolLocLength;
            public double[] iSolLocWidth;

            public double sawkerf;

            public void StartPacking()
            {
                double lastPartLength = -1;
                double lastPartWidth = -1;
                int oldSolCount = iSolCount;
                // loop through the parts, from big to small
                for (int i = PartsCount - 1; i >= 0; i--)
                {
                    Part iPart = Parts[i];

                    #region // check if the part is a viable candidate ...
                    // ignore parts already packed
                    if (iPart.Packed) continue;
                    // ignore parts larger than the largest board section
                    //if (iPart.Area > MainBoard.Area) continue;
                    // short-circuit repeat parts
                    if (iPart.Length == lastPartLength && iPart.Width == lastPartWidth) continue;
                    // ignore parts already temporarily packed
                    if (iSolution.Contains(iPart)) continue;

                    lastPartLength = iPart.Length;
                    lastPartWidth = iPart.Width;
                    #endregion

                    #region // Find first board that will fit the part ...
                    // find first board that will accomodate the part
                    int j = 0;
                    while (j < BoardSectionsCount && BoardSections[j].Area < iPart.Area) j++;
                    while (j < BoardSectionsCount && (BoardSections[j].Disabled || iPart.Length > BoardSections[j].Length || iPart.Width > BoardSections[j].Width)) j++;

                    // if no boards will accomodate the part, continue to next part
                    if (j >= BoardSectionsCount) continue;
                    Board iBoardSection = BoardSections[j];
                    #endregion

                    #region // place the part in the current bin ...
                    iSolLocLength[iSolCount] = iBoardSection.dLength;
                    iSolLocWidth[iSolCount] = iBoardSection.dWidth;
                    iSolution[iSolCount++] = iPart;
                    iSolArea += iPart.Area;
                    #endregion

                    #region // store best solution ...
                    //if this is a better solution than the current best one ... replace the current best one
                    if (iSolArea > MainBoard.PackedArea)
                    {

                        MainBoard.PackedParts = iSolution.Clone() as Part[];
                        MainBoard.PartdLengths = iSolLocLength.Clone() as double[];
                        MainBoard.PartdWidths = iSolLocWidth.Clone() as double[];
                        MainBoard.PartsCount = iSolCount;
                        //Trace.WriteLine($" ...approved solution: [{string.Join(",", MainBoard.PackedParts.Select(t=>t?.ID))}]  ({iSolArea} > {MainBoard.PackedArea})");
                        MainBoard.PackedArea = iSolArea;
                    }
                    else
                    {
                        //if(iSolution[0].ID == "003")
                        //Trace.WriteLine($" ...rejected solution: [{string.Join(",", iSolution.Select(t => t?.ID))}] ({iSolArea} <= {MainBoard.PackedArea})");
                    }
                    #endregion

                    #region // Break association and adjust associate if a board is used that is associated with another to prevent overlapping placements ...
                    Board iAssocBoardSection = iBoardSection.AssociatedBoard;
                    double oAssocLength = 0,
                        oAssocWidth = 0,
                        oiBoardLength = 0,
                        oiBoardWidth = 0;
                    // if the board section used has a buddy from a previous placement, adjust the buddy and break the association
                    if (iAssocBoardSection != null)
                    {
                        oAssocLength = iAssocBoardSection.Length;
                        oAssocWidth = iAssocBoardSection.Width;
                        oiBoardLength = iBoardSection.Length;
                        oiBoardWidth = iBoardSection.Width;

                        //we have to adjust the buddy, so as not to place another part on the overlapping area
                        if (iBoardSection.dWidth < iAssocBoardSection.dWidth)
                        {
                            //if this is Rem1
                            //if the part is wider than the left portion of Rem1
                            if (iBoardSection.dWidth + iPart.Width + sawkerf > iAssocBoardSection.dWidth)
                                iAssocBoardSection.Length -= (iBoardSection.Length + sawkerf);
                            else
                                iBoardSection.Width -= (iAssocBoardSection.Width + sawkerf);
                        }
                        else
                        {
                            //if this is Rem2
                            if (iBoardSection.dLength + iPart.Length + sawkerf > iAssocBoardSection.dLength)
                                iAssocBoardSection.Width -= (iBoardSection.Width + sawkerf);
                            else
                                iBoardSection.Length -= (iAssocBoardSection.Length + sawkerf);
                        }

                        //then break the pair
                        iAssocBoardSection.AssociatedBoard = null;
                        iBoardSection.AssociatedBoard = null;
                    }
                    #endregion

                    #region // replace the used board with 2 overlapping remainder pieces after subtracting the part ...
                    // divide the board into two overlapping remainder sections
                    iBoardSection.Disabled = true;
                    ActiveBoardSectionsCount--;

                    // create new sections
                    Board boardSection1 = new Board(iBoardSection.ID, iBoardSection.Length - iPart.Length - sawkerf, iBoardSection.Width, iBoardSection.dLength + iPart.Length + sawkerf, iBoardSection.dWidth);
                    Board boardSection2 = new Board(iBoardSection.ID, iBoardSection.Length, iBoardSection.Width - iPart.Width - sawkerf, iBoardSection.dLength, iBoardSection.dWidth + iPart.Width + sawkerf);
                    boardSection1.AssociatedBoard = boardSection2;
                    boardSection2.AssociatedBoard = boardSection1;
                    int boardSection1Index = BoardSectionsCount;
                    int boardSection2Index = BoardSectionsCount;

                    if (boardSection1.Area > 0)
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
                        ActiveBoardSectionsCount++;
                        BoardSectionsCount++;
                    }
                    else
                    {
                        boardSection1 = null;
                        boardSection2.AssociatedBoard = null;
                    }

                    
                    if (boardSection2.Area > 0)
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
                        ActiveBoardSectionsCount++;
                        BoardSectionsCount++;
                    }
                    else
                    {
                        boardSection2 = null;
                        if(boardSection1!=null) boardSection1.AssociatedBoard = null;
                    }

                    #endregion

                    #region // pack the remaining parts on the remaining boards ...
                    // pack the remaining parts on the remaining boards
                    StartPacking();
                    #endregion

                    #region // undo the placement so we can iterate to the next part and test with it ...

                    // remove the remainder board sections we added...
                    for (int irem = boardSection2Index; irem < BoardSectionsCount; irem++)
                        BoardSections[irem] = BoardSections[irem + 1];
                    if (boardSection2Index < BoardSectionsCount)
                    {
                        ActiveBoardSectionsCount--;
                        BoardSectionsCount--;
                    }

                    for (int irem = boardSection1Index; irem < BoardSectionsCount; irem++)
                        BoardSections[irem] = BoardSections[irem + 1];
                    if (boardSection1Index < BoardSectionsCount)
                    {
                        ActiveBoardSectionsCount--;
                        BoardSectionsCount--;
                    }
                    

                    // restore associations, and the original associated board's size
                    if (iAssocBoardSection != null)
                    {
                        iBoardSection.AssociatedBoard = iAssocBoardSection;
                        iAssocBoardSection.AssociatedBoard = iBoardSection;
                        iAssocBoardSection.Length = oAssocLength;
                        iAssocBoardSection.Width = oAssocWidth;
                        iBoardSection.Length = oiBoardLength;
                        iBoardSection.Width = oiBoardWidth;
                    }

                    // place the board back
                    iBoardSection.Disabled = false;
                    ActiveBoardSectionsCount++;

                    // remove the part from the temporary solution
                    iSolCount--;
                    iSolution[iSolCount] = null;
                    iSolArea -= iPart.Area;

                    #endregion
                }

                // pop any packed part in iSolution if one is still there from this recursion
                iSolCount = oldSolCount;
            }

        }

    }
}
