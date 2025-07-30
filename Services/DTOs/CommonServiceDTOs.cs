using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace VCashApp.Services.DTOs
{
    // DTOs para EmployeeLog
    public class EmployeeLogStateDto
    {
        public string Status { get; set; }
        public string? EntryDate { get; set; }
        public string? EntryTime { get; set; }
        public string? ExitDate { get; set; }
        public string? ExitTime { get; set; }
        public string CurrentDate { get; set; }
        public string CurrentTime { get; set; }
        public string? UnitType { get; set; }
        public string? ErrorMessage { get; set; }
        public double? HoursWorked { get; set; }
        public string? ValidationType { get; set; }
        public bool NeedsConfirmation { get; set; }
        public int? OpenLogId { get; set; }
    }

    public class EmployeeLogDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string LastName { get; set; }
        public string? SecondLastName { get; set; }
        public int CargoId { get; set; }
        public string CargoName { get; set; }
        public string UnitId { get; set; }
        public string UnitName { get; set; }
        public int BranchId { get; set; }
        public string BranchName { get; set; }
        public string EntryDate { get; set; }
        public string EntryTime { get; set; }
        public string? ExitDate { get; set; }
        public string? ExitTime { get; set; }
        public bool IsEntryRecorded { get; set; }
        public bool IsExitRecorded { get; set; }
        public string? ConfirmedValidation { get; set; }
    }

    // ServiceResult (la versión completa con Errors)
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public bool NeedsConfirmation { get; set; } = false;
        public string? ValidationType { get; set; }
        public double? HoursWorked { get; set; }
        public int? Id { get; set; }
        public string? UserId { get; set; }

        public object? Data { get; set; }

        public Dictionary<string, string[]>? Errors { get; set; } // Propiedad Errors

        public static ServiceResult SuccessResult(string message, int? id = null, string? userId = null) => new ServiceResult { Success = true, Message = message, Id = id, UserId = userId };
        public static ServiceResult SuccessResult(string message, object? data) => new ServiceResult { Success = true, Message = message, Data = data };
        public static ServiceResult FailureResult(string message) => new ServiceResult { Success = false, Message = message };
        public static ServiceResult FailureResult(string message, Dictionary<string, string[]>? errors) =>
            new ServiceResult { Success = false, Message = message, Errors = errors };
        public static ServiceResult ConfirmationRequired(string message, string validationType, double hours) =>
            new ServiceResult { Success = false, Message = message, NeedsConfirmation = true, ValidationType = validationType, HoursWorked = hours };
    }

    // DTOs para Rutas Diarias (ejemplo)
    public class RutaDiariaCreationDto
    {
        [Required(ErrorMessage = "Debe seleccionar una Sucursal.")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una Sucursal válida.")]
        public int CodSucursal { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una Ruta Maestra.")]
        public string CodRutaSuc { get; set; }

        [Required(ErrorMessage = "La fecha de ejecución es requerida.")]
        public DateOnly FechaEjecucion { get; set; }
    }

    public class GeneracionRutasDiariasResult
    {
        public int RutasCreadas { get; set; }
        public int RutasOmitidas { get; set; }
        public bool ExitoParcial { get; set; }
        public string Mensaje { get; set; }
    }

    public class StatusChangeRequestDTO
    {
        [Required]
        public int EmployeeId { get; set; } // Para asegurarse de que el ID es enviado en el cuerpo
        [Required(ErrorMessage = "El nuevo estado es obligatorio.")]
        public int NewStatus { get; set; } // El valor numérico del enum
        [StringLength(500, ErrorMessage = "La razón no debe exceder los 500 caracteres.")]
        public string? ReasonForChange { get; set; } // Razón del cambio (opcional)
    }
}