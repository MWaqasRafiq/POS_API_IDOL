namespace POS_API_IDOL.Model
{
    public class AddItemRequest
    {
        public string TransactionId { get; set; }
        public string StoreNo { get; set; }
        public string TerminalNo { get; set; }
        public string BarCode { get; set; }
        /// <summary>
        /// weight could be here for weighted items in grams
        /// </summary>
        public int Qty { get; set; }
    }
}
