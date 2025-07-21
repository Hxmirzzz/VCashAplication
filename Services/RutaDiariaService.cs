using Microsoft.EntityFrameworkCore;
using VCashApp.Data;
using VCashApp.Models.Entities;
using VCashApp.Services.DTOs;
using VCashApp.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;

namespace VCashApp.Services
{
    public class RutaDiariaService : IRutaDiariaService
    {
        private readonly AppDbContext _context;

        public RutaDiariaService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TdvRutaDiaria> CrearRutaDiariaInicialAsync(TdvRutaDiaria nuevaRuta)
        {
            var resultList = await _context.Database
                                            .SqlQueryRaw<string>("EXEC GenerarNuevoRutaId @prefijo = 'R';")
                                            .AsNoTracking()
                                            .ToListAsync();
            var nuevoId = resultList.FirstOrDefault();

            if (string.IsNullOrEmpty(nuevoId))
            {
                Log.Error("Error al generar un nuevo ID para la ruta diaria desde el SP. No se pudo crear la ruta.");
                throw new InvalidOperationException("No se pudo generar un ID único para la nueva ruta.");
            }

            nuevaRuta.Id = nuevoId; // Asignar el ID generado
            nuevaRuta.FechaPlaneacion = DateOnly.FromDateTime(DateTime.Now);
            nuevaRuta.HoraPlaneacion = TimeOnly.FromDateTime(DateTime.Now);
            nuevaRuta.Estado = (int)EstadoRuta.GENERADO;
            nuevaRuta.CodVehiculo = null;
            nuevaRuta.CedulaJT = null;
            nuevaRuta.NombreJT = null;
            nuevaRuta.CodCargoJT = null;
            nuevaRuta.FechaIngresoJT = null;
            nuevaRuta.HoraIngresoJT = null;
            nuevaRuta.FechaSalidaJT = null;
            nuevaRuta.HoraSalidaJT = null;
            nuevaRuta.CedulaConductor = null;
            nuevaRuta.NombreConductor = null;
            nuevaRuta.CodCargoConductor = null;
            nuevaRuta.CedulaTripulante = null;
            nuevaRuta.NombreTripulante = null;
            nuevaRuta.CodCargoTripulante = null;
            nuevaRuta.FechaCargue = null;
            nuevaRuta.HoraCargue = null;
            nuevaRuta.CantBolsaBilleteEntrega = null;
            nuevaRuta.CantBolsaMonedaEntrega = null;
            nuevaRuta.UsuarioCEFCargue = null;
            nuevaRuta.FechaDescargue = null;
            nuevaRuta.HoraDescargue = null;
            nuevaRuta.CantBolsaBilleteRecibe = null;
            nuevaRuta.CantBolsaMonedaRecibe = null;
            nuevaRuta.UsuarioCEFDescargue = null;
            nuevaRuta.KmInicial = null;
            nuevaRuta.FechaSalidaRuta = null;
            nuevaRuta.HoraSalidaRuta = null;
            nuevaRuta.UsuarioSupervisorApertura = null;
            nuevaRuta.KmFinal = null;
            nuevaRuta.FechaEntradaRuta = null;
            nuevaRuta.HoraEntradaRuta = null;
            nuevaRuta.UsuarioSupervisorCierre = null;


            _context.TdvRutasDiarias.Add(nuevaRuta);
            await _context.SaveChangesAsync();
            return nuevaRuta;
        }

        public async Task<GeneracionRutasDiariasResult> GenerarRutasDiariasInicialesPorSucursalAsync(int codSucursal, DateOnly fechaEjecucion, string usuarioPlaneacionId, string usuarioPlaneacionNombre)
        {
            int rutasCreadas = 0;
            int rutasOmitidas = 0;
            DateOnly fechaActual = DateOnly.FromDateTime(DateTime.Now);
            TimeOnly horaActual = TimeOnly.FromDateTime(DateTime.Now);

            var rutasMaestras = await _context.AdmRutas
                                                .Where(rm => rm.CodSucursal == codSucursal && rm.EstadoRuta == true)
                                                .ToListAsync();

            if (!rutasMaestras.Any())
            {
                Log.Warning("No se encontraron rutas maestras activas para la sucursal {CodSucursal} para generar rutas diarias.", codSucursal);
                return new GeneracionRutasDiariasResult { RutasCreadas = 0, RutasOmitidas = 0, Mensaje = $"No hay rutas maestras activas para la sucursal {codSucursal}." };
            }

            var existingRutasDiariasForBranchAndDate = await _context.TdvRutasDiarias
                .Where(r => r.CodSucursal == codSucursal && r.FechaEjecucion == fechaEjecucion)
                .Select(r => r.CodRutaSuc) // Solo necesitamos el CodRutaSuc para comparar
                .ToListAsync();

            foreach (var rutaMaster in rutasMaestras)
            {
                // **Lógica de unicidad interna:**
                // Solo genera la ruta diaria si NO existe ya una para esta ruta maestra, sucursal y fecha.
                if (existingRutasDiariasForBranchAndDate.Contains(rutaMaster.CodRutaSuc))
                {
                    Log.Information("Ruta diaria para RutaMaestra {CodRutaMaster} en Sucursal {CodSucursal} para Fecha {Fecha} ya existe. Omitiendo.", rutaMaster.CodRutaSuc, codSucursal, fechaEjecucion);
                    rutasOmitidas++;
                    continue; // Pasa a la siguiente ruta maestra
                }

                var resultList = await _context.Database
                                                .SqlQueryRaw<string>("EXEC GenerarNuevoRutaId @prefijo = 'R';")
                                                .AsNoTracking()
                                                .ToListAsync();
                var nuevoId = resultList.FirstOrDefault();

                if (string.IsNullOrEmpty(nuevoId))
                {
                    Log.Error("Error al generar un nuevo ID para la ruta diaria desde el SP. No se pudo crear la ruta para RutaMaestra.CodRuta={CodRutaMaster} en sucursal {CodSucursal}.", rutaMaster.CodRuta, codSucursal);
                    // Decidir si omitir o lanzar excepción. Para generación masiva, omitir y loguear es mejor.
                    rutasOmitidas++; // Considerar como omitida porque no se pudo crear
                    continue;
                }

                var nuevaRutaDiaria = new TdvRutaDiaria
                {
                    Id = nuevoId,
                    CodSucursal = codSucursal,
                    NombreSucursal = (await _context.AdmSucursales.FirstOrDefaultAsync(s => s.CodSucursal == codSucursal))?.NombreSucursal ?? "Desconocida",
                    CodRutaSuc = rutaMaster.CodRutaSuc,
                    NombreRuta = rutaMaster.NombreRuta,
                    FechaPlaneacion = fechaActual,
                    HoraPlaneacion = horaActual,
                    FechaEjecucion = fechaEjecucion,
                    UsuarioPlaneacion = usuarioPlaneacionId,
                    Estado = (int)EstadoRuta.GENERADO,
                    TipoRuta = rutaMaster.TipoRuta,
                    TipoVehiculo = rutaMaster.TipoVehiculo,
                    CodVehiculo = null,
                    CedulaJT = null,
                    NombreJT = null,
                    CodCargoJT = null,
                    FechaIngresoJT = null,
                    HoraIngresoJT = null,
                    FechaSalidaJT = null,
                    HoraSalidaJT = null,
                    CedulaConductor = null,
                    NombreConductor = null,
                    CodCargoConductor = null,
                    CedulaTripulante = null,
                    NombreTripulante = null,
                    CodCargoTripulante = null,
                    FechaCargue = null,
                    HoraCargue = null,
                    CantBolsaBilleteEntrega = null,
                    CantBolsaMonedaEntrega = null,
                    UsuarioCEFCargue = null,
                    FechaDescargue = null,
                    HoraDescargue = null,
                    CantBolsaBilleteRecibe = null,
                    CantBolsaMonedaRecibe = null,
                    UsuarioCEFDescargue = null,
                    KmInicial = null,
                    FechaSalidaRuta = null,
                    HoraSalidaRuta = null,
                    UsuarioSupervisorApertura = null,
                    KmFinal = null,
                    FechaEntradaRuta = null,
                    HoraEntradaRuta = null,
                    UsuarioSupervisorCierre = null
                };

                _context.TdvRutasDiarias.Add(nuevaRutaDiaria);
                rutasCreadas++;
            }

            await _context.SaveChangesAsync(); // Guarda todos los cambios en un solo batch al final del bucle

            // Retorna el resultado completo, incluyendo rutas creadas y omitidas
            return new GeneracionRutasDiariasResult
            {
                RutasCreadas = rutasCreadas,
                RutasOmitidas = rutasOmitidas,
                ExitoParcial = rutasCreadas > 0 && rutasOmitidas > 0,
                Mensaje = "" // El mensaje principal se construirá en el controlador para mayor flexibilidad
            };
        }

        public async Task<List<TdvRutaDiaria>> ObtenerRutasGeneradasParaPlaneacionAsync(DateOnly fechaEjecucion, int codSucursal)
        {
            return await _context.TdvRutasDiarias
                                 .Where(r => r.Estado == 1 && r.FechaEjecucion == fechaEjecucion && r.CodSucursal == codSucursal)
                                 .ToListAsync();
        }

        public async Task<TdvRutaDiaria> ObtenerRutaDiariaPorIdAsync(string id)
        {
            return await _context.TdvRutasDiarias.FindAsync(id);
        }

        public async Task<bool> ActualizarRutaDiariaPlaneadorAsync(TdvRutaDiaria rutaActualizada)
        {
            var rutaExistente = await _context.TdvRutasDiarias.FindAsync(rutaActualizada.Id);

            if (rutaExistente == null)
            {
                return false;
            }

            if (rutaExistente.Estado != 1)
            {
                throw new InvalidOperationException("La ruta no se encuentra en el estado GENERADO y no puede ser actualizada por el planeador.");
            }

            if (!string.IsNullOrEmpty(rutaActualizada.CodVehiculo))
            {
                var vehicleassigned = await _context.TdvRutasDiarias
                    .AnyAsync(r => r.Id != rutaActualizada.Id &&
                                   r.CodVehiculo == rutaActualizada.CodVehiculo &&
                                   r.FechaEjecucion == rutaExistente.FechaEjecucion &&
                                   (r.Estado == (int)EstadoRuta.GENERADO ||
                                    r.Estado == (int)EstadoRuta.PLANEADO ||
                                    r.Estado == (int)EstadoRuta.CARGUE_REGISTRADO ||
                                    r.Estado == (int)EstadoRuta.SALIDA_REGISTRADA ||
                                    r.Estado == (int)EstadoRuta.DESCARGUE_REGISTRADO));
                if (vehicleassigned)
                {
                    throw new InvalidOperationException($"El vehículo {rutaActualizada.CodVehiculo} ya está asignado a otra ruta para la fecha {rutaExistente.FechaEjecucion.ToShortDateString()}.");
                }
            }

            rutaExistente.CodVehiculo = rutaActualizada.CodVehiculo;
            rutaExistente.CedulaJT = rutaActualizada.CedulaJT;
            rutaExistente.NombreJT = rutaActualizada.NombreJT;
            rutaExistente.CodCargoJT = rutaActualizada.CodCargoJT;

            if (rutaExistente.TipoVehiculo == "M")
            {
                rutaExistente.CedulaJT = null;
                rutaExistente.NombreJT = null;
                rutaExistente.CodCargoJT = null;
            }

            rutaExistente.CedulaConductor = rutaActualizada.CedulaConductor;
            rutaExistente.NombreConductor = rutaActualizada.NombreConductor;
            rutaExistente.CodCargoConductor = rutaActualizada.CodCargoConductor;
            rutaExistente.CedulaTripulante = rutaActualizada.CedulaTripulante;
            rutaExistente.NombreTripulante = rutaActualizada.NombreTripulante;
            rutaExistente.CodCargoTripulante = rutaActualizada.CodCargoTripulante;

            rutaExistente.FechaIngresoJT = null;
            rutaExistente.HoraIngresoJT = null;

            rutaExistente.Estado = 2;

            try
            {
                _context.TdvRutasDiarias.Update(rutaExistente);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al actualizar ruta diaria: {ex.Message}");
                return false;
            }
        }

        // Implementación del método para el CEF para registrar el cargue de dinero
        public async Task<bool> RegistrarCargueCEFAsync(string rutaId, DateOnly fechaCargue, TimeOnly horaCargue, int cantBolsaBilleteEntrega, int cantBolsaMonedaEntrega,
            int cantPlanillaEntrega, string usuarioCefCargue)
        {
            var rutaExistente = await _context.TdvRutasDiarias.FindAsync(rutaId);

            if (rutaExistente == null)
            {
                return false;
            }

            if (rutaExistente.Estado != (int)EstadoRuta.PLANEADO) //
            {
                throw new InvalidOperationException($"La ruta no se encuentra en estado PLANEADO ({rutaExistente.Estado}) para registrar el cargue de CEF.");
            }

            rutaExistente.FechaCargue = fechaCargue;
            rutaExistente.HoraCargue = horaCargue;
            rutaExistente.CantBolsaBilleteEntrega = cantBolsaBilleteEntrega;
            rutaExistente.CantBolsaMonedaEntrega = cantBolsaMonedaEntrega;
            rutaExistente.CantPlanillaEntrega = cantPlanillaEntrega;
            rutaExistente.UsuarioCEFCargue = usuarioCefCargue;
            rutaExistente.Estado = (int)EstadoRuta.CARGUE_REGISTRADO;

            try
            {
                _context.TdvRutasDiarias.Update(rutaExistente);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al registrar cargue de CEF en el servicio: {ex.Message}");
                throw;
            }
        }

        // Implementación del método para el CEF para registrar el descargue de dinero
        public async Task<bool> RegistrarDescargueCEFAsync(string rutaId, DateOnly fechaDescargue, TimeOnly horaDescargue, int cantBolsaBilleteRecibe, 
            int cantBolsaMonedaRecibe, int cantPlanillaRecibe, string usuarioCefDescargue)
        {
            var rutaExistente = await _context.TdvRutasDiarias.FindAsync(rutaId);

            if (rutaExistente == null) return false;

            if (rutaExistente.Estado != (int)EstadoRuta.SALIDA_REGISTRADA)
            {
                throw new InvalidOperationException($"La ruta no se encuentra en estado SALIDA REGISTRADA ({rutaExistente.Estado}) para registrar el descargue de CEF.");
            }

            if (!rutaExistente.FechaSalidaRuta.HasValue || !rutaExistente.HoraSalidaRuta.HasValue)
            {
                throw new InvalidOperationException("No se puede registrar el descargue de efectivo. El Supervisor debe registrar primero la salida del vehículo.");
            }

            rutaExistente.FechaDescargue = fechaDescargue;
            rutaExistente.HoraDescargue = horaDescargue;
            rutaExistente.CantBolsaBilleteRecibe = cantBolsaBilleteRecibe;
            rutaExistente.CantBolsaMonedaRecibe = cantBolsaMonedaRecibe;
            rutaExistente.CantPlanillaRecibe = cantPlanillaRecibe;
            rutaExistente.UsuarioCEFDescargue = usuarioCefDescargue;
            rutaExistente.Estado = (int)EstadoRuta.DESCARGUE_REGISTRADO;

            try
            {
                _context.TdvRutasDiarias.Update(rutaExistente);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al registrar descargue de CEF en el servicio: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> RegistrarSalidaVehiculoAsync(string rutaId, decimal kmInicial, DateOnly? fechaSalidaRuta, TimeOnly? horaSalidaRuta, string usuarioSupervisorApertura)
        {
            var rutaExistente = await _context.TdvRutasDiarias.FindAsync(rutaId);
            if (rutaExistente == null) return false;
            if (rutaExistente.Estado != (int)EstadoRuta.CARGUE_REGISTRADO)
            {
                throw new InvalidOperationException($"La ruta no se encuentra en estado CARGUE REGISTRADO ({rutaExistente.Estado}) para registrar la salida del vehículo.");
            }

            if (kmInicial <= 0) { throw new InvalidOperationException("El Kilometraje Inicial debe ser un valor positivo."); }
            if (!fechaSalidaRuta.HasValue) { throw new InvalidOperationException("La fecha de salida es requerida."); }
            if (!horaSalidaRuta.HasValue) { throw new InvalidOperationException("La hora de salida es requerida."); }
            if (fechaSalidaRuta.Value > DateOnly.FromDateTime(DateTime.Today)) // <-- Acceder a .Value
            { throw new InvalidOperationException("La fecha de salida de la ruta no puede ser una fecha futura."); }


            rutaExistente.KmInicial = kmInicial;
            rutaExistente.FechaSalidaRuta = fechaSalidaRuta.Value;
            rutaExistente.HoraSalidaRuta = horaSalidaRuta.Value;
            rutaExistente.Estado = (int)EstadoRuta.SALIDA_REGISTRADA;
            rutaExistente.UsuarioSupervisorApertura = usuarioSupervisorApertura;

            try { _context.TdvRutasDiarias.Update(rutaExistente); await _context.SaveChangesAsync(); return true; }
            catch (DbUpdateConcurrencyException) { return false; }
            catch (Exception ex) { Console.WriteLine($"Error al registrar salida del vehículo: {ex.Message}"); throw; }
        }

        public async Task<bool> RegistrarEntradaVehiculoAsync(string rutaId, decimal kmFinal, DateOnly? fechaEntradaRuta, TimeOnly? horaEntradaRuta, string usuarioSupervisorCierre)
        {
            var rutaExistente = await _context.TdvRutasDiarias.FindAsync(rutaId);
            if (rutaExistente == null) return false;
            if (rutaExistente.Estado != (int)EstadoRuta.DESCARGUE_REGISTRADO)
            {
                throw new InvalidOperationException($"La ruta no se encuentra en estado DESCARGUE REGISTRADO ({rutaExistente.Estado}) para registrar la entrada del vehículo y cerrarla.");
            }
            if (!rutaExistente.FechaSalidaRuta.HasValue || !rutaExistente.HoraSalidaRuta.HasValue)
            {
                throw new InvalidOperationException("No se puede registrar la entrada del vehículo. El Supervisor debe registrar primero la salida del vehículo.");
            }

            if (kmFinal <= 0) { throw new InvalidOperationException("El Kilometraje Final es requerido y debe ser un valor positivo."); }
            if (rutaExistente.KmInicial.HasValue && kmFinal < rutaExistente.KmInicial.Value)
            { throw new InvalidOperationException("El Kilometraje Final no puede ser menor que el Kilometraje Inicial."); }
            if (!fechaEntradaRuta.HasValue) { throw new InvalidOperationException("La fecha de entrada es requerida."); }
            if (!horaEntradaRuta.HasValue) { throw new InvalidOperationException("La hora de entrada es requerida."); }
            if (fechaEntradaRuta.Value > DateOnly.FromDateTime(DateTime.Today))
            { throw new InvalidOperationException("La fecha de entrada de la ruta no puede ser una fecha futura."); }

            var salidaDateTime = new DateTime(rutaExistente.FechaSalidaRuta.Value.Year, rutaExistente.FechaSalidaRuta.Value.Month, rutaExistente.FechaSalidaRuta.Value.Day,
                                            rutaExistente.HoraSalidaRuta.Value.Hour, rutaExistente.HoraSalidaRuta.Value.Minute, 0);
            var entradaDateTime = new DateTime(fechaEntradaRuta.Value.Year, fechaEntradaRuta.Value.Month, fechaEntradaRuta.Value.Day,
                                               horaEntradaRuta.Value.Hour, horaEntradaRuta.Value.Minute, 0);

            if (entradaDateTime < salidaDateTime)
            {
                throw new InvalidOperationException("La fecha y hora de entrada no pueden ser anteriores a la fecha y hora de salida.");
            }

            rutaExistente.KmFinal = kmFinal;
            rutaExistente.FechaEntradaRuta = fechaEntradaRuta.Value;
            rutaExistente.HoraEntradaRuta = horaEntradaRuta.Value;
            rutaExistente.UsuarioSupervisorCierre = usuarioSupervisorCierre;
            rutaExistente.Estado = (int)EstadoRuta.CERRADO;

            try { _context.TdvRutasDiarias.Update(rutaExistente); await _context.SaveChangesAsync(); return true; }
            catch (DbUpdateConcurrencyException) { return false; }
            catch (Exception ex) { Console.WriteLine($"Error al registrar entrada del vehículo: {ex.Message}"); throw; }
        }
    }
}