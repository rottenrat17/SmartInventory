namespace SmartInventoryManagement.Models
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public List<Product> Products { get; set; }
    }
}