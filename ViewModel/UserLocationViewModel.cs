using System.ComponentModel.DataAnnotations;
namespace Village_Manager.ViewModel
{
    public class UserLocationViewModel
    {
        public int Id { get; set; }
        [Required]
        public string Label { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }
        [Required]
        public int UserId { get; set; }
    }
}
