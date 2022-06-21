using System.ComponentModel.DataAnnotations;

namespace JWTMinimalAPI.Model
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string VCode { get; set; }
    }
}
