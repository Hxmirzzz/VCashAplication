using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VCashApp.Models.ViewModels
{
    /// <summary>
    /// ViewModel utilizado para la creación de nuevos usuarios.
    /// </summary>
    public class UserCreateViewModel
    {

        [Display(Name = "Nombre Usuario")]
        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [StringLength(256, ErrorMessage = "El nombre de usuario no puede exceder los 256 caracteres.")]
        public string UserName { get; set; }

        [Display(Name = "Nombre Completo del Usuario")]
        [Required(ErrorMessage = "El nombre completo del usuario es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre completo no debe exceder los 100 caracteres.")]
        public string NombreUsuario { get; set; }

        [Display(Name = "Correo Electrónico")]
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
        [StringLength(256, ErrorMessage = "El correo electrónico no puede exceder los 256 caracteres.")]
        public string Email { get; set; }

        [Display(Name = "Rol")]
        [Required(ErrorMessage = "El rol es obligatorio.")]
        public string SelectedRole { get; set; }
        public List<SelectListItem> RolesList { get; set; } = new List<SelectListItem>();

        [Display(Name = "Sucursales Asignadas")]
        public List<int> AssignedBranchIds { get; set; } = new List<int>();
        public List<SelectListItem> AvailableBranchesList { get; set; } = new List<SelectListItem>();

        [Display(Name = "Contraseña")]
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(100, ErrorMessage = "La contraseña debe tener al menos {2} caracteres y no más de {1}.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Confirmar Contraseña")]
        [Required(ErrorMessage = "La confirmación de la contraseña es obligatoria.")]
        [Compare("Password", ErrorMessage = "La contraseña y la confirmación de la contraseña no coinciden.")]
        public string ConfirmPassword { get; set; }

        public List<ViewPermissionViewModel> ViewPermissions { get; set; } = new List<ViewPermissionViewModel>();
    }
}
