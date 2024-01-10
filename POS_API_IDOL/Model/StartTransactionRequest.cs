namespace POS_API_IDOL.Model
{
    public class StartTransactionRequest
    {
        public string StoreNo { get; set; }
        public string TerminalNo { get; set; }
    }

    public class StartTransactionResponse
    {
        public string TransactionId { get; set; }
        public string Message { get; set; }
    }
}
