using System.ComponentModel.DataAnnotations;

namespace APICatalogo.DTOs
{
    public class RegisterModelDTO
    {
        [Required(ErrorMessage = "Username e obrigatorio")]
        public string UserName { get; set; }
        [EmailAddress(ErrorMessage = "Email invalido"),
            Required(ErrorMessage = "Email e obrigatorio")]
        public string Email { get; set; }
        [DataType(DataType.Password),
            Required(ErrorMessage = "Password e obrigatorio")]
        public string Password { get; set; }
    }
}
