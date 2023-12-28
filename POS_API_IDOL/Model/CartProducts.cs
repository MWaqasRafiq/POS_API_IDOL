namespace POS_API_IDOL.Model
{
    public class CartProducts
    {
        public List<ProductDetail> Products { get; set; }
        public TransactionTotal Total { get; set; }
        public CartProducts() 
        {
            Products = new List<ProductDetail>();
            Total = new TransactionTotal();
        }
    }
}
