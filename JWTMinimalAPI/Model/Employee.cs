using System.ComponentModel.DataAnnotations;

namespace JWTMinimalAPI.Model
{
    public class Employee
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public double Salary { get; set; }
        public Nullable<long> PhoneNumber { get; set; }
        public bool IsActive { get; set; }
    }
}
