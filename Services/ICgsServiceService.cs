using System.Collections.Generic;
using System.Threading.Tasks;
using VCashApp.Models.ViewModels.Servicio;
using Microsoft.AspNetCore.Mvc.Rendering;
using VCashApp.Models.Entities;
using VCashApp.Services.DTOs;
using System;

namespace VCashApp.Services
{
    /// <summary>
    /// Interfaz para los servicios de lógica de negocio del Centro de Gestión de Servicios (CGS).
    /// Define las operaciones disponibles para la creación y consulta de solicitudes de servicio.
    /// </summary>
    public interface ICgsServiceService
    {
        /// <summary>
        /// Prepara un ViewModel para el formulario de creación de una nueva solicitud de servicio.
        /// Carga las listas desplegables necesarias y los datos del usuario actual para la vista.
        /// </summary>
        /// <param name="currentUserId">El ID del usuario actual autenticado.</param>
        /// <param name="currentIP">La dirección IP del cliente que realiza la solicitud.</param>
        /// <returns>Un objeto <see cref="CgsServiceRequestViewModel"/> con datos iniciales y listas preparadas.</returns>
        Task<CgsServiceRequestViewModel> PrepareServiceRequestViewModelAsync(string currentUserId, string currentIP);

        /// <summary>
        /// Procesa y guarda una nueva solicitud de servicio a partir de un ViewModel.
        /// Este método valida la lógica de negocio y llama al Stored Procedure para insertar el registro en la base de datos.
        /// </summary>
        /// <param name="viewModel">El <see cref="CgsServiceRequestViewModel"/> con los datos de la solicitud.</param>
        /// <param name="currentUserId">El ID del usuario que realiza la operación.</param>
        /// <param name="currentIP">La dirección IP del cliente.</param>
        /// <returns>Un <see cref="ServiceResult"/> indicando el éxito o fracaso de la operación, y el ServiceOrderId generado en caso de éxito.</returns>
        Task<ServiceResult> CreateServiceRequestAsync(CgsServiceRequestViewModel viewModel, string currentUserId, string currentIP);

        /// <summary>
        /// Obtiene una lista de puntos (oficinas/ATMs) para un cliente y sucursal específicos.
        /// </summary>
        /// <param name="clientCode">Código del cliente.</param>
        /// <param name="branchCode">Código de la sucursal.</param>
        /// <param name="pointType">Tipo de punto a buscar (0 para oficina, 1 para ATM).</param>
        /// <returns>Una lista de <see cref="SelectListItem"/> con los puntos encontrados.</returns>
        Task<List<SelectListItem>> GetPointsByClientAndBranchAsync(int clientCode, int branchCode, int pointType);

        /// <summary>
        /// Obtiene una lista de fondos para un cliente y sucursal específicos.
        /// </summary>
        /// <param name="clientCode">Código del cliente.</param>
        /// <param name="branchCode">Código de la sucursal.</param>
        /// <param name="fundType">Tipo de fondo a buscar (0 para puntos, 1 para ATM).</param>
        /// <returns>Una lista de <see cref="SelectListItem"/> con los fondos encontrados.</returns>
        Task<List<SelectListItem>> GetFundsByClientAndBranchAsync(int clientCode, int branchCode, int fundType);

        /// <summary>
        /// Obtiene los detalles de un punto específico (nombre, dirección, ciudad, etc.) para mostrarlos en el frontend.
        /// </summary>
        /// <param name="pointCode">El código del punto.</param>
        /// <returns>Un objeto anónimo con los detalles del punto si se encuentra; de lo contrario, null.</returns>
        Task<dynamic?> GetPointDetailsAsync(string pointCode);

        /// <summary>
        /// Obtiene los detalles de un fondo específico (nombre, ciudad, etc.) para mostrarlos en el frontend.
        /// </summary>
        /// <param name="fundCode">El código del fondo.</param>
        /// <returns>Un objeto anónimo con los detalles del fondo si se encuentra; de lo contrario, null.</returns>
        Task<dynamic?> GetFundDetailsAsync(string fundCode);

        /// <summary>
        /// Obtiene una lista paginada y filtrada de solicitudes de servicio para el dashboard.
        /// </summary>
        /// <param name="searchTerm">Término de búsqueda para filtrar por ID de servicio, cliente o sucursal.</param>
        /// <param name="clientCode">Código del cliente para filtrar.</param>
        /// <param name="branchCode">Código de la sucursal para filtrar.</param>
        /// <param name="conceptCode">Código del concepto de servicio para filtrar.</param>
        /// <param name="startDate">Fecha de inicio para el filtro por fecha de solicitud.</param>
        /// <param name="endDate">Fecha de fin para el filtro por fecha de solicitud.</param>
        /// <param name="status">Código del estado del servicio para filtrar.</param>
        /// <param name="pageNumber">Número de página actual para la paginación (por defecto, 1).</param>
        /// <param name="pageSize">Tamaño de la página (por defecto, 10).</param>
        /// <param name="currentUserId">El ID del usuario actual para aplicar filtros de permisos.</param>
        /// <param name="isAdmin">Bandera que indica si el usuario es administrador para omitir filtros de permiso.</param>
        /// <returns>Una tupla que contiene la lista de <see cref="CgsServiceSummaryViewModel"/> y el conteo total de registros.</returns>
        Task<Tuple<List<CgsServiceSummaryViewModel>, int>> GetFilteredServiceRequestsAsync(
            string? search, int? clientCode, int? branchCode, int? conceptCode, DateOnly? startDate, DateOnly? endDate, int? status,
            int page = 1, int pageSize = 10, string? currentUserId = null, bool isAdmin = false);

        // -- MÉTODOS PARA POPULAR DROPDOWNS --
        /// <summary>
        /// Obtiene una lista de clientes para ser utilizada en listas desplegables.
        /// </summary>
        /// <returns>Una lista de <see cref="SelectListItem"/> de clientes.</returns>
        Task<List<SelectListItem>> GetClientsForDropdownAsync();

        /// <summary>
        /// Obtiene una lista de sucursales para ser utilizada en listas desplegables.
        /// </summary>
        /// <returns>Una lista de <see cref="SelectListItem"/> de sucursales.</returns>
        Task<List<SelectListItem>> GetBranchesForDropdownAsync();

        /// <summary>
        /// Obtiene una lista de conceptos de servicio para ser utilizada en listas desplegables.
        /// </summary>
        /// <returns>Una lista de <see cref="SelectListItem"/> de conceptos de servicio.</returns>
        Task<List<SelectListItem>> GetServiceConceptsForDropdownAsync();

        /// <summary>
        /// Obtiene una lista de estados de servicio para ser utilizada en listas desplegables.
        /// </summary>
        /// <returns>Una lista de <see cref="SelectListItem"/> de estados de servicio.</returns>
        Task<List<SelectListItem>> GetServiceStatusesForDropdownAsync();

        /// <summary>
        /// Obtiene una lista de modalidades de servicio para ser utilizada en listas desplegables.
        /// </summary>
        /// <returns>Una lista de <see cref="SelectListItem"/> de modalidades de servicio.</returns>
        Task<List<SelectListItem>> GetServiceModalitiesForDropdownAsync();

        /// <summary>
        /// Obtiene la lista de responsables de fallos para los dropdowns.
        /// </summary>
        /// <returns>Una lista de <see cref="SelectListItem"/> de responsables fallo del servicio.</returns>
        Task<List<SelectListItem>> GetFailedResponsiblesForDropdown();

        /// <summary>
        /// Gets details of a point or fund given its code.
        /// Obtiene los detalles de un punto o fondo dado su código.
        /// </summary>
        /// <param name="code">Point or fund code.</param>
        /// <param name="clientId">Client ID.</param>
        /// <param name="isPoint">True if it's a point, false if it's a fund.</param>
        /// <returns>A dynamic object with details (Name, City, Branch, Range).</returns>
        Task<object?> GetLocationDetailsByCodeAsync(string code, int clientId, bool isPoint);
    }
}