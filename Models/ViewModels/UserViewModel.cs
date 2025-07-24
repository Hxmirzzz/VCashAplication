using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VCashApp.Models.ViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; }

        [Display(Name = "Nombre Usuario")]
        public string UserName { get; set; }

        [Display(Name = "Nombre Completo del Usuario")]
        public string NombreUsuario { get; set; }

        [Display(Name = "Correo Electrónico")]
        public string Email { get; set; }

        [Display(Name = "Rol")]
        public string SelectedRole { get; set; }
        public List<int> AssignedBranchIds { get; set; } = new List<int>();

        [Display(Name = "Sucursales Asignadas (Nombres)")]
        public string? AssignedBranchesNames { get; set; }

        public string? SearchTerm { get; set; }
        public string? SelectedRoleFilter { get; set; }
        public int? SelectedBranchFilter { get; set; }

        public List<ViewPermissionViewModel> ViewPermissions { get; set; } = new List<ViewPermissionViewModel>();

    }

    public class ViewPermissionViewModel
    {
        public string CodVista { get; set; }
        public string NombreVista { get; set; }
        public bool PuedeVer { get; set; }
        public bool PuedeCrear { get; set; }
        public bool PuedeEditar { get; set; }
    }
}
