using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectListItem
using Microsoft.AspNetCore.Http; // For IFormFile
using System.ComponentModel.DataAnnotations; // For DisplayAttribute

namespace VCashApp.Models.ViewModels
{
    public class EmpleadoViewModel
    {
        // Propiedades de la entidad AdmEmpleado (en español en la DB, mapeadas a nombres en inglés aquí)
        [Display(Name = "Cédula")] // Display name for frontend
        public int CodCedula { get; set; } // Mapeo directo a CodCedula de entidad

        [Display(Name = "Tipo Documento")]
        public string? TipoDocumento { get; set; }

        [Display(Name = "Primer Nombre")] // Renombrado
        public string? FirstName { get; set; }

        [Display(Name = "Segundo Nombre")] // Renombrado
        public string? MiddleName { get; set; }

        [Display(Name = "Primer Apellido")] // Renombrado
        public string? FirstLastName { get; set; }

        [Display(Name = "Segundo Apellido")] // Renombrado
        public string? SecondLastName { get; set; }

        [Display(Name = "Nombre Completo")] // Puede ser calculado o mapeado
        public string? NombreCompleto { get; set; } // Mantener si se usa para display

        [Display(Name = "Número Carnet")]
        public string? NumeroCarnet { get; set; }

        [Display(Name = "Fecha Nacimiento")]
        public DateOnly? FechaNacimiento { get; set; }

        [Display(Name = "Fecha Expedición")]
        public DateOnly? FechaExpedicion { get; set; }

        [Display(Name = "Ciudad Expedición")]
        public string? CiudadExpedicion { get; set; }

        [Display(Name = "Código Cargo")] // Renombrado
        public int? CargoCode { get; set; }

        [Display(Name = "Nombre Cargo")] // Para mostrar en UI
        public string? NombreCargo { get; set; }

        [Display(Name = "Nombre Unidad")] // Para mostrar en UI
        public string? NombreUnidad { get; set; }


        [Display(Name = "Código Sucursal")] // Renombrado
        public int? BranchCode { get; set; }

        [Display(Name = "Nombre Sucursal")] // Para mostrar en UI
        public string? NombreSucursal { get; set; }

        [Display(Name = "Celular")]
        public string? Celular { get; set; }

        [Display(Name = "Dirección")]
        public string? Direccion { get; set; }

        [Display(Name = "Correo")]
        public string? Correo { get; set; }

        [Display(Name = "Tipo de Sangre (RH)")]
        public string? BloodType { get; set; }

        [Display(Name = "Género")]
        public string? Genero { get; set; }

        [Display(Name = "Otro Género")]
        public string? OtroGenero { get; set; }

        [Display(Name = "Fecha Vinculación")]
        public DateOnly? FechaVinculacion { get; set; }

        [Display(Name = "Fecha Retiro")]
        public DateOnly? FechaRetiro { get; set; }

        [Display(Name = "Indicador Catálogo")]
        public bool IndicadorCatalogo { get; set; }

        [Display(Name = "Ingreso República")]
        public bool IngresoRepublica { get; set; }

        [Display(Name = "Ingreso Aeropuerto")]
        public bool IngresoAeropuerto { get; set; }

        [Display(Name = "Estado Empleado")]
        public int? EmployeeStatus { get; set; }

        [Display(Name = "URL Foto")]
        public string? PhotoPath { get; set; }

        [Display(Name = "URL Firma")]
        public string? SignaturePath { get; set; }

        [Display(Name = "Archivo Foto")] // Renombrado
        public IFormFile? PhotoFile { get; set; }

        [Display(Name = "Archivo Firma")] // Renombrado
        public IFormFile? SignatureFile { get; set; }

        // Propiedades para SelectLists en la vista (no son del modelo de empleado directamente)
        public List<SelectListItem>? Cargos { get; set; }
        public List<SelectListItem>? Sucursales { get; set; } // Mantener si se usa el nombre en español en la vista
        public List<SelectListItem>? Ciudades { get; set; } // Mantener si se usa el nombre en español en la vista
    }
}