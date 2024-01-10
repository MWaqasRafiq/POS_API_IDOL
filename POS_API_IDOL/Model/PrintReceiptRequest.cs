namespace POS_API_IDOL.Model
{
    public class PrintReceiptRequest
    {
        public string TransactionId { get; set; }
        public string MobileNumber { get; set; }
        public bool GoGreen { get; set; }

    }
}
