using Keyloop.Scheduler.Domain.Enums;

namespace Keyloop.Scheduler.Core.DTOs;

public record AppointmentSummaryDto(
    string AppointmentId,
    string DealershipId,
    string DealershipName,
    string ServiceTypeId,
    string ServiceName,
    string VehicleVin,
    string VehicleDetails,
    string CustomerId,
    string CustomerName,
    string TechnicianId,
    string TechnicianName,
    string ServiceBayId,
    string ServiceBayName,
    DateTime StartTime,
    DateTime EndTime,
    AppointmentStatus Status,
    string Notes,
    DateTime CreatedAt
);
