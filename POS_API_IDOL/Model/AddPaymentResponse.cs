namespace POS_API_IDOL.Model
{
    public class AddPaymentResponse
    {
        public int? Code { get; set; }
        public string? StatusMessage { get; set; }
        public string? Status { get; set; }
        public bool? PaymentCompleted { get; set; }
    }
}
