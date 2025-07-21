using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Reflection;
using ClosedXML.Excel;
using System.Linq;

using Newtonsoft.Json.Serialization;

namespace VCashApp.Services
{
    public class ExportService : IExportService
    {
        // Constructor (sin cambios)
        public ExportService()
        {
        }

        // Método auxiliar para filtrar propiedades (sin cambios)
        private PropertyInfo[] GetExportableProperties<T>()
        {
            return typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p =>
                    (p.PropertyType.IsPrimitive ||
                     p.PropertyType == typeof(string) ||
                     p.PropertyType == typeof(DateTime) ||
                     p.PropertyType == typeof(DateTime?) ||
                     p.PropertyType == typeof(DateOnly) ||
                     p.PropertyType == typeof(DateOnly?) ||
                     p.PropertyType == typeof(TimeOnly) ||
                     p.PropertyType == typeof(TimeOnly?) ||
                     p.PropertyType.IsValueType && Nullable.GetUnderlyingType(p.PropertyType) != null && Nullable.GetUnderlyingType(p.PropertyType)!.IsPrimitive) &&
                    !(p.PropertyType.IsGenericType &&
                      p.PropertyType.GetGenericTypeDefinition() == typeof(List<>) &&
                      p.PropertyType.GetGenericArguments()[0] == typeof(Microsoft.AspNetCore.Mvc.Rendering.SelectListItem))) // Asegúrate del namespace completo para SelectListItem
                .ToArray();
        }

        // Método principal de exportación (sin cambios en la estructura)
        public async Task<FileContentResult> ExportDataAsync<T>(
            IEnumerable<T> data,
            string exportFormat,
            string fileNamePrefix,
            Dictionary<string, string>? columnDisplayNames = null)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "Los datos para exportar no pueden ser nulos.");
            }

            string fileName = $"{fileNamePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}";
            byte[] fileBytes;
            string contentType;

            switch (exportFormat.ToUpper())
            {
                case "CSV":
                    fileBytes = GenerateCsv(data, columnDisplayNames);
                    contentType = "text/csv";
                    fileName += ".csv";
                    break;
                case "JSON":
                    fileBytes = GenerateJson(data, columnDisplayNames);
                    contentType = "application/json";
                    fileName += ".json";
                    break;
                case "EXCEL":
                    fileBytes = GenerateExcel(data, columnDisplayNames);
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    fileName += ".xlsx";
                    break;
                case "PDF":
                    throw new NotImplementedException("La exportación a PDF no está implementada completamente y requiere una librería dedicada (ej. QuestPDF o iTextSharp).");
                default:
                    throw new ArgumentException($"Formato de exportación no soportado: {exportFormat}");
            }

            return new FileContentResult(fileBytes, contentType)
            {
                FileDownloadName = fileName
            };
        }

        // Generar CSV (se ajusta para usar FormatValue)
        private byte[] GenerateCsv<T>(IEnumerable<T> data, Dictionary<string, string>? columnDisplayNames)
        {
            var sb = new StringBuilder();
            var properties = GetExportableProperties<T>();

            sb.AppendLine(string.Join(",", properties.Select(p => EscapeCsv(columnDisplayNames != null && columnDisplayNames.ContainsKey(p.Name) ? columnDisplayNames[p.Name] : p.Name))));

            foreach (var item in data)
            {
                sb.AppendLine(string.Join(",", properties.Select(p => EscapeCsv(FormatValue(p.GetValue(item), p.PropertyType)?.ToString())))); // <-- Convertir a string
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        // Generar Excel (CORREGIDO)
        private byte[] GenerateExcel<T>(IEnumerable<T> data, Dictionary<string, string>? columnDisplayNames)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Datos");

                var properties = GetExportableProperties<T>();

                // Add headers
                for (int i = 0; i < properties.Length; i++)
                {
                    string headerName = columnDisplayNames != null && columnDisplayNames.ContainsKey(properties[i].Name) ? columnDisplayNames[properties[i].Name] : properties[i].Name;
                    worksheet.Cell(1, i + 1).Value = headerName;
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                    worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                // Add data
                int row = 2;
                foreach (var item in data)
                {
                    for (int i = 0; i < properties.Length; i++)
                    {
                        var prop = properties[i];
                        var rawValue = prop.GetValue(item); // Valor original del objeto
                        var formattedValue = FormatValue(rawValue, prop.PropertyType); // Valor formateado (puede ser string, DateOnly, etc.)

                        // --- CORRECCIÓN CLAVE AQUÍ: Asignar directamente el valor formateado o string.Empty si es null ---
                        if (formattedValue != null)
                        {
                            // ClosedXML puede manejar muchos tipos directamente, pero a veces es mejor un ToString()
                            // para evitar conversiones implícitas problemáticas de 'object'.
                            worksheet.Cell(row, i + 1).Value = formattedValue.ToString();
                        }
                        else
                        {
                            worksheet.Cell(row, i + 1).Value = string.Empty; // Asignar cadena vacía para null
                        }
                    }
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        // Generar JSON (CORREGIDO)
        private byte[] GenerateJson<T>(IEnumerable<T> data, Dictionary<string, string>? columnDisplayNames)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                DateFormatString = "dd/MM/yyyy HH:mm:ss",
                ContractResolver = new DefaultContractResolver // <-- Corregido con using
                {
                    NamingStrategy = new CamelCaseNamingStrategy() // <-- Corregido con using
                }
            };

            var filteredAndRenamedData = data.Select(item =>
            {
                var result = new Dictionary<string, object?>();
                foreach (var prop in GetExportableProperties<T>())
                {
                    var value = FormatValue(prop.GetValue(item), prop.PropertyType);
                    string keyName = columnDisplayNames != null && columnDisplayNames.ContainsKey(prop.Name) ? columnDisplayNames[prop.Name] : prop.Name;

                    // Aplicar CamelCase para JSON si NamingStrategy está configurado
                    if (settings.ContractResolver is DefaultContractResolver defaultResolver && defaultResolver.NamingStrategy is CamelCaseNamingStrategy camelCaseStrategy)
                    {
                        keyName = camelCaseStrategy.GetPropertyName(keyName, false); // No necesitas el "Get" en GetPropertyName.
                    }
                    result[keyName] = value;
                }
                return result;
            });

            var json = JsonConvert.SerializeObject(filteredAndRenamedData, settings);
            return Encoding.UTF8.GetBytes(json);
        }

        // --- MÉTODOS AUXILIARES GENÉRICOS ---
        private string EscapeCsv(string? value)
        {
            if (value == null) return "";
            value = value.Replace("\"", "\"\"");
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return $"\"{value}\"";
            }
            return value;
        }

        // Método para formatear valores, incluyendo DateOnly y TimeOnly
        private object? FormatValue(object? value, Type propertyType)
        {
            if (value == null) return null;

            // Manejo de valores de enumeración (Ej. EstadoRuta)
            if (propertyType.IsEnum)
            {
                return Enum.GetName(propertyType, value)?.Replace("_", " ") ?? value.ToString();
            }
            // Manejo de valores de enumeración anulables (Ej. int? a un Enum)
            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && Nullable.GetUnderlyingType(propertyType)?.IsEnum == true)
            {
                Type underlyingType = Nullable.GetUnderlyingType(propertyType)!;
                return Enum.GetName(underlyingType, value)?.Replace("_", " ") ?? value.ToString();
            }

            if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
            {
                return ((DateTime)value).ToString("dd/MM/yyyy HH:mm:ss");
            }
            if (propertyType == typeof(DateOnly) || propertyType == typeof(DateOnly?))
            {
                return ((DateOnly)value).ToString("dd/MM/yyyy");
            }
            if (propertyType == typeof(TimeOnly) || propertyType == typeof(TimeOnly?))
            {
                return ((TimeOnly)value).ToString("HH:mm:ss");
            }
            if (propertyType == typeof(bool) || propertyType == typeof(bool?)) // Manejo para booleanos
            {
                return (bool)value ? "Sí" : "No"; // O "Activo" / "Inactivo"
            }

            return value.ToString();
        }
    }
}