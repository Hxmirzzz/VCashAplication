using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace VCashApp.Services
{
    /// <summary>
    /// Interfaz para un servicio que maneja la exportación de datos a diferentes formatos de archivo.
    /// </summary>
    public interface IExportService
    {
        /// <summary>
        /// Exporta una colección de datos a un formato de archivo específico (ej. Excel, CSV, PDF, JSON).
        /// </summary>
        /// <typeparam name="T">El tipo de los objetos en la colección de datos a exportar.</typeparam>
        /// <param name="data">La colección de datos a exportar.</param>
        /// <param name="exportFormat">El formato de exportación deseado (ej., "xlsx", "csv", "pdf", "json").</param>
        /// <param name="fileNamePrefix">El prefijo para el nombre del archivo de salida (ej., "Reporte_").</param>
        /// <param name="columnDisplayNames">
        /// Un diccionario opcional que mapea los nombres de las propiedades de 'T' a los nombres de columna deseados en la salida.
        /// Si es nulo o una propiedad no está mapeada, se usará el nombre de la propiedad.
        /// </param>
        /// <returns>
        /// Un <see cref="FileContentResult"/> que contiene el contenido del archivo a descargar,
        /// o lanza una <see cref="NotImplementedException"/> si el formato no está soportado.
        /// </returns>
        Task<FileContentResult> ExportDataAsync<T>(
            IEnumerable<T> data,
            string exportFormat,
            string fileNamePrefix,
            Dictionary<string, string>? columnDisplayNames = null);
    }
}