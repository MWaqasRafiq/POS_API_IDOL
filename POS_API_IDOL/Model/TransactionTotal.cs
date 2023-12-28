namespace POS_API_IDOL.Model
{
    public class TransactionTotal
    {
        public string TransactionId { get; set; }
        public decimal TotalAmount { get; set; }
        public int TotalItems { get; set; }
        public decimal TotalVat { get; set; }
        public decimal TotalDiscount { get; set; }
    }
}
