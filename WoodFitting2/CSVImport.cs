using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LINQtoCSV;
using WoodFitting2.Packer_v1;

namespace WoodFitting2
{
    class Import
    {

        class CutListPlusCSVRecord
        {
            [CsvColumn(Name = "Part #", FieldIndex = 1,CanBeNull = false)]
            public string PartNumber { get; set; }

            [CsvColumn(Name= "Sub-Assembly", FieldIndex = 2)]
            public string SubAssembly { get; set; }

            [CsvColumn(Name = "Description", FieldIndex = 3)]
            public string PartName { get; set; }

            [CsvColumn(Name = "Copies", FieldIndex = 4)]
            public string Quantity { get; set; }

            [CsvColumn(Name = "Thickness(T)", FieldIndex = 5)]
            public string Thickness { get; set; }

            [CsvColumn(Name = "Width(W)", FieldIndex = 6)]
            public string Width { get; set; }

            [CsvColumn(Name = "Length(L)", FieldIndex = 7)]
            public string Length { get; set; }

            [CsvColumn(Name = "Material Type", FieldIndex = 8)]
            public string MaterialType { get; set; }

            [CsvColumn(Name = "Material Name", FieldIndex = 9)]
            public string MaterialName { get; set; }

            [CsvColumn(Name = "Can Rotate", FieldIndex = 10)]
            public string CanRotate { get; set; }
            [CsvColumn(Name = "nothing", FieldIndex = 11)]
            public string nothing { get; set; }
        }
        class CSVRecord
        {
            [CsvColumn(Name = "Type", FieldIndex = 1, CanBeNull = false)]
            public string ItemType { get; set; }

            [CsvColumn(Name = "ID", FieldIndex = 2)]
            public string PartID { get; set; }

            [CsvColumn(Name = "Length", FieldIndex = 3)]
            public string Length { get; set; }

            [CsvColumn(Name = "Width", FieldIndex = 4)]
            public string Width { get; set; }
        }

        public static void FromCutlistPlusCSV(string filePath, out PartList parts, out BoardList boards)
        {
            CsvFileDescription inputFileDescription = new CsvFileDescription
            {
                SeparatorChar = ',',
                FirstLineHasColumnNames = false,
                EnforceCsvColumnAttribute = true,
                UseFieldIndexForReadingData = true
            };
            CsvContext cc = new CsvContext();
            
            IEnumerable<CutListPlusCSVRecord> records = new List<CutListPlusCSVRecord>( 
                cc.Read<CutListPlusCSVRecord>(filePath, inputFileDescription));

            parts = new PartList();
            boards = new BoardList();
            foreach (var iline in records.Where(t=>t.PartNumber!="Part #"))
                if(iline.MaterialName == "Stock")
                    boards.Append(new BoardNode(iline.PartName, double.Parse(iline.Length.Replace("mm", "")), double.Parse(iline.Width.Replace("mm", ""))));
                else
                    parts.Append(new PartNode(iline.PartName, double.Parse(iline.Length.Replace("mm", "")), double.Parse(iline.Width.Replace("mm", ""))));
            
        }

        internal static void FromCSV(string path, out PartList parts, out BoardList boards)
        {
            CsvFileDescription inputFileDescription = new CsvFileDescription
            {
                SeparatorChar = ',',
                FirstLineHasColumnNames = false,
                EnforceCsvColumnAttribute = true,
                UseFieldIndexForReadingData = true
            };
            CsvContext cc = new CsvContext();

            IEnumerable<CSVRecord> records = new List<CSVRecord>(
                cc.Read<CSVRecord>(path, inputFileDescription));

            parts = new PartList();
            boards = new BoardList();
            foreach (var iline in records.Where(t => !t.ItemType.StartsWith("#")))
                if (iline.ItemType.ToLower() == "board")
                    boards.Append(new BoardNode(iline.PartID, double.Parse(iline.Length), double.Parse(iline.Width)));
                else
                    parts.Append(new PartNode(iline.PartID, double.Parse(iline.Length), double.Parse(iline.Width)));
        }
    }
}
