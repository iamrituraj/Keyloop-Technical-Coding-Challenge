using Keyloop.Scheduler.Domain.Entities;
using Keyloop.Scheduler.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Keyloop.Scheduler.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task InitializeAndSeedAsync(SchedulerDbContext context, CancellationToken ct = default)
    {
        await context.Database.EnsureCreatedAsync(ct);
        await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL;", ct);
        await context.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout = 5000;", ct);
        await context.Database.ExecuteSqlRawAsync("PRAGMA synchronous = NORMAL;", ct);
        await SeedAsync(context);
    }

    public static async Task SeedAsync(SchedulerDbContext context)
    {
        if (await context.Dealerships.AnyAsync()) return;

        // Dealerships across Indian Automotive Technology Hubs
        var dealership = new Dealership
        {
            Id = "DLR-001",
            Name = "Keyloop Prime Auto — Hyderabad Tech Hub",
            Address = "Plot 12, HITEC City, Madhapur, Hyderabad, Telangana 500081, India",
            OpeningTime = "09:00",
            ClosingTime = "19:00",
            SlotIntervalMinutes = 30,
            Timezone = "Asia/Kolkata"
        };

        var bay1 = new ServiceBay { Id = "BAY-01", DealershipId = dealership.Id, Name = "Bay 1 - Express Service Lift", BayType = BayType.StandardLift };
        var bay2 = new ServiceBay { Id = "BAY-02-EV", DealershipId = dealership.Id, Name = "Bay 2 - High Voltage EV Safety Bay (ARAI Certified)", BayType = BayType.EvCertified };
        var bay3 = new ServiceBay { Id = "BAY-03", DealershipId = dealership.Id, Name = "Bay 3 - Heavy Duty SUV & Fleet Hoist", BayType = BayType.HeavyDuty };
        var bay4 = new ServiceBay { Id = "BAY-04-ALIGN", DealershipId = dealership.Id, Name = "Bay 4 - 3D Laser Alignment & ADAS Calibration Bay", BayType = BayType.WheelAlignment };

        var tech1 = new Technician { Id = "TECH-01", DealershipId = dealership.Id, Name = "Aarav Sharma", Email = "aarav.sharma@keyloop.in" };
        var tech2 = new Technician { Id = "TECH-02", DealershipId = dealership.Id, Name = "Priya Nair", Email = "priya.nair@keyloop.in" };
        var tech3 = new Technician { Id = "TECH-03", DealershipId = dealership.Id, Name = "Vikram Patel", Email = "vikram.patel@keyloop.in" };
        var tech4 = new Technician { Id = "TECH-04", DealershipId = dealership.Id, Name = "Rohan Verma", Email = "rohan.verma@keyloop.in" };

        var skills = new List<TechnicianSkill>
        {
            // Aarav: Master EV Specialist & Diagnostics
            new() { Id = "SK-01", TechnicianId = tech1.Id, SkillCode = SkillCode.EvPowertrain, CertificationLevel = "Level 4 High-Voltage Master Specialist" },
            new() { Id = "SK-02", TechnicianId = tech1.Id, SkillCode = SkillCode.EngineDiagnostics, CertificationLevel = "Senior Diagnostic Engineer" },
            new() { Id = "SK-03", TechnicianId = tech1.Id, SkillCode = SkillCode.GeneralMaintenance, CertificationLevel = "Master Technician" },

            // Priya: Senior Diagnostics & ADAS Engineer
            new() { Id = "SK-04", TechnicianId = tech2.Id, SkillCode = SkillCode.EngineDiagnostics, CertificationLevel = "Master Diagnostic Engineer" },
            new() { Id = "SK-05", TechnicianId = tech2.Id, SkillCode = SkillCode.AdasCalibration, CertificationLevel = "Certified ADAS Specialist" },
            new() { Id = "SK-06", TechnicianId = tech2.Id, SkillCode = SkillCode.BrakeAndChassis, CertificationLevel = "Senior Chassis Specialist" },

            // Vikram: Brake & Chassis Specialist
            new() { Id = "SK-07", TechnicianId = tech3.Id, SkillCode = SkillCode.BrakeAndChassis, CertificationLevel = "Senior Specialist" },
            new() { Id = "SK-08", TechnicianId = tech3.Id, SkillCode = SkillCode.GeneralMaintenance, CertificationLevel = "Senior Technician" },

            // Rohan: General Maintenance Technician
            new() { Id = "SK-09", TechnicianId = tech4.Id, SkillCode = SkillCode.GeneralMaintenance, CertificationLevel = "Certified Technician" }
        };

        var service1 = new ServiceType
        {
            Id = "SRV-EV-HEALTH",
            Name = "High-Voltage EV Battery Pack & Inverter Diagnostic",
            Description = "Full state-of-health analysis, BMS thermal management diagnostic, and 400V AIS-038 insulation resistance testing.",
            DurationMinutes = 90,
            RequiredBayType = BayType.EvCertified,
            RequiredSkills = new List<SkillCode> { SkillCode.EvPowertrain }
        };

        var service2 = new ServiceType
        {
            Id = "SRV-BRAKES",
            Name = "Performance Brake Disc & Hydraulic System Overhaul",
            Description = "Front and rear ventilated rotor replacement, ceramic pad fitting, ABS sensor calibration, and pressurized bleed.",
            DurationMinutes = 60,
            RequiredBayType = BayType.StandardLift,
            RequiredSkills = new List<SkillCode> { SkillCode.BrakeAndChassis }
        };

        var service3 = new ServiceType
        {
            Id = "SRV-ADAS",
            Name = "Windshield Camera & Front Radar ADAS Recalibration",
            Description = "Dynamic digital laser target calibration for Autonomous Emergency Braking (AEB) and Lane Keep Assist (LKA).",
            DurationMinutes = 60,
            RequiredBayType = BayType.WheelAlignment,
            RequiredSkills = new List<SkillCode> { SkillCode.AdasCalibration }
        };

        var service4 = new ServiceType
        {
            Id = "SRV-MAINT-MINOR",
            Name = "Express Periodic Maintenance & 50-Point Monsoon Check",
            Description = "Engine oil, synthetic filter replacement, electrical multi-point health check, and battery load testing.",
            DurationMinutes = 30,
            RequiredBayType = BayType.StandardLift,
            RequiredSkills = new List<SkillCode> { SkillCode.GeneralMaintenance }
        };

        var customer1 = new Customer
        {
            Id = "CUST-001",
            Name = "Rajesh Malhotra",
            Email = "rajesh.malhotra@example.com",
            Phone = "+91 98765 43210"
        };

        var vehicle1 = new Vehicle
        {
            Vin = "TS 09 EA 4521",
            CustomerId = customer1.Id,
            Make = "Tata",
            Model = "Nexon EV Max Long Range",
            Year = 2024,
            PowertrainType = PowertrainType.BatteryElectric
        };

        var vehicle2 = new Vehicle
        {
            Vin = "DL 01 AB 1234",
            CustomerId = customer1.Id,
            Make = "Mahindra",
            Model = "XUV700 AX7 (Petrol AT)",
            Year = 2024,
            PowertrainType = PowertrainType.InternalCombustion
        };

        var vehicleVin = new Vehicle
        {
            Vin = "MAT622154N1A98765",
            CustomerId = customer1.Id,
            Make = "Tata",
            Model = "Nexon EV Max Long Range",
            Year = 2024,
            PowertrainType = PowertrainType.BatteryElectric
        };

        context.Dealerships.Add(dealership);
        context.ServiceBays.AddRange(bay1, bay2, bay3, bay4);
        context.Technicians.AddRange(tech1, tech2, tech3, tech4);
        context.TechnicianSkills.AddRange(skills);
        context.ServiceTypes.AddRange(service1, service2, service3, service4);
        context.Customers.Add(customer1);
        context.Vehicles.AddRange(vehicle1, vehicle2, vehicleVin);

        await context.SaveChangesAsync();
    }
}
