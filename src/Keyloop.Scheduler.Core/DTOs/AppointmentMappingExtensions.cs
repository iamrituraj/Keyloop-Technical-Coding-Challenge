using Keyloop.Scheduler.Domain.Entities;

namespace Keyloop.Scheduler.Core.DTOs;

public static class AppointmentMappingExtensions
{
    public static AppointmentSummaryDto ToSummaryDto(
        this Appointment apt,
        Dealership? dealership = null,
        ServiceType? serviceType = null,
        Vehicle? vehicle = null,
        Customer? customer = null,
        Technician? technician = null,
        ServiceBay? serviceBay = null)
    {
        var resolvedDealership = dealership ?? apt.Dealership;
        var resolvedServiceType = serviceType ?? apt.ServiceType;
        var resolvedVehicle = vehicle ?? apt.Vehicle;
        var resolvedCustomer = customer ?? apt.Customer;
        var resolvedTechnician = technician ?? apt.Technician;
        var resolvedBay = serviceBay ?? apt.ServiceBay;

        var vehicleDetails = resolvedVehicle != null
            ? $"{resolvedVehicle.Year} {resolvedVehicle.Make} {resolvedVehicle.Model}"
            : apt.VehicleVin;

        return new AppointmentSummaryDto(
            apt.Id,
            apt.DealershipId,
            resolvedDealership?.Name ?? "Dealership",
            apt.ServiceTypeId,
            resolvedServiceType?.Name ?? "Service",
            apt.VehicleVin,
            vehicleDetails,
            apt.CustomerId,
            resolvedCustomer?.Name ?? "Customer",
            apt.TechnicianId,
            resolvedTechnician?.Name ?? "Technician",
            apt.ServiceBayId,
            resolvedBay?.Name ?? "Bay",
            apt.StartTime,
            apt.EndTime,
            apt.Status,
            apt.Notes,
            apt.CreatedAt
        );
    }
}
