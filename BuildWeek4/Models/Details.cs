namespace BuildWeek4.Models
{
    public class Details
    {
        public Guid IdProdotto { get; set; }
        public string? URLImmagine { get; set; }

        public decimal Prezzo { get; set; }
        public string? Dettaglio { get; set; }
        public string? Descrizione { get; set; }

        public string? Categoria { get; set; }
        public int Quantita { get; set; }
        public bool OutOfStock { get; set; }
    }
}
