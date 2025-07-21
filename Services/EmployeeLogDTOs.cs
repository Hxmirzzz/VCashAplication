using System;
using System.Collections.Generic;

namespace VCashApp.Services
{

    public class EmployeeLogStateDto
    {
        public string Status { get; set; }
        public string? EntryDate { get; set; }
        public string? EntryTime { get; set; }
        public string? ExitDate { get; set; }
        public string? ExitTime { get; set; }
        public string CurrentDate { get; set; }
        public string CurrentTime { get; set; }
        public string? UnitType { get; set; }
        public string? ErrorMessage { get; set; }
        public double? HoursWorked { get; set; }
        public string? ValidationType { get; set; }
        public bool NeedsConfirmation { get; set; }
        public int? OpenLogId { get; set; }
    }

    public class EmployeeLogDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string LastName { get; set; }
        public string? SecondLastName { get; set; }
        public int CargoId { get; set; }
        public string CargoName { get; set; }
        public string UnitId { get; set; }
        public string UnitName { get; set; }
        public int BranchId { get; set; }
        public string BranchName { get; set; }
        public string EntryDate { get; set; }
        public string EntryTime { get; set; }
        public string? ExitDate { get; set; }
        public string? ExitTime { get; set; }
        public bool IsEntryRecorded { get; set; }
        public bool IsExitRecorded { get; set; }
        public string? ConfirmedValidation { get; set; }
    }

    public class ServiceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public bool NeedsConfirmation { get; set; } = false;
        public string? ValidationType { get; set; }
        public double? HoursWorked { get; set; }
        public int? Id { get; set; }

        public static ServiceResult SuccessResult(string message, int? id = null) => new ServiceResult { Success = true, Message = message, Id = id };
        public static ServiceResult FailureResult(string message) => new ServiceResult { Success = false, Message = message };
        public static ServiceResult ConfirmationRequired(string message, string validationType, double hours) =>
            new ServiceResult { Success = false, Message = message, NeedsConfirmation = true, ValidationType = validationType, HoursWorked = hours };
    }
}