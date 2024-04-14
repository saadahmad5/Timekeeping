using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Timekeeping
{
    public record TimeCard
    {
        [Key]
        public string id { get; set; }
        [Required(ErrorMessage = "Please supply uniqueId", AllowEmptyStrings = false)]
        public string uniqueId { get; set; }
        [Required(ErrorMessage = "Please supply date", AllowEmptyStrings = false)]
        public DateOnly date { get; set; }
        [Required(ErrorMessage = "Please supply hours", AllowEmptyStrings = false)]
        public double hours { get; set; }

        public TimeCard()
        {
            id = Guid.NewGuid().ToString();
        }

    }
}
