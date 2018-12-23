using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WoodFitting2
{
    class ComboFit
    {
        private static ComboList GetCombos(PartList parts, Item board, double cumilativeArea = 0, double minimumArea = 0)
        {
            PartList mycopyofParts = parts.Copy();      // create a copy of the list, because we will be changing the list, but not the contents
            ComboList myCombos = new ComboList();       // collection of all the combinations we could construct

            // loop though all the parts ...
            foreach (var ipart in parts)
            {
                if (!board.BiggerThan(ipart))            // rule out parts that are too long/wide
                {
                    mycopyofParts.Remove(ipart);
                    continue;
                }

                double ncumArea = ipart.Area + cumilativeArea;
                if (ncumArea > board.Area)              // dont add parts that would push the cumilative area of the combination over that of the board
                    continue;


                if (ncumArea > minimumArea)
                    myCombos.Add(new Combo(ipart));

                mycopyofParts.Remove(ipart);
                if (mycopyofParts.Count == 0) continue; // no need to try call myself again if there is no parts left

                ComboList sublist = GetCombos(mycopyofParts, board, ncumArea, minimumArea);
                foreach (var icombo in sublist)
                {
                    double cumsubArea = icombo.CumalativeArea + ncumArea;
                    icombo.Add(ipart);

                    if (icombo.Count > 1 && icombo[0].Width + icombo[1].Width > board.Width && icombo[0].Length + icombo[1].Length > board.Length)
                        continue;

                    myCombos.Add(icombo);
                }
            }

            return myCombos;
        }

        public async static Task<Solution> Packold(PartList parts, BoardList boards)
        {
            #region // Algorithm : ...
            /*
             for each board
                remove all parts that will not fit on board
                do
                    Get the combos of parts that have cumalative area smaller than board, but bigger than 90% of board (we aim high...)
                    level=level*level
                while combos.count = 0
                sort combos by cum area desc
                foreach combo
                    if can fit on board, break

             end for each board   
             keep board with leaste waste

             repeat for boards and parts left

                 */
            #endregion
            if (parts.Count * boards.Count == 0) return null;

            if (!parts.All(t => boards.Any(q => q.BiggerThan(t)))) return null;

            PartList mycopyofParts = parts.Copy();
            BoardList mycopyofBoards = boards.Copy();
            mycopyofParts.Sort(Part.CompareByAreaDecending);

            Solution CompleteSolution = new Solution() { TotalStockArea = boards.Sum(t => t.Area) };
            do
            {
                double minCoverageRatio = 0.9;
                int boardcount = mycopyofBoards.Count;
                List<Solution> iSolutionSet = new List<Solution>();
                object lockobj = new object();
                do
                {
                    List<Task> threads = new List<Task>();
                    Trace.WriteLine($"Getting combinations for {boardcount} boards [{string.Join(",", mycopyofBoards.Select(t => t.Name))}] and {mycopyofParts.Count} parts with at least {minCoverageRatio * 100} % coverage");

                    for (int j = 0; j < boardcount; j++)
                    {
                        // get the best solution for every board
                        threads.Add(
                            Task.Factory.StartNew((o) =>
                            {
                                Item iBoard = mycopyofBoards[(int)o];
                                var iCombos = GetCombos(mycopyofParts, iBoard, 0, minCoverageRatio * iBoard.Area);
                                iCombos.Sort(Combo.CompareByCumAreaDesc);
                                Trace.WriteLine($"Finding combinations for board {iBoard.Name}");

                                Solution topSolution = null;
                                Combo topCombo = iCombos.FirstOrDefault(q => (topSolution = BruteForce.Pack_async(q.AsPartList(), iBoard).Result) != null);
                                if (topSolution != null)
                                {
                                    Trace.WriteLine($"Best satisfactory combination for board {iBoard.Name}: [{string.Join(",", topCombo.Select(q => q.Name))}] ; coverage = {(topSolution.PlacedArea / iBoard.Area * 100):0.0} %, waste = {topSolution.Waste / 1000000} m\u00b2");
                                    iSolutionSet.Add(topSolution);
                                }
                                else
                                    Trace.WriteLine($"No satisfactory combination for board {iBoard.Name}");
                            }, j)
                        );
                    }
                    Task.WaitAll(threads.ToArray());

                    minCoverageRatio -= minCoverageRatio * minCoverageRatio;
                    if (minCoverageRatio < 0.5) minCoverageRatio = 0;
                } while (iSolutionSet.Count == 0);

                // keep the best solution
                iSolutionSet.Sort(Solution.CompareByWasteAscending);

                // check if there is another solution that did not include any of these ...
                foreach (var topSol in iSolutionSet)
                {
                    // if none of the parts in this solution is already part of the kept solutions
                    if (!topSol.Any(t => CompleteSolution.FirstOrDefault(q => q.Part.Name == t.Part.Name) != null))
                    {
                        // add this solution to the kept solutions
                        Item topBoard = topSol.First().Stock;
                        Trace.WriteLine($"Keeping solution for board {topBoard.Name}: [{string.Join(",", topSol.Select(q => q.Part.Name))}] ; coverage = {(topSol.PlacedArea / topBoard.Area * 100):0.0} %, waste = {topSol.Waste / 1000000} m\u00b2");
                        CompleteSolution.AddRange(topSol);

                        // remove the parts and boards
                        topSol.ForEach(t => mycopyofParts.Remove(t.Part));
                        mycopyofBoards.Remove(topBoard);
                    }
                }

                Trace.WriteLine($"{mycopyofBoards.Count} boards and {mycopyofParts.Count} parts remain");
                Trace.WriteLine("");

                if (mycopyofBoards.Count == 0 && mycopyofParts.Count > 0) return null;
            } while (mycopyofParts.Count > 0);

            return CompleteSolution;
        }

        class state
        {
            public PartList Parts { get; set; }
            public Part iPart { get; set; }
            public int i { get; set; }
            public BoardList Boards { get; set; }
        }


        static int partcount = 0;
        static UInt64 count = 0;
        static Part[] parts;

        public static void DoWork(int istart, int level)
        {
            int t = istart;
            List<Task> tasks = new List<Task>();
            for (int i = istart; i < partcount; i++)
            {
                tasks.Add(
                    Task.Factory.StartNew((o) =>
                    {
                        object[] args = (object[])o;

                        int ii = (int)args[0];
                        int ilevel = (int)args[1];

                        DoWork(ii, ilevel + 1);

                        t++;
                    }, new object[] { i + 1, level })
                );
            }

            Task.WaitAll(tasks.ToArray());

        }
        
        public async static Task<Solution> Pack(PartList parts, Item board)
        {
            ComboFit.parts = parts.Where(t => int.Parse(t.Name) < 25).ToArray();
            partcount = ComboFit.parts.Length;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            DoWork(0, 0);
            sw.Stop();
            Trace.WriteLine($"{count} in {sw.ElapsedMilliseconds} ms");
            return null;
        }



    }


}
