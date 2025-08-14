using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VCashApp.Models.ViewModels
{
    /// <summary>
    /// Representa la información principal de una sucursal.
    /// </summary>
    public class BranchViewModel
    {
        /// <summary>
        /// Identificador único de la sucursal.
        /// </summary>
        public int CodSucursal { get; set; }

        /// <summary>
        /// Nombre de la sucursal.
        /// </summary>
        [Display(Name = "Nombre de la Sucursal")]
        [Required(ErrorMessage = "El nombre de la sucursal es obligatorio.")]
        [StringLength(256, ErrorMessage = "El nombre de la sucursal no puede exceder los 256 caracteres.")]
        public string NameBranch { get; set; }

        /// <summary>
        /// Latitud geográfica de la sucursal.
        /// </summary>
        [Display(Name = "Latitud")]
        public string? Latitude { get; set; }

        /// <summary>
        /// Longitud geográfica de la sucursal.
        /// </summary>
        [Display(Name = "Longitud")]
        public string? Longitude { get; set; }

        /// <summary>
        /// Siglas asociadas a la sucursal.
        /// </summary>
        [Display(Name = "Siglas de la Sucursal")]
        [Required(ErrorMessage = "Las siglas de la sucursal son obligatorias.")]
        [StringLength(5, ErrorMessage = "Las siglas de la sucursal no pueden exceder los 5 caracteres.")]
        public string Initials { get; set; }

        /// <summary>
        /// Código del centro operativo al que pertenece la sucursal.
        /// </summary>
        [Display(Name = "Centro Operativo")]
        public string? CoSucursal { get; set; }

        /// <summary>
        /// Indicador de si la sucursal está asociada al Banco de la República.
        /// </summary>
        [Display(Name = "Banco República")]
        public int? BancoRepublica { get; set; }

        /// <summary>
        /// Estado actual de la sucursal.
        /// </summary>
        [Display(Name = "Estado")]
        [Required(ErrorMessage = "El estado es obligatorio.")]
        public bool BranchStatus { get; set; }

        /// <summary>
        /// Texto de búsqueda auxiliar para filtrar sucursales.
        /// </summary>
        public string? SearchTerm { get; set; }
    }
}
