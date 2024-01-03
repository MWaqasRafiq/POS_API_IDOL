namespace POS_API_IDOL.Model
{
    public class PrintReceiptResponse
    {
        public decimal Amount { get; set; }
        public decimal Vat { get; set; }
        public decimal DiscountAmount { get; set; }
        public string Currency { get; set; }
        public string Receipt { get; set; }
    }
}
