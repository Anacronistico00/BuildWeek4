namespace BuildWeek4.Models
{
    public class Utente
    {
        public Guid IdUtente { get; set; }
        public string? Nome { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public bool IsAdmin { get; set; }   
    }
}
