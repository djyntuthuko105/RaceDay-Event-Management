using RaceDay_Event_Management.API.Contracts;
namespace RaceDay.Web.Models;
public class ProfilePage : UpdateProfileRequest
{
    public IFormFile? PictureFile { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    public string Email { get; set; } = "";
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    public string Role { get; set; } = "";
}
