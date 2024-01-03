namespace POS_API_IDOL.Model
{
    public class PrintReceiptRequest
    {
        public string TransactionId { get; set; }
        public string StoreNo { get; set; }
        public string TerminalNo { get; set; }
        public string MobileNumber { get; set; }
        public bool? GoGreen { get; set; }

    }
}
