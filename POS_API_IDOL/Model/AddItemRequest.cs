namespace POS_API_IDOL.Model
{
    public class AddItemRequest
    {
        /// <summary>
        /// unique transaction id
        /// </summary>
        public string TransactionId { get; set; }
        /// <summary>
        /// Barcode of a product
        /// </summary>
        public string BarCode { get; set; }
        /// <summary>
        /// weight could be here for weighted items in grams
        /// </summary>
        public int Qty { get; set; }
    }
}
