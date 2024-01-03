namespace POS_API_IDOL.Model
{
    public class AddPaymentRequest
    {
        public string TransactionId { get; set; }
        public string StoreNo { get; set; }
        public string TerminalNo { get; set; }
        public string PaymentType { get; set; }
        public string BankTransactionId { get; set; }
        public string CardMaskNo { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
    }
}
