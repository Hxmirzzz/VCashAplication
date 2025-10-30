namespace VCashApp.Services.Employee.Application.Options
{
    /// <summary>
    /// Representa las opciones de configuración para el repositorio de empleados.
    /// </summary>
    public class RepositoryOptions
    {
        /// <summary>
        /// Obtiene o establece la ruta base donde se almacenan los datos de los empleados.
        /// </summary>
        public string BasePath { get; set; } = string.Empty;
    }
}
