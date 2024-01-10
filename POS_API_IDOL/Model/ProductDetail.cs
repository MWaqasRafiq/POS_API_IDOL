namespace POS_API_IDOL.Model
{
    public class ProductDetail
    {
        public string Description { get; set; }
        public string Description2 { get; set; }
        public int Qty { get; set; }
        public decimal Price { get; set; }
        public decimal Vat { get; set; }
        public string DiscountDesc { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalPrice { get; set; }
        public bool Weighted { get; set; }
        public bool AgeRestriction { get; set; }
        public string BarCode { get; set; }
    }

    public class ProductsDetails
    {
        public ProductDetail[] products { get; set; }
    }
}
