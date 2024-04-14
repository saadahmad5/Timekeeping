using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Timekeeping
{
    public record Employee
    {
        [Key]
        public string id { get; set; }
        [Key]
        [Required(ErrorMessage = "Please supply uniqueId", AllowEmptyStrings = false)]
        public string uniqueId { get; set; }
        [Required(ErrorMessage = "Please supply name", AllowEmptyStrings = false)]
        public string name { get; set; }
        [Required]
        [EmailAddress(ErrorMessage = "Please supply emailAddress.")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.(com|net|org|gov)$", ErrorMessage = "Invalid email pattern.")]
        public string emailAddress { get; set; }
        public double hourlyWage { get; set; }

        public Employee()
        {
            id = Guid.NewGuid().ToString();
        }
    }
}
