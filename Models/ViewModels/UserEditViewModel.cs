using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VCashApp.Models.ViewModels
{
    public class UserEditViewModel
    {
        public string Id { get; set; }

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

        public List<ViewPermissionViewModel> ViewPermissions { get; set; } = new List<ViewPermissionViewModel>();
    }
}
