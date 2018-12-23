using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WoodFitting2
{
    class BruteForce
    {
        public static Solution PackALL(PartList parts, Item stock)
        {
            Solution t = PackALL(parts, new BoardList { stock });
            if (t != null)
                t.UsedStockArea = stock.Area;
            return t;
        }

        /// <summary>
        /// ALL parts must fit for an acceptable solution - we use this to check if a given combination of parts will fit on a board.
        /// </summary>
        /// <param name="parts"></param>
        /// <param name="stock"></param>
        /// <returns></returns>
        private static Solution PackALL(PartList parts, BoardList stock)
        {
            Solution sol = new Solution();
            PartList mycopyofParts = parts.Copy();

            Part iPart = mycopyofParts[0];
            mycopyofParts.RemoveAt(0);

            int stockcount = stock.Count;
            for (int i = 0; i < stockcount; i++)
            {
                Item iStock = stock[i];
                if (!iStock.TrySplit(iPart, out Item H1, out Item H2, out Item V1, out Item V2)) continue;

                if (mycopyofParts.Count == 0)
                {
                    sol.Add(iPart, iStock);
                    return sol;
                }

                BoardList myVstock = stock.Copy();
                myVstock.Remove(iStock);
                myVstock.AddRange(V2, V1);
                Solution solV = PackALL(mycopyofParts, myVstock);
                if (solV != null)
                {
                    sol.Add(iPart, iStock);
                    sol.AddRange(solV);
                    return sol;
                }

                BoardList myHstock = stock.Copy();
                myHstock.Remove(iStock);
                myHstock.AddRange(H2, H1);
                Solution solH = PackALL(mycopyofParts, myHstock);
                if (solH != null)
                {
                    sol.Add(iPart, iStock);
                    sol.AddRange(solH);
                    return sol;
                }
            }
            return null;
        }


        /*
                    for the first part,
                    loop through all stock
                        if fits
                            put part on stock and devide remaining stock
                            place remaining parts on remainig stock


                    for the first board,
                    loop through all parts
                        if part fits on stock
                            put part on stock and devide remaining stock
                            place remaining parts on remaining stock



                     
        public static Solution PackALL2(PartList parts, BoardList stock, int level=0)
        {
            Trace.WriteLine($"{level}:started with {parts.Count} parts:");
            parts.ForEach(t => Trace.WriteLine($"{level}:   {t},"));
            Trace.WriteLine($"{level}: ....and {stock.Count} boards:");
            stock.ForEach(t => Trace.WriteLine($"{level}:   {t},"));
            
            if (parts.Count == 0)
            {
                Trace.WriteLine($"{level}:no parts to place. returning empty solution");
                return new Solution();
            }


            Solution sol = new Solution();
            
            //Trace.WriteLine($"{level}:Making local copy of stocklist");
            BoardList mycopyofStock = stock.Copy();

            Board iBoard = mycopyofStock[0];
            Trace.WriteLine($"{level}:remove board {iBoard} from locl copy of stocklist");
            mycopyofStock.RemoveAt(0);

            foreach (Part iPart in parts)
            {               
                if (!iBoard.TrySplit(iPart, out Board H1, out Board H2, out Board V1, out Board V2))
                {
                    Trace.WriteLine($"{level}:part {iPart} does not fit on board {iBoard}");
                    continue;
                }
                Trace.WriteLine($"{level}:part {iPart} fits on board {iBoard} with remainders:");
                Trace.WriteLine($"{level}:   {H1},");
                Trace.WriteLine($"{level}:   {H2},");
                Trace.WriteLine($"{level}:   {V1},");
                Trace.WriteLine($"{level}:   {V2},");
                
                Trace.WriteLine($"{level}:packing remaining parts on remaining boards plus V2 and V1");
                BoardList myVstock = mycopyofStock.Copy();
                myVstock.AddRange(V2, V1);
                PartList partsV = parts.Copy();
                partsV.Remove(iPart);
                Solution solV = PackALL2(partsV, myVstock,level+1);
                if (solV != null)
                {
                    Trace.WriteLine($"{level}:succesfully packed {solV.Count} parts using V2 & V1");
                    Trace.WriteLine($"{level}:merging solution for part {iPart} and remaining parts and returning");
                    sol.Add(iPart, iBoard);
                    sol.AddRange(solV);
                    return sol;
                }

                Trace.WriteLine($"{level}:packing remaining parts on remaining boards plus H2 and H1");
                BoardList myHstock = mycopyofStock.Copy();
                myHstock.AddRange(H2, H1);

                PartList partsH = parts.Copy();
                partsH.Remove(iPart);
                Solution solH = PackALL2(partsH, myHstock,level+1);
                
                if (solH != null)
                {
                    Trace.WriteLine($"{level}:succesfully packed {solH.Count} parts using H2 & H1");
                    Trace.WriteLine($"{level}:merging solution for part {iPart} and remaining parts and returning");
                    sol.Add(iPart, iBoard);
                    sol.AddRange(solH);
                    return sol;
                }

                Trace.WriteLine($"{level}:neither H or V remainder variants procuced succesfull packings...lets try another part in this place in stead...");
            }
            Trace.WriteLine($"{level}:packing failed - returning null");
            return null;
        }

*/




        public async static Task<Solution> Pack_async(PartList parts, Item stock)
        {
            Solution t = await Pack_async(parts, new BoardList { stock });
            if (t != null)
                t.UsedStockArea = stock.Area;
            return t;
        }
        private async static Task<Solution> Pack_async(PartList parts, BoardList stock)
        {
            List<Task> threads = new List<Task>();
            int stockcount = stock.Count;
            Solution[] solutions = new Solution[stockcount];
            
            for (int i = 0; i < stockcount; i++)
            {
                threads.Add(
                Task.Factory.StartNew(async (o) =>
                {
                    int ii = (int)o;
                    Item iStock = stock[ii];
                    PartList mycopyofParts = parts.Copy();
                    Part iPart = mycopyofParts[0];
                    mycopyofParts.RemoveAt(0);

                    if (!iStock.TrySplit(iPart, out Item H1, out Item H2, out Item V1, out Item V2)) return;
                    
                    if (mycopyofParts.Count == 0)
                    {
                        solutions[ii] = new Solution(iPart, iStock);
                        return;
                    }
                    
                    BoardList myVstock = stock.Copy();
                    myVstock.Remove(iStock);
                    myVstock.AddRange(V2, V1);
                    Solution solV = PackALL(mycopyofParts, myVstock);
                    if (solV != null)
                    {
                        solutions[ii] = new Solution(iPart, iStock);
                        solutions[ii].AddRange(solV);
                        return;
                    }

                    BoardList myHstock = stock.Copy();
                    myHstock.Remove(iStock);
                    myHstock.AddRange(H2, H1);
                    Solution solH = PackALL(mycopyofParts, myHstock);
                    if (solH != null)
                    {
                        solutions[ii] = new Solution(iPart, iStock);
                        solutions[ii].AddRange(solH);
                        return;
                    }
                }, i));
            }
            Task.WaitAll(threads.ToArray());
            

            return solutions.FirstOrDefault(t=>t != null);
        }

    }
}
