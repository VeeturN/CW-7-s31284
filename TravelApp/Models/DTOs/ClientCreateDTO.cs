using System.ComponentModel.DataAnnotations;

namespace TravelApp.Models.DTOs;

public class ClientCreateDTO
{
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; }

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [Phone]
    public string Telephone { get; set; }

    [Required]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "Pesel musi składać się z 11 cyfr.")]
    public string Pesel { get; set; }
}