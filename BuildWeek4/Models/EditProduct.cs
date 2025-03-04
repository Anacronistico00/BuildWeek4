using System.ComponentModel.DataAnnotations;

namespace BuildWeek4.Models
{
    public class EditProduct
    {

        public Guid IdProdotto { get; set; }

        [Display(Name = "URL")]
        [Required(ErrorMessage = "URL Obbligatorio")]
        [StringLength(1000, MinimumLength = 5, ErrorMessage = "Il nome più lungo di 5 caratteri")]
        public string? URLImmagine { get; set; }


        [Display(Name = "Prezzo")]
        [Required(ErrorMessage = "Prezzo Obbligatorio")]
        [Range(0.01, 10000, ErrorMessage = "Il Prezzo deve essere compreso tra 0,1 e 10000")]
        public decimal Prezzo { get; set; }


        [Display(Name = "Dettaglio")]
        [Required(ErrorMessage = "Dettaglio Obbligatorio")]
        [StringLength(1000, MinimumLength = 2, ErrorMessage = "i Dettagli devono essere più lungi di 2 caratteri")]
        public string? Dettaglio { get; set; }

        [Display(Name = "Descrizione")]
        [Required(ErrorMessage = "Descrizione Obbligatoria")]
        [StringLength(1000, MinimumLength = 5, ErrorMessage = "i Dettagli devono essere più lungi di 5 caratteri")]
        public string? Descrizione { get; set; }

        [Display(Name = "Quantita")]
        [Required(ErrorMessage = "Inserisci una quantità")]
        [Range(0, 100, ErrorMessage = "La quantità deve essere compresa tra 1 e 100")]
        public int Quantita { get; set; }
        public Guid IdCategoria { get; set; }
    }
}
