using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace VCashApp.Models.ViewModels.Employee
{
    /// <summary>
    /// ViewModel que describe los datos básicos de un empleado.
    /// </summary>
    public class EmployeeViewModel
    {
        [Display(Name = "Cédula")]
        [Required(ErrorMessage = "La cédula es obligatoria.")]
        [Range(typeof(long), "1000000", "9999999999", ErrorMessage = "El número de documento debe tener entre 7 y 10 dígitos.")]
        public long CodCedula { get; set; }

        [Display(Name = "Tipo Documento")]
        [Required(ErrorMessage = "El tipo de documento es obligatorio.")]
        public string? TipoDocumento { get; set; }

        [Display(Name = "Primer Nombre")]
        [Required(ErrorMessage = "El primer nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El primer nombre no puede exceder los 100 caracteres.")]
        public string? FirstName { get; set; }

        [Display(Name = "Segundo Nombre")]
        [StringLength(100, ErrorMessage = "El segundo nombre no puede exceder los 100 caracteres.")]
        public string? MiddleName { get; set; }

        [Display(Name = "Primer Apellido")]
        [Required(ErrorMessage = "El primer apellido es obligatorio.")]
        [StringLength(100, ErrorMessage = "El primer apellido no puede exceder los 100 caracteres.")]
        public string? FirstLastName { get; set; }

        [Display(Name = "Segundo Apellido")]
        [StringLength(100, ErrorMessage = "El segundo apellido no puede exceder los 100 caracteres.")]
        public string? SecondLastName { get; set; }

        [Display(Name = "Nombre Completo")]
        public string? NombreCompleto { get; set; }

        [Display(Name = "Número Carnet")]
        [Required(ErrorMessage = "El número de carnet es obligatorio.")]
        [StringLength(50, ErrorMessage = "El número de carnet no puede exceder los 50 caracteres.")]
        public string? NumeroCarnet { get; set; }

        [Display(Name = "Fecha Nacimiento")]
        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
        [DataType(DataType.Date)]
        public DateOnly? FechaNacimiento { get; set; }

        [Display(Name = "Fecha Expedición")]
        [Required(ErrorMessage = "La fecha de expedición es obligatoria.")]
        [DataType(DataType.Date)]
        public DateOnly? FechaExpedicion { get; set; }

        [Display(Name = "Ciudad Expedición")]
        [Required(ErrorMessage = "La ciudad de expedición es obligatoria.")]
        public string? CiudadExpedicion { get; set; }

        [Display(Name = "Cargo")]
        [Required(ErrorMessage = "El cargo es obligatorio.")]
        public int? CargoCode { get; set; }

        [Display(Name = "Nombre Cargo")]
        public string? NombreCargo { get; set; }

        [Display(Name = "Nombre Unidad")]
        public string? NombreUnidad { get; set; }


        [Display(Name = "Sucursal")]
        [Required(ErrorMessage = "La sucursal es obligatoria.")]
        public int? BranchCode { get; set; }

        [Display(Name = "Nombre Sucursal")]
        public string? NombreSucursal { get; set; }

        [Display(Name = "Celular")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "El número de celular debe tener 10 dígitos.")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "El número de celular debe tener exactamente 10 dígitos.")]
        public string? Celular { get; set; }

        [Display(Name = "Dirección")]
        [StringLength(255, ErrorMessage = "La dirección no puede exceder los 255 caracteres.")]
        public string? Direccion { get; set; }

        [Display(Name = "Correo")]
        [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
        [StringLength(255, ErrorMessage = "El correo electrónico no puede exceder los 255 caracteres.")]
        public string? Correo { get; set; }

        [Display(Name = "Tipo de Sangre (RH)")]
        public string? BloodType { get; set; }

        [Display(Name = "Género")]
        [Required(ErrorMessage = "El género es obligatorio.")]
        public string? Genero { get; set; }

        [Display(Name = "Otro Género")]
        [StringLength(50, ErrorMessage = "El otro género no puede exceder los 50 caracteres.")]
        public string? OtroGenero { get; set; }

        [Display(Name = "Fecha Vinculación")]
        [Required(ErrorMessage = "La fecha de vinculación es obligatoria.")]
        [DataType(DataType.Date)]
        public DateOnly? FechaVinculacion { get; set; }

        [Display(Name = "Fecha Retiro")]
        [DataType(DataType.Date)]
        public DateOnly? FechaRetiro { get; set; }

        [Display(Name = "Indicador Catálogo")]
        public bool IndicadorCatalogo { get; set; }

        [Display(Name = "Ingreso República")]
        public bool IngresoRepublica { get; set; }

        [Display(Name = "Ingreso Aeropuerto")]
        public bool IngresoAeropuerto { get; set; }

        [Display(Name = "Estado Empleado")]
        [Required(ErrorMessage = "El estado del empleado es obligatorio.")]
        public int? EmployeeStatus { get; set; }

        [Display(Name = "URL Foto")]
        public string? PhotoPath { get; set; }

        [Display(Name = "URL Firma")]
        public string? SignaturePath { get; set; }

        [Display(Name = "Archivo Foto")]
        public IFormFile? PhotoFile { get; set; }

        [Display(Name = "Archivo Firma")]
        public IFormFile? SignatureFile { get; set; }

        public List<SelectListItem>? Cargos { get; set; }
        public List<SelectListItem>? Sucursales { get; set; }
        public List<SelectListItem>? Ciudades { get; set; }
    }
}