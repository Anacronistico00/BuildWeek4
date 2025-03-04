namespace BuildWeek4.Models
{
    public class Cart
    {
        public Guid IdProdotto { get; set; }
        public string? Dettaglio { get; set; }
        public int Quantita { get; set; }
        public decimal Prezzo { get; set; }
    }
}
