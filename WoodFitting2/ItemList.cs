using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WoodFitting2
{
    public class PartNode
    {
        public string ID;
        public double Width;
        public double Length;
        public double Area;
        public double dWidth;
        public double dLength;
        public string Container;

        public PartNode Next;   // the next node in the list
        public PartNode Prev;   // the previous node in the list

        public PartNode() { }

        public PartNode(PartNode original)
        {
            ID = original.ID;
            dWidth = original.dWidth;
            dLength = original.dLength;
            Length = original.Length;
            Width = original.Width;
            Area = original.Area;
            Container = original.Container;
        }

        public PartNode(string id, double length, double width, double dlength = 0, double dwidth = 0)
        {
            ID = id;
            dWidth = dwidth;
            dLength = dlength;
            Length = length;
            Width = width;
            Area = Length * Width;
        }

        public void Inflate(double deltaWidth, double deltaLength)
        {
            //dWidth -= deltaWidth;
            //dLength -= deltaLength;
            Width += 2 * deltaWidth;
            Length += 2 * deltaLength;
        }

        public override string ToString()
        {
            if (Container != "")
                return $"{ID} [{Length,7:0.0} x {Width,5:0.0}] on {Container} @ ({dLength,7:0.0}, {dWidth,5:0.0})";
            return $"{ID} [{Length,7:0.0} x {Width,5:0.0}]";
        }
    }

    public class BoardNode
    {
        public string ID;
        public double Width;
        public double Length;
        public double Area;
        public double dWidth;
        public double dLength;
        public BoardNode AssociatedBoard;

        public BoardNode Next;   // the next node in the list
        public BoardNode Prev;   // the previous node in the list

        public BoardNode()
        { }

        public BoardNode(BoardNode original)
        {
            ID = original.ID;
            dWidth = original.dWidth;
            dLength = original.dLength;
            Length = original.Length;
            Width = original.Width;
            Area = original.Area;
            AssociatedBoard = original.AssociatedBoard;
        }

        public BoardNode(string id, double length, double width, double dlength = 0, double dwidth = 0)
        {
            ID = id;
            dWidth = dwidth;
            dLength = dlength;
            Length = length;
            Width = width;
            Area = Length * Width;
        }

        public override string ToString()
        {
            if (dLength != 0 || dWidth != 0)
                return $"{ID} [{Length,7:0.0} x {Width,5:0.0}] @ ({dLength,7:0.0}, {dWidth,5:0.0})";

            return $"{ID} [{Length,7:0.0} x {Width,5:0.0}]";
        }

        public void Inflate(double deltaWidth, double deltaLength)
        {
            dWidth -= deltaWidth;
            dLength -= deltaLength;
            Width += 2 * deltaWidth;
            Length += 2 * deltaLength;
        }
    }

    public class BoardList
    {
        public BoardNode Head;
        public BoardNode Tail;

        public int Count;
        public double TotalArea
        {
            get
            {
                double tot = 0;
                for (var iBoard = Head; iBoard != null; iBoard = iBoard.Next)
                    tot += iBoard.Area;
                return tot;
            }
        }

        public BoardList()
        {
        }

        public BoardList(BoardList original)
        {
            Head = new BoardNode(original.Head);
            Tail = Head;
            Count = original.Count;
            for (BoardNode iBoard = original.Head.Next; iBoard != null; iBoard = iBoard.Next)
            {
                var newPart = new BoardNode(iBoard);
                newPart.Prev = Tail;
                Tail.Next = newPart;
                Tail = newPart;
            }
        }

        public BoardList(params BoardNode[] boards)
        {
            Head = boards[0];
            Tail = Head;
            Count = boards.Length;
            for (int i = 1; i < Count; i++)
            {
                boards[i].Prev = Tail;
                Tail = Tail.Next = boards[i];
            }
        }

        public void InsertItemSortedbyAreaAsc(BoardNode board)
        {
            if (Head == null)   // list is empty
            {
                board.Prev = null;
                board.Next = null;
                Head = board;
                Tail = board;
                Count = 1;
                return;
            }

            BoardNode iBoard = Head;
            while (iBoard != null && iBoard.Area < board.Area)
                iBoard = iBoard.Next;

            if (iBoard == null)  //passed the end of the list
            {
                board.Prev = Tail;
                Tail.Next = board;
                Tail = board;
            }
            else
            {   // not past the end
                if (iBoard == Head) // at the head
                {
                    board.Prev = null;
                    board.Next = Head;
                    Head.Prev = board;
                    Head = board;
                }
                else
                {
                    board.Prev = iBoard.Prev;
                    board.Next = iBoard;
                    iBoard.Prev.Next = board;
                    iBoard.Prev = board;
                }
            }

            Count++;
        }

        public void Remove(BoardNode board)
        {
            if (board == Head)
            {
                if (board == Tail)
                {
                    Head = null;
                    Tail = null;
                    Count = 0;
                }
                else
                {
                    Head = Head.Next;
                    Head.Prev = null;
                    Count--;
                }
            }
            else
            {
                if (board == Tail)
                {
                    Tail = Tail.Prev;
                    Tail.Next = null;
                    Count--;
                }
                else
                {
                    board.Prev.Next = board.Next;
                    board.Next.Prev = board.Prev;
                    Count--;
                }
            }
        }

        public void Remove(string id)
        {
            BoardNode iBoard = Head;
            while (iBoard.ID != id) iBoard = iBoard.Next;
            if (iBoard == null) return;

            if (iBoard == Head)
            {
                if (iBoard == Tail)
                {
                    Head = null;
                    Tail = null;
                    Count = 0;
                }
                else
                {
                    Head = Head.Next;
                    Head.Prev = null;
                    Count--;
                }
            }
            else
            {
                if (iBoard == Tail)
                {
                    Tail = Tail.Prev;
                    Tail.Next = null;
                    Count--;
                }
                else
                {
                    iBoard.Prev.Next = iBoard.Next;
                    iBoard.Next.Prev = iBoard.Prev;
                    Count--;
                }
            }
        }

        public BoardNode[] ToArray
        {
            get
            {
                List<BoardNode> boardList = new List<BoardNode>();
                for (BoardNode iBoard = Head; iBoard != null; iBoard = iBoard.Next)
                    boardList.Add(iBoard);

                return boardList.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (BoardNode iBoard = Head; iBoard != null; iBoard = iBoard.Next)
                sb.Append($"{iBoard.ID} [{iBoard.Length,7:0.0} x {iBoard.Width,5:0.0}] @ ({iBoard.dLength,7:0.0} , {iBoard.dWidth,5:0.0}) \r\n");
            return sb.ToString();
        }

        public BoardList OrderredByArea()
        {
            BoardList orderredList = new BoardList();
            for (BoardNode iBoard = Head; iBoard != null; iBoard = iBoard.Next)
                orderredList.InsertItemSortedbyAreaAsc(new BoardNode(iBoard));

            return orderredList;
        }
    }

    public class PartList
    {
        public PartNode Head;
        public PartNode Tail;
        public int Count;

        public PartList()
        {
        }

        public PartList(params PartNode[] parts)
        {
            Head = parts[0];
            Tail = Head;
            Count = parts.Length;
            for (int i = 1; i < Count; i++)
            {
                parts[i].Prev = Tail;
                Tail.Next = parts[i];
                Tail = parts[i];
            }
        }

        public PartList(PartList original)
        {
            Head = new PartNode(original.Head);
            Tail = Head;
            Count = original.Count;
            for (PartNode iPart = original.Head.Next; iPart != null; iPart = iPart.Next)
            {
                var newPart = new PartNode(iPart);
                newPart.Prev = Tail;
                Tail.Next = newPart;
                Tail = newPart;
            }
        }

        public void Append(PartNode part)
        {
            if (Head == null)
            {
                part.Next = null;
                part.Prev = null;
                Head = part;
                Tail = part;
                Count = 1;
            }
            else
            {
                part.Next = null;
                part.Prev = Tail;
                Tail.Next = part;
                Tail = part;
                Count++;
            }
        }

        public void InsertItemSortedbyAreaAsc(PartNode part)
        {
            if (Head == null)   // list is empty
            {
                part.Prev = null;
                part.Next = null;
                Head = part;
                Tail = part;
                Count = 1;
                return;
            }

            PartNode iPart = Head;
            while (iPart != null && iPart.Area < part.Area)
                iPart = iPart.Next;

            if (iPart == null)  //passed the end of the list
            {
                part.Prev = Tail;
                Tail.Next = part;
                Tail = part;
            }
            else
            {   // not past the end
                if (iPart == Head) // at the head
                {
                    part.Prev = null;
                    part.Next = Head;
                    Head.Prev = part;
                    Head = part;
                }
                else
                {
                    part.Prev = iPart.Prev;
                    part.Next = iPart;
                    iPart.Prev.Next = part;
                    iPart.Prev = part;
                }
            }

            Count++;
        }

        public void Append(PartList list)
        {
            if (Head == null)
            {
                Head = list.Head;
                Tail = list.Tail;
                Count = list.Count;
            }
            else
            {
                Tail.Next = list.Head;
                list.Head.Prev = Tail;
                Tail = list.Tail;
                Count += list.Count;
            }

        }

        public void Remove(PartNode part)
        {
            if (part == Head)
            {
                if (part == Tail)
                {
                    Head = null;
                    Tail = null;
                    Count = 0;
                }
                else
                {
                    Head = Head.Next;
                    Head.Prev = null;
                    Count--;
                }
            }
            else
            {
                if (part == Tail)
                {
                    Tail = Tail.Prev;
                    Tail.Next = null;
                    Count--;
                }
                else
                {
                    part.Prev.Next = part.Next;
                    part.Next.Prev = part.Prev;
                    Count--;
                }
            }
        }

        public void Remove(string id)
        {
            PartNode iPart = Head;
            while (iPart.ID != id) iPart = iPart.Next;

            if (iPart == Head)
            {
                if (iPart == Tail)
                {
                    Head = null;
                    Tail = null;
                    Count = 0;
                }
                else
                {
                    Head = Head.Next;
                    Head.Prev = null;
                    Count--;
                }
            }
            else
            {
                if (iPart == Tail)
                {
                    Tail = Tail.Prev;
                    Tail.Next = null;
                    Count--;
                }
                else
                {
                    iPart.Prev.Next = iPart.Next;
                    iPart.Next.Prev = iPart.Prev;
                    Count--;
                }
            }
        }

        public PartNode[] ToArray
        {
            get
            {
                List<PartNode> partList = new List<PartNode>();
                for (var iPart = Head; iPart != null; iPart = iPart.Next)
                    partList.Add(iPart);

                return partList.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (PartNode iPart = Head; iPart != null; iPart = iPart.Next)
                sb.Append($"{iPart.ID} [{iPart.Length,7:0.0} x {iPart.Width,5:0.0}] @ ({iPart.dLength,7:0.0} , {iPart.dWidth,5:0.0}) \r\n");
            return sb.ToString();
        }

        public void InflateAll(double deltaWidth, double deltaHeight)
        {
            for (PartNode iPart = Head; iPart != null; iPart = iPart.Next)
                iPart.Inflate(deltaWidth, deltaHeight);
        }

        public double TotalArea
        {
            get
            {
                double tot = 0;
                for (var iPart = Head; iPart != null; iPart = iPart.Next)
                    tot += iPart.Area;
                return tot;
            }
        }

        public PartList OrderredByArea()
        {
            PartList orderredList = new PartList();
            for (PartNode iPart = Head; iPart != null; iPart = iPart.Next)
                orderredList.InsertItemSortedbyAreaAsc(new PartNode(iPart));

            return orderredList;
        }
    }

}
