using System.ComponentModel.DataAnnotations;
namespace VCashApp.Services.DTOs
{
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string? Code { get; set; }
        public bool NeedsConfirmation { get; set; } = false;
        public string? ValidationType { get; set; }
        public double? HoursWorked { get; set; }
        public int? Id { get; set; }
        public string? UserId { get; set; }

        public object? Data { get; set; }

        public Dictionary<string, string[]>? Errors { get; set; }

        public static ServiceResult SuccessResult(string message, int? id = null, string? userId = null) => new ServiceResult { Success = true, Message = message, Id = id, UserId = userId };
        public static ServiceResult SuccessResult(string message, object? data) => new ServiceResult { Success = true, Message = message, Data = data };
        public static ServiceResult FailureResult(string message) => new ServiceResult { Success = false, Message = message };
        public static ServiceResult FailureResult(string message, Dictionary<string, string[]>? errors) =>
            new ServiceResult { Success = false, Message = message, Errors = errors };
        public static ServiceResult FailureResult(string message, string? code)
            => new() { Success = false, Message = message, Code = code };
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
}