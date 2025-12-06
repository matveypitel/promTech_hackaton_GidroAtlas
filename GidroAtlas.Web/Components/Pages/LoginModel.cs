using System.ComponentModel.DataAnnotations;

namespace GidroAtlas.Web.Components.Pages;

/// <summary>
/// Model for login form
/// </summary>
public class LoginModel
{
    [Required(ErrorMessage = "Введите логин")]
    [MinLength(3, ErrorMessage = "Логин должен содержать минимум 3 символа")]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите пароль")]
    [MinLength(4, ErrorMessage = "Пароль должен содержать минимум 4 символа")]
    public string Password { get; set; } = string.Empty;
}
