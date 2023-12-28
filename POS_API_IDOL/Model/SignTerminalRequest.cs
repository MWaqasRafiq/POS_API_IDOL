namespace POS_API_IDOL.Model
{
    public class SignTerminalRequest
    {
        public string StoreNo { get; set; }
        public string TerminalNo { get; set; }
        public string? UserId { get; set; }
        public string? Password { get; set; }
        public string Type { get; set; }
    }
}
