using Microsoft.Extensions.Options;
using VCashApp.Services.Employee.Application.Options;
using VCashApp.Services.Employee.Domain;

namespace VCashApp.Services.Employee.Infrastructure
{
    /// <summary>
    /// Manejo de almacenamiento de archivos en el sistema de archivos local.
    /// </summary>
    public class FileSystemEmployeeStorage : IEmployeeFileStorage
    {
        private readonly string _basePath;

        /// <summary>
        /// Método constructor.
        /// </summary>
        /// <param name="options">Opciones de configuración para el repositorio de empleados.</param>
        public FileSystemEmployeeStorage(IOptions<RepositoryOptions> options)
        {
            _basePath = options.Value.BasePath?.Trim() ?? "";
        }

        /// <summary>
        /// Abre un stream (o null) para una ruta relativa dentro del repositorio unificado.
        /// </summary>
        /// <param name="relativePath">Ruta relativa del archivo a abrir.</param>
        /// <returns>Una tarea que representa la operación asincrónica, con el stream del archivo o null si no existe.</returns>
        public async Task<Stream?> OpenReadAsync(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(_basePath) || string.IsNullOrWhiteSpace(relativePath))
                return null;

            var safeRel = relativePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var full = Path.Combine(_basePath, safeRel);

            if (!File.Exists(full)) return null;
            return await Task.FromResult<Stream?>(File.OpenRead(full));
        }

        /// <summary>
        /// Guarda y devuelve la ruta relativa final (carpeta + nombre) para persistir en DB.
        /// </summary>
        /// <param name="targetFolder">Tarjeta dentro del repositorio unificado.</param>
        /// <param name="fileName"> Nombre del archivo.</param>
        /// <param name="content">Contenido del archivo.</param>
        /// <returns>Una tarea que representa la operación asincrónica, con la ruta relativa del archivo guardado.</returns>
        public async Task<string> SaveAsync(string targetFolder, string fileName, Stream content)
        {
            var folder = Path.Combine(_basePath, targetFolder ?? "");
            Directory.CreateDirectory(folder);

            var safeName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
            var full = Path.Combine(folder, safeName);

            using (var fs = File.Create(full))
            {
                await content.CopyToAsync(fs);
            }

            var rel = Path.Combine(targetFolder ?? "", safeName)
                        .Replace('\\', '/');
            return rel;
        }

        /// <summary>
        /// Elimina un archivo si existe (best-effort).
        /// </summary>
        /// <param name="relativePath">Ruta relativa del archivo a eliminar.</param>
        /// <returns>Una tarea que representa la operación asincrónica.</returns>
        public async Task DeleteIfExistAsync(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return;
            var full = Path.Combine(_basePath, relativePath.TrimStart('\\', '/'));
            if (File.Exists(full)) File.Delete(full);
            await Task.CompletedTask;
        }
    }
}
