using RaceDay_Event_Management.API.Contracts;
namespace RaceDay.Web.Models;
public class ProfilePage : UpdateProfileRequest
{
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
}
