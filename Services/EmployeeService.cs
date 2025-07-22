using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VCashApp.Data;
using VCashApp.Enums;
using VCashApp.Models;
using VCashApp.Models.Entities;
using VCashApp.Models.ViewModels;
using VCashApp.Services.DTOs;

namespace VCashApp.Services
{
    /// <summary>
    /// Servicio para manejar la lógica de negocio relacionada con los empleados.
    /// </summary>
    public class EmployeeService : IEmployeeService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly Serilog.ILogger _logger;
        private const string REPO_BASE_PATH = @"C:\VCash\Repositorio\";

        public EmployeeService(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
            _logger = Log.ForContext<EmployeeService>();
        }

        public async Task<(List<SelectListItem> Cargos, List<SelectListItem> Sucursales, List<SelectListItem> Ciudades)> GetDropdownListsAsync(string currentUserId, bool isAdmin)
        {
            var allActiveBranches = await _context.AdmSucursales
                .Where(s => s.Estado && s.CodSucursal != 32)
                .Select(s => new { s.CodSucursal, s.NombreSucursal })
                .ToListAsync();

            List<SelectListItem> permittedBranchesList;

            if (!isAdmin)
            {
                var permittedBranchIds = await GetUserPermittedBranchIdsAsync(currentUserId);

                permittedBranchesList = allActiveBranches
                    .Where(s => permittedBranchIds.Contains(s.CodSucursal))
                    .Select(s => new SelectListItem { Value = s.CodSucursal.ToString(), Text = s.NombreSucursal })
                    .ToList();
            }
            else
            {
                permittedBranchesList = allActiveBranches
                    .Select(s => new SelectListItem { Value = s.CodSucursal.ToString(), Text = s.NombreSucursal })
                    .ToList();
            }

            var cargos = await _context.AdmCargos
                .Select(c => new SelectListItem { Value = c.CodCargo.ToString(), Text = c.NombreCargo })
                .ToListAsync();

            var cities = await _context.AdmCiudades
                .Include(c => c.Departamento)
                .OrderBy(c => c.NombreCiudad)
                .Select(c => new SelectListItem
                {
                    Value = c.CodCiudad.ToString(),
                    Text = $"{c.NombreCiudad} - {c.Departamento.NombreDepartamento}"
                }).ToListAsync();

            return (cargos, permittedBranchesList, cities);
        }

        public async Task<(IEnumerable<EmpleadoViewModel> Employees, int TotalCount)> GetFilteredEmployeesAsync(
            string currentUserId, int? cargoId, int? branchId, int? employeeStatus,
            string? search, string? gender, int page, int pageSize, bool isAdmin)
        {
            // Obtener sucursales permitidas para el usuario no-admin
            var permittedBranches = new List<int>();
            if (!isAdmin)
            {
                permittedBranches = await GetUserPermittedBranchIdsAsync(currentUserId);
                if (!permittedBranches.Any())
                {
                    return (new List<EmpleadoViewModel>(), 0);
                }
            }

            // Crear Table-Valued Parameter para las sucursales
            DataTable tvpTable = new DataTable();
            tvpTable.Columns.Add("Value", typeof(int));
            foreach (int id in permittedBranches)
            {
                tvpTable.Rows.Add(id);
            }

            // Configurar parámetros para el Stored Procedure
            var pPermittedBranchIds = new SqlParameter("@PermittedBranchIds", tvpTable)
            {
                TypeName = "dbo.IntListType",
                SqlDbType = SqlDbType.Structured
            };

            var pCargoId = new SqlParameter("@CargoId", cargoId ?? (object)DBNull.Value);
            var pBranchIdFilter = new SqlParameter("@BranchIdFilter", branchId ?? (object)DBNull.Value);
            var pEmployeeStatus = new SqlParameter("@EmployeeStatus", employeeStatus ?? (object)DBNull.Value);
            var pSearchTerm = new SqlParameter("@SearchTerm", string.IsNullOrEmpty(search) ? (object)DBNull.Value : search);
            var pGender = new SqlParameter("@Gender", string.IsNullOrEmpty(gender) ? (object)DBNull.Value : gender);
            var pPage = new SqlParameter("@Page", page);
            var pPageSize = new SqlParameter("@PageSize", pageSize);
            var pIsAdmin = new SqlParameter("@IsAdmin", isAdmin);

            var employees = new List<EmpleadoViewModel>();
            int totalCount = 0;

            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "dbo.GetFilteredEmployees";
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddRange(new[] {
            pPermittedBranchIds, pCargoId, pBranchIdFilter, pEmployeeStatus,
            pSearchTerm, pGender, pPage, pPageSize, pIsAdmin
        });

                await _context.Database.OpenConnectionAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    // Leer el primer resultado: el conteo total
                    if (await reader.ReadAsync())
                    {
                        totalCount = reader.GetInt32(0);
                    }

                    // Moverse al segundo resultado: los datos de los empleados
                    await reader.NextResultAsync();

                    // Mapear los resultados al ViewModel
                    while (await reader.ReadAsync())
                    {
                        employees.Add(new EmpleadoViewModel
                        {
                            CodCedula = reader.GetInt32(reader.GetOrdinal("CodCedula")),
                            FirstName = reader.IsDBNull(reader.GetOrdinal("PrimerNombre")) ? null : reader.GetString(reader.GetOrdinal("PrimerNombre")),
                            FirstLastName = reader.IsDBNull(reader.GetOrdinal("PrimerApellido")) ? null : reader.GetString(reader.GetOrdinal("PrimerApellido")),
                            NombreCompleto = reader.IsDBNull(reader.GetOrdinal("NombreCompleto")) ? null : reader.GetString(reader.GetOrdinal("NombreCompleto")),
                            Celular = reader.IsDBNull(reader.GetOrdinal("Celular")) ? null : reader.GetString(reader.GetOrdinal("Celular")),
                            Genero = reader.IsDBNull(reader.GetOrdinal("Genero")) ? null : reader.GetString(reader.GetOrdinal("Genero")),
                            FechaVinculacion = reader.IsDBNull(reader.GetOrdinal("FecVinculacion")) ? (DateOnly?)null : DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("FecVinculacion"))),
                            EmployeeStatus = reader.IsDBNull(reader.GetOrdinal("EmpleadoEstado")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("EmpleadoEstado")),
                            NombreSucursal = reader.IsDBNull(reader.GetOrdinal("NombreSucursal")) ? "Sin asignar" : reader.GetString(reader.GetOrdinal("NombreSucursal")),
                            NombreCargo = reader.IsDBNull(reader.GetOrdinal("NombreCargo")) ? "Sin asignar" : reader.GetString(reader.GetOrdinal("NombreCargo"))
                        });
                    }
                }
            }

            return (employees, totalCount);
        }

        public async Task<EmpleadoViewModel> GetEmployeeForEditAsync(int employeeId, string currentUserId, bool isAdmin)
        {
            var employee = await _context.AdmEmpleados.FindAsync(employeeId);
            if (employee == null) return null;

            // Validar que el usuario tenga permiso para ver este empleado
            if (!isAdmin)
            {
                var permittedBranches = await GetUserPermittedBranchIdsAsync(currentUserId);
                if (!employee.CodSucursal.HasValue || !permittedBranches.Contains(employee.CodSucursal.Value))
                {
                    _logger.Warning("User {UserId} attempted to access employee {EmployeeId} without permission.", currentUserId, employeeId);
                    return null; // Acceso no permitido
                }
            }

            // Mapear a ViewModel
            return new EmpleadoViewModel
            {
                CodCedula = employee.CodCedula,
                TipoDocumento = employee.TipoDocumento,
                FirstName = employee.PrimerNombre,
                MiddleName = employee.SegundoNombre,
                FirstLastName = employee.PrimerApellido,
                SecondLastName = employee.SegundoApellido,
                NumeroCarnet = employee.NumeroCarnet,
                FechaNacimiento = employee.FechaNacimiento,
                FechaExpedicion = employee.FechaExpedicion,
                CiudadExpedicion = employee.CiudadExpedicion,
                CargoCode = employee.CodCargo,
                BranchCode = employee.CodSucursal,
                Celular = employee.Celular,
                Direccion = employee.Direccion,
                Correo = employee.Correo,
                BloodType = employee.RH,
                Genero = employee.Genero,
                OtroGenero = employee.OtroGenero,
                FechaVinculacion = employee.FecVinculacion,
                FechaRetiro = employee.FecRetiro,
                IndicadorCatalogo = employee.IndicadorCatalogo,
                IngresoRepublica = employee.IngresoRepublica,
                IngresoAeropuerto = employee.IngresoAeropuerto,
                EmployeeStatus = (int)employee.EmpleadoEstado,
                PhotoPath = employee.FotoUrl,
                SignaturePath = employee.FirmaUrl,
            };
        }

        public async Task<EmpleadoViewModel> GetEmployeeForDetailsAsync(int employeeId, string currentUserId, bool isAdmin)
        {
            var employee = await _context.AdmEmpleados.FindAsync(employeeId);
            if (employee == null) return null;
            if (!isAdmin)
            {
                var permittedBranches = await GetUserPermittedBranchIdsAsync(currentUserId);
                if (!employee.CodSucursal.HasValue || !permittedBranches.Contains(employee.CodSucursal.Value))
                {
                    _logger.Warning("User {UserId} attempted to access employee {EmployeeId} without permission.", currentUserId, employeeId);
                    return null; // Acceso no permitido
                }
            }
            // Mapear a ViewModel
            return new EmpleadoViewModel
            {
                CodCedula = employee.CodCedula,
                TipoDocumento = employee.TipoDocumento,
                FirstName = employee.PrimerNombre,
                MiddleName = employee.SegundoNombre,
                FirstLastName = employee.PrimerApellido,
                SecondLastName = employee.SegundoApellido,
                NombreCompleto = employee.NombreCompleto,
                NumeroCarnet = employee.NumeroCarnet,
                FechaNacimiento = employee.FechaNacimiento,
                FechaExpedicion = employee.FechaExpedicion,
                CiudadExpedicion = employee.CiudadExpedicion,
                CargoCode = employee.CodCargo,
                BranchCode = employee.CodSucursal,
                Celular = employee.Celular,
                Direccion = employee.Direccion,
                Correo = employee.Correo,
                BloodType = employee.RH,
                Genero = employee.Genero,
                OtroGenero = employee.OtroGenero,
                FechaVinculacion = employee.FecVinculacion,
                FechaRetiro = employee.FecRetiro,
                IndicadorCatalogo = employee.IndicadorCatalogo,
                IngresoRepublica = employee.IngresoRepublica,
                IngresoAeropuerto = employee.IngresoAeropuerto,
                EmployeeStatus = (int)employee.EmpleadoEstado,
                PhotoPath = employee.FotoUrl,
                SignaturePath = employee.FirmaUrl
            };
        }

        public async Task<ServiceResult> CreateEmployeeAsync(EmpleadoViewModel model, string currentUserId)
        {
            if (await _context.AdmEmpleados.AnyAsync(e => e.CodCedula == model.CodCedula))
            {
                return ServiceResult.FailureResult($"Ya existe un empleado con la cédula {model.CodCedula}.");
            }

            try
            {
                var (photoPath, signaturePath) = await ProcessEmployeeFilesAsync(model);

                var newEmployee = new AdmEmpleado
                {
                    PrimerApellido = FormatText(model.FirstLastName),
                    SegundoApellido = FormatText(model.SecondLastName),
                    PrimerNombre = FormatText(model.FirstName),
                    SegundoNombre = FormatText(model.MiddleName),
                    NombreCompleto = string.Join(" ", new[] { FormatText(model.FirstName), FormatText(model.MiddleName), FormatText(model.FirstLastName), FormatText(model.SecondLastName) }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim(),
                    TipoDocumento = model.TipoDocumento,
                    CodCedula = model.CodCedula,
                    NumeroCarnet = model.NumeroCarnet,
                    FechaNacimiento = model.FechaNacimiento,
                    FechaExpedicion = model.FechaExpedicion,
                    CiudadExpedicion = model.CiudadExpedicion,
                    CodCargo = model.CargoCode,
                    CodSucursal = model.BranchCode,
                    Celular = model.Celular,
                    Direccion = model.Direccion,
                    Correo = model.Correo,
                    RH = model.BloodType,
                    Genero = model.Genero,
                    OtroGenero = model.OtroGenero,
                    FecVinculacion = model.FechaVinculacion,
                    FecRetiro = model.FechaRetiro,
                    IndicadorCatalogo = model.IndicadorCatalogo,
                    IngresoRepublica = model.IngresoRepublica,
                    IngresoAeropuerto = model.IngresoAeropuerto,
                    FotoUrl = photoPath,
                    FirmaUrl = signaturePath,
                    EmpleadoEstado = (EstadoEmpleado)model.EmployeeStatus
                };

                _context.AdmEmpleados.Add(newEmployee);
                await _context.SaveChangesAsync();

                _logger.Information("Employee {EmployeeId} created by user {UserId}.", newEmployee.CodCedula, currentUserId);
                return ServiceResult.SuccessResult("Empleado creado exitosamente.", newEmployee.CodCedula);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error creating employee with cédula {CodCedula}.", model.CodCedula);
                return ServiceResult.FailureResult("Ocurrió un error interno al crear el empleado.");
            }
        }

        public async Task<ServiceResult> UpdateEmployeeAsync(EmpleadoViewModel model, string currentUserId)
        {
            var employee = await _context.AdmEmpleados.FindAsync(model.CodCedula);
            if (employee == null)
            {
                return ServiceResult.FailureResult("Empleado no encontrado para actualizar.");
            }

            try
            {
                var (photoPath, signaturePath) = await ProcessEmployeeFilesAsync(model);

                employee.PrimerApellido = FormatText(model.FirstLastName);
                employee.SegundoApellido = FormatText(model.SecondLastName);
                employee.PrimerNombre = FormatText(model.FirstName);
                employee.SegundoNombre = FormatText(model.MiddleName);
                employee.NombreCompleto = string.Join(" ", new[] { FormatText(model.FirstName), FormatText(model.MiddleName), FormatText(model.FirstLastName), FormatText(model.SecondLastName) }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
                employee.CodCargo = model.CargoCode;
                employee.CodSucursal = model.BranchCode;
                employee.FechaNacimiento = model.FechaNacimiento;
                employee.FechaExpedicion = model.FechaExpedicion;
                employee.CiudadExpedicion = model.CiudadExpedicion;
                employee.Celular = model.Celular;
                employee.EmpleadoEstado = (EstadoEmpleado)model.EmployeeStatus;
                if (photoPath != null) employee.FotoUrl = photoPath;
                if (signaturePath != null) employee.FirmaUrl = signaturePath;
                // ... resto de las propiedades

                await _context.SaveChangesAsync();

                _logger.Information("Employee {EmployeeId} updated by user {UserId}.", employee.CodCedula, currentUserId);
                return ServiceResult.SuccessResult("Empleado actualizado exitosamente.", employee.CodCedula);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error updating employee with cédula {CodCedula}.", model.CodCedula);
                return ServiceResult.FailureResult("Ocurrió un error interno al actualizar el empleado.");
            }
        }

        public async Task<Stream?> GetEmployeeImageStreamAsync(string relativePath) 
        {
            string fullPath = Path.Combine(REPO_BASE_PATH, relativePath);
            if (!File.Exists(fullPath))
            {
                _logger.Warning("Image file not found at path: {FullPath}", fullPath);
                return null;
            }

            try
            {
                return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error al abrir el stream de la imagen: {FullPath}", fullPath);
                return null;
            }
        }

        // --- MÉTODOS PRIVADOS AUXILIARES ---

        private async Task<List<int>> GetUserPermittedBranchIdsAsync(string userId)
        {
            return await _context.UserClaims
                .Where(uc => uc.UserId == userId && uc.ClaimType == "SucursalId")
                .Select(uc => int.Parse(uc.ClaimValue))
                .ToListAsync();
        }

        private async Task<(string? photoPath, string? signaturePath)> ProcessEmployeeFilesAsync(EmpleadoViewModel model)
        {
            string photosPath = Path.Combine(REPO_BASE_PATH, "Fotos");
            string signaturesPath = Path.Combine(REPO_BASE_PATH, "Firmas");
            Directory.CreateDirectory(photosPath); // Asegurar que el directorio exista
            Directory.CreateDirectory(signaturesPath); // Asegurar que el directorio exista

            string? relativePhotoPath = model.PhotoPath;
            string? relativeSignaturePath = model.SignaturePath;

            // Procesar foto con recorte/redimensionamiento
            if (model.PhotoFile != null && model.PhotoFile.Length > 0)
            {
                string photoFileName = $"{model.CodCedula}P{Path.GetExtension(model.PhotoFile.FileName)}";
                string fullPhotoPath = Path.Combine(photosPath, photoFileName);

                try
                {
                    using (var imageStream = model.PhotoFile.OpenReadStream())
                    {
                        using (var originalImage = Image.FromStream(imageStream))
                        {
                            // Lógica para el recorte cuadrado
                            int size = Math.Min(originalImage.Width, originalImage.Height);
                            int startX = (originalImage.Width - size) / 2;
                            int startY = (originalImage.Height - size) / 2;

                            using (var croppedImage = new Bitmap(size, size))
                            using (var graphics = Graphics.FromImage(croppedImage))
                            {
                                graphics.CompositingQuality = CompositingQuality.HighQuality;
                                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                graphics.SmoothingMode = SmoothingMode.HighQuality;

                                graphics.DrawImage(originalImage, new Rectangle(0, 0, size, size),
                                                   new Rectangle(startX, startY, size, size),
                                                   GraphicsUnit.Pixel);

                                // Guardar la imagen recortada. Usa Jpeg o Png según la necesidad, o determina desde el original.
                                // Para simplificar, guardemos como JPEG.
                                croppedImage.Save(fullPhotoPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                            }
                        }
                    }
                    relativePhotoPath = Path.Combine("Fotos", photoFileName).Replace('\\', '/');
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error al procesar y guardar la foto del empleado {CodCedula}.", model.CodCedula);
                    relativePhotoPath = model.PhotoPath;
                }
            }
            if (model.SignatureFile != null && model.SignatureFile.Length > 0)
            {
                string signatureFileName = $"{model.CodCedula}F{Path.GetExtension(model.SignatureFile.FileName)}";
                string fullSignaturePath = Path.Combine(signaturesPath, signatureFileName);
                try
                {
                    using (var stream = new FileStream(fullSignaturePath, FileMode.Create))
                    {
                        await model.SignatureFile.CopyToAsync(stream);
                    }
                    relativeSignaturePath = Path.Combine("Firmas", signatureFileName).Replace('\\', '/');
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error al procesar y guardar la firma del empleado {CodCedula}.", model.CodCedula);
                    relativeSignaturePath = model.SignaturePath;
                }
            }

            return (relativePhotoPath, relativeSignaturePath);
        }

        public async Task<ServiceResult> ChangeEmployeeStatusAsync(int employeeId, int newStatus, string reasonForChange, string currentUserId)
        {
            var employee = await _context.AdmEmpleados.FindAsync(employeeId);
            if (employee == null)
            {
                _logger.Warning("Employee with ID {EmployeeId} not found for status change.", employeeId);
                return ServiceResult.FailureResult("Empleado no encontrado.");
            }

            // Validar que el nuevo estado sea un valor válido de tu enum EstadoEmpleado
            if (!Enum.IsDefined(typeof(EstadoEmpleado), newStatus))
            {
                return ServiceResult.FailureResult("Estado no válido proporcionado.");
            }

            try
            {
                var oldStatus = employee.EmpleadoEstado;
                employee.EmpleadoEstado = (EstadoEmpleado)newStatus; // Castear al enum

                // Opcional: Podrías querer guardar un historial de cambios de estado
                // var history = new HistorialCambioEstado
                // {
                //     CodCedula = employeeId,
                //     EstadoAnterior = oldStatus.ToString(),
                //     EstadoNuevo = employee.EmpleadoEstado.ToString(),
                //     RazonCambio = reasonForChange,
                //     FechaCambio = DateTime.Now,
                //     UsuarioId = currentUserId
                // };
                // _context.HistorialCambioEstado.Add(history);

                await _context.SaveChangesAsync();

                _logger.Information("Employee {EmployeeId} status changed from {OldStatus} to {NewStatus} by user {UserId}. Reason: {Reason}",
                    employeeId, oldStatus, employee.EmpleadoEstado, currentUserId, reasonForChange);

                return ServiceResult.SuccessResult($"Estado del empleado cambiado exitosamente a {employee.EmpleadoEstado}.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error changing status for employee {EmployeeId} to {NewStatus}.", employeeId, newStatus);
                return ServiceResult.FailureResult("Ocurrió un error al cambiar el estado del empleado.");
            }
        }

        private string? FormatText(string? text)
        {
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim().ToUpperInvariant();
        }
    }
}