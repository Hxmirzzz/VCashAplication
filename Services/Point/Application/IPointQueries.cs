using VCashApp.Models.Dtos.Point;

namespace VCashApp.Services.Point.Application
{
    public interface IPointQueries
    {
        /// <summary>Devuelve lista paginada de puntos según filtros.</summary>
        Task<(IEnumerable<PointListDto> Items, int TotalCount)> GetPagedAsync(PointFilterDto filter);

        /// <summary>Devuelve lista completa para exportación.</summary>
        Task<IEnumerable<PointListDto>> ExportAsync(PointFilterDto filter);

        /// <summary>Devuelve datos necesarios para los select del formulario.</summary>
        Task<PointLookupDto> GetLookupsAsync();

        /// <summary>Devuelve datos completos para edición.</summary>
        Task<PointUpsertDto?> GetForEditAsync(string codPunto);

        /// <summary>Devuelve datos listos para Vista Previa (KPIs, datos extendidos).</summary>
        Task<PointPreviewDto?> GetPreviewAsync(string codPunto);
    }
}
