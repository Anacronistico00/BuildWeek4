using System.ComponentModel.DataAnnotations;

namespace BuildWeek4.Models
{
    public class EditProduct
    {

        public Guid IdProdotto { get; set; }

        [Display(Name = "URL")]
        [Required(ErrorMessage = "URL Obbligatorio")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Il nome più lungo di 5 caratteri")]
        public string URlImmagine { get; set; }


        [Display(Name = "Prezzo")]
        [Required(ErrorMessage = "Prezzo Obbligatorio")]
        [Range(1, 10000, ErrorMessage = "Il Prezzo deve essere compreso tra 1 e 10000")]
        public decimal Prezzo { get; set; }


        [Display(Name = "Dettaglio")]
        [Required(ErrorMessage = "Dettaglio Obbligatorio")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "i Dettagli devono essere più lungi di 5 caratteri")]
        public string Dettaglio { get; set; }

        [Display(Name = "Descrizione")]
        [Required(ErrorMessage = "Descrizione Obbligatoria")]
        [StringLength(1000, MinimumLength = 5, ErrorMessage = "i Dettagli devono essere più lungi di 5 caratteri")]
        public string Descrizione { get; set; }

        public Guid IdCategoria { get; set; }
    }
}
