namespace ComponentBOMTool.Models
{
    public class BomItem
    {
        public string PartNumber { get; set; }
        public int Quantity { get; set; }

        public string Manufacturer { get; set; }

        // ✅ NEW FIELDS
        public string Lifecycle { get; set; }
        public string Stock { get; set; }

        public decimal? UnitPrice { get; set; }
        public decimal? TotalPrice { get; set; }

        public string Alternates { get; set; }
    }
}
