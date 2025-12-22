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
        Task<string> GenerateVatcoCodeAsync(int codCliente, int tipoPunto);

        /// <summary>Validaciones antes de Create/Update.</summary>
        Task<ServiceResult> ValidateAsync(PointUpsertDto dto, bool isEdit);

        /// <summary>Obtiene las opciones de clientes principales para un cliente dado.</summary>
        Task<IReadOnlyList<MainClientOptionDto>> GetMainClientOptionsAsync(int codCliente);

        /// <summary>Metodo asincrono que obtiene el html de los fondos de un punto.</summary>
        Task<string> GetFundsOptionsHtmlAsync(int branchId, int clientId, int mainClientId);

        /// <summary>Metodo asincrono que obtiene el html de las rutas de un punto.</summary>
        Task<string> GetRoutesOptionsHtmlAsync(int branchId);

        /// <summary>Metodo asincrono que obtiene el html de los rangos de un punto.</summary>
        Task<string> GetRangeOptionsHtmlAsync(int clientId);

        /// <summary>Metodo asincrono que obtiene el html de la informacion de un rango.</summary>
        Task<string> GetRangeInfoHtmlAsync(int rangeId);
    }
}