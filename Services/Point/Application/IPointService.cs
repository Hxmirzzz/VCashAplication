using VCashApp.Services.DTOs;
using VCashApp.Models.Dtos.Point;

namespace VCashApp.Services.Point.Application
{
    public interface IPointService
    {
        /// <summary>Crear nuevo punto.</summary>
        Task<ServiceResult> CreateAsync(PointUpsertDto dto, IFormFile? cartaFile);

        /// <summary>Actualizar un punto existente.</summary>
        Task<ServiceResult> UpdateAsync(PointUpsertDto dto, IFormFile? cartaFile, bool removeCartaActual);

        /// <summary>Activar o desactivar un punto.</summary>
        Task<ServiceResult> ToggleStatusAsync(string codPunto);

        /// <summary>Genera el código interno CodPunto según reglas (incremental o lógica propia).</summary>
        Task<string> GenerateCodPuntoAsync(int codCliente);

        /// <summary>Validaciones antes de Create/Update.</summary>
        Task<ServiceResult> ValidateAsync(PointUpsertDto dto, bool isEdit);
    }
}