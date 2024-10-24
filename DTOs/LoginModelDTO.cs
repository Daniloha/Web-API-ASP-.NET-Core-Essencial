using System.ComponentModel.DataAnnotations;

namespace APICatalogo.DTOs
{
    public class LoginModelDTODTO
    {
        [Required(ErrorMessage = "UserName e obrigatorio")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Password e obrigatorio")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
