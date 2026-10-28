namespace InvoiceApi.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string Address { get; set; } = string.Empty;
    }

    public enum UserRole { Finance, Postman, Client }
}
