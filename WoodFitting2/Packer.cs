﻿using System;
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
        public double currentSolutionShortcutRatio = 0.85;
        public double currentSolutionArea = 0;
        public PartList currentSolution = null;
        public double boardArea = 0;
        public double sawkerf = 4;
        static readonly object lck = new object();
        static public PartList Pack(PartList parts, BoardList boards, double sawkerf = 4, double boardMarginLength = 0, double boardMarginWidth = 0, double partPaddingLength = 0, double partPaddingWidth = 0)
        {
            // order the parts by Area, Ascending
            var orderredParts = parts.OrderredByArea();
            var oderredBoards = boards.OrderredByArea();

            // add padding to parts
            orderredParts.InflateAll(partPaddingWidth, partPaddingLength);

            // init the bag for the solution
            PartList completeSolution = new PartList();

            // repeat until all parts are placed, or boards used up
            int iteration = 0;
            while (orderredParts.Count > 0 && oderredBoards.Count > 0)
            {
                // for this iteration, prepare to hold the best board's packing solution
                PartList bestsolution = null;
                double bestsolutioncoverage = 0;

                // we will pack each board in its own thread, so we need to track the threads
                List<Task> threads = new List<Task>();

                // loop through the available board sections
                for (BoardNode iBoard = oderredBoards.Head; iBoard != null; iBoard = iBoard.Next)
                    threads.Add(
                        Task.Factory.StartNew((o) =>
                        {
                            // for every board
                            BoardNode tiBoard = new BoardNode((BoardNode)o);
                            // subtract the margin from the board
                            tiBoard.Inflate(-boardMarginWidth, -boardMarginLength);

                            // init a packer object
                            Packer iPacker = new Packer()
                            {
                                boardArea = tiBoard.Area,
                                sawkerf = sawkerf
                            };

                            // pack the board recursively, starting at the first part and an empty solution
                            iPacker.Pack_recursive(orderredParts, new BoardList(tiBoard), new PartList(), 0);

                            // replace the best solution if this one is better
                            lock (lck)
                                if (iPacker.currentSolutionArea / iPacker.boardArea > bestsolutioncoverage)
                                {
                                    bestsolutioncoverage = iPacker.currentSolutionArea / iPacker.boardArea;
                                    bestsolution = iPacker.currentSolution;
                                }
                        }, iBoard));
                Task.WaitAll(threads.ToArray());

                // if no board could be packed, stop
                if (bestsolutioncoverage == 0)
                {
                    Trace.WriteLine("STOPPING: Non of the parts left to place would fit any of the available boards...");
                    break;
                }

                // report the best packking for this iteration
                Trace.WriteLine($"Best solution for iteration {++iteration}: Board {bestsolution.Head.Container} packed to {bestsolutioncoverage:0 %} :\r\n{bestsolution.ToString()}");

                // remove best packed board from the list of available boards
                oderredBoards.Remove(bestsolution.Head.Container);

                // remove the parts packed from the list of required parts
                for (PartNode iPart = bestsolution.Head; iPart != null; iPart = iPart.Next)
                    orderredParts.Remove(iPart.ID);

                // add this partial solution to the complete solution...
                completeSolution.Append(bestsolution);
            }

            // report if not enough boards
            if (orderredParts.Count > 0)
                Trace.WriteLine("STOPPING: Boards are used up, and we have parts left...");

            // return the solution
            return completeSolution;
        }

        private void Pack_recursive(PartList parts, BoardList boards, PartList packedParts, double packedPartsArea)
        {

            // loop through remaining parts
            for (PartNode iPart = parts.Head; iPart != null; iPart = iPart.Next)
            {
                #region // check if the part is a viable candidate ...
                // if adding this part would increase the required volume past that of the board, stop...the rest of the parts are even bigger than this one
                double newPackedPartsArea = packedPartsArea + iPart.Area;
                if (newPackedPartsArea >= boardArea)
                    break;

                // if the previous part was the same size, pass this one - we already completed this iteration
                if (iPart != parts.Head && iPart.Length == iPart.Prev.Length && iPart.Width == iPart.Prev.Width)
                    continue;
                #endregion

                #region // Find first board that will fit the part ...
                // find first board that will accomodate the part
                BoardNode iBoard = boards.Head;
                while (iBoard != null && iPart.Area > iBoard.Area) iBoard = iBoard.Next;
                if (iBoard == null)
                    break;

                while (iBoard != null && (iPart.Length > iBoard.Length || iPart.Width > iBoard.Width)) iBoard = iBoard.Next;
                if (iBoard == null)
                    continue; // if this part cannot fit any board, continue to next part

                #endregion

                #region // place the part ...
                //append the part to the list of packed parts
                PartNode PackedPart = new PartNode(iPart)
                {
                    Container = iBoard.ID,
                    dWidth = iBoard.dWidth,
                    dLength = iBoard.dLength
                };
                packedParts.Append(PackedPart);
                #endregion

                #region // store best solution ...
                //if this is a better solution than the current best one ... replace the current best one
                if (newPackedPartsArea > currentSolutionArea)
                {
                    currentSolutionArea = newPackedPartsArea;
                    currentSolution = new PartList(packedParts);
                }
                #endregion

                #region // pack rmaining parts onto remaining board segmnts ...
                
                // if there are more parts, pack them too
                if (parts.Count == 1) break;
                
                // if the board section used has a buddy from a previous placement, adjust the buddy and break the association
                if (iBoard.AssociatedBoard != null)
                {
                    //we have to adjust the buddy, so as not to place another part on the overlapping area
                    if (iBoard.dWidth < iBoard.AssociatedBoard.dWidth)
                    {
                        //if this is Rem1
                        //if the part is wider than the left portion of Rem1
                        if (iBoard.dWidth + iPart.Width + sawkerf > iBoard.AssociatedBoard.dWidth)
                            iBoard.AssociatedBoard.Length -= (iBoard.Length + sawkerf);
                        else
                            iBoard.Width -= (iBoard.AssociatedBoard.Width + sawkerf);
                    }
                    else
                    {
                        //if this is Rem2
                        if (iBoard.dLength + iPart.Length + sawkerf > iBoard.AssociatedBoard.dLength)
                            iBoard.AssociatedBoard.Width -= (iBoard.Width + sawkerf);
                        else
                            iBoard.Length -= (iBoard.AssociatedBoard.Length + sawkerf);
                    }

                    //then break the pair
                    iBoard.AssociatedBoard.AssociatedBoard = null;
                    iBoard.AssociatedBoard = null;
                }

                // make my own copy of the list, minus the current part
                PartList newParts;
                if (iPart.Prev == null)
                {
                    parts.Head = iPart.Next;
                    newParts = new PartList(parts);
                    parts.Head = iPart;
                }
                else
                {
                    iPart.Prev.Next = iPart.Next;
                    newParts = new PartList(parts);
                    iPart.Prev.Next = iPart;
                }
                newParts.Count--;

                BoardList newBoards = new BoardList(boards);
                newBoards.Remove(iBoard.ID);

                // divide the board into two overlapping remainder sections
                BoardNode boardSection1 = null;
                double l = iBoard.Length - iPart.Length - sawkerf;
                if (l * iBoard.Width >= newParts.Head.Area)
                {
                    boardSection1 = new BoardNode(iBoard.ID, l, iBoard.Width, iBoard.dLength + iPart.Length + sawkerf, iBoard.dWidth);
                    newBoards.InsertItemSortedbyAreaAsc(boardSection1);
                }
                BoardNode boardSection2 = null;
                double w = iBoard.Width - iPart.Width - sawkerf;
                if (w * iBoard.Length >= newParts.Head.Area)
                {
                    boardSection2 = new BoardNode(iBoard.ID, iBoard.Length, w, iBoard.dLength, iBoard.dWidth + iPart.Width + sawkerf);
                    newBoards.InsertItemSortedbyAreaAsc(boardSection2);
                    boardSection2.AssociatedBoard = boardSection1;
                    if (boardSection1 != null) boardSection1.AssociatedBoard = boardSection2;
                }
                

                // remove board sections smaller than the smallest part
                //for (BoardNode i = boards.Head; i != null; i = i.Next) if (i.Area < parts.Head.Area) boards.Remove(i); else break;



                // pack the remaining parts on the remaining boards
                Pack_recursive(newParts, newBoards, packedParts, newPackedPartsArea);

                // remove all the boards we added...
                //if (boardSection1 != null) boards.Remove(boardSection1);
                //if (boardSection2 != null) boards.Remove(boardSection2);

                // place the board back
                //boards.InsertItemSortedbyAreaAsc(iBoard);

                // remove the current part from the list so we can try the next one
                packedParts.Remove(PackedPart);
                #endregion
            }

        }


    }
}