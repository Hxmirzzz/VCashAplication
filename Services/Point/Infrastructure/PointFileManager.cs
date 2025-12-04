using Microsoft.Extensions.Options;
using VCashApp.Extensions;

namespace VCashApp.Services.Point.Infrastructure
{
    public sealed class PointFileManager
    {
        private readonly string _rootPath;

        private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
        private const long MaxFileSize = 30 * 1024 * 1024; // 30MB

        public PointFileManager(IOptions<RepositorioOptions> opts)
        {
            _rootPath = opts.Value.BasePath;
        }

        /// <summary>
        /// Guarda la carta de inclusión y devuelve el filename.
        /// </summary>
        public async Task<string> SaveCartaAsync(int codCliente, string codPCliente, IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("Archivo vacío.");

            if (file.Length > MaxFileSize)
                throw new InvalidOperationException("El archivo supera el límite de 30MB.");

            var ext = Path.GetExtension(file.FileName).ToLower();
            if (!AllowedExtensions.Contains(ext))
                throw new InvalidOperationException("Extensión no permitida.");

            string clienteDir = Path.Combine(_rootPath, "Points", codCliente.ToString());
            string inclusionDir = Path.Combine(clienteDir, "CartasInclusion");

            Directory.CreateDirectory(inclusionDir);

            string fileName = $"{codCliente}-{codPCliente}{ext}";
            string fullPath = Path.Combine(inclusionDir, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
                await file.CopyToAsync(stream);

            return fileName;
        }

        /// <summary>
        /// Elimina la carta actual del punto si existe.
        /// </summary>
        public void DeleteCarta(int codCliente, string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return;

            string path = Path.Combine(_rootPath, "Points", codCliente.ToString(), "CartasInclusion", fileName);

            if (File.Exists(path))
                File.Delete(path);
        }
    }
}