namespace BuildWeek4.Models
{
    public class Product
    {
        public Guid IdProdotto { get; set; }
        public string URlImmagine { get; set; }

        public decimal Prezzo { get; set; }
        public string Dettaglio { get; set; }
        public string Descrizione { get; set; }

        public string Categoria { get; set; }

    }
}
