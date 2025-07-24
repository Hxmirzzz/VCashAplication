using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VCashApp.Models.ViewModels
{
    public class BranchViewModel
    {
        public int CodSucursal { get; set; }

        [Display(Name = "Nombre de la Sucursal")]
        [Required(ErrorMessage = "El nombre de la sucursal es obligatorio.")]
        [StringLength(256, ErrorMessage = "El nombre de la sucursal no puede exceder los 256 caracteres.")]
        public string NameBranch { get; set; }

        [Display(Name = "Latitud")]
        public string? Latitude { get; set; }

        [Display(Name = "Longitud")]
        public string? Longitude { get; set; }

        [Display(Name = "Siglas de la Sucursal")]
        [Required(ErrorMessage = "Las siglas de la sucursal son obligatorias.")]
        [StringLength(5, ErrorMessage = "Las siglas de la sucursal no pueden exceder los 5 caracteres.")]
        public string Initials { get; set; }

        [Display(Name = "Centro Operativo")]
        public string? CoSucursal { get; set; }

        [Display(Name = "Banco República")]
        public int? BancoRepublica { get; set; }

        [Display(Name = "Estado")]
        [Required(ErrorMessage = "El estado es obligatorio.")]
        public bool BranchStatus { get; set; }
        
        public string? SearchTerm { get; set; }
    }
}
