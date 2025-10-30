namespace VCashApp.Services.Employee.Domain
{
    /// <summary>
    /// Define las operaciones para el almacenamiento de archivos relacionados con empleados.
    /// </summary>
    public interface IEmployeeFileStorage
    {
        /// <summary>Devuelve un stream (o null) para una ruta relativa dentro del repositorio unificado.</summary>
        Task<Stream?> OpenReadAsync(string relativePath);
        /// <summary>Guarda y devuelve la ruta relativa final (carpeta + nombre) para persistir en DB.</summary>
        Task<string> SaveAsync(string targetFolder, string fileName, Stream content);
        /// <summary>Elimina un archivo si existe (best-effort).</summary>
        Task DeleteIfExistAsync(string relativePath);
    }
}
