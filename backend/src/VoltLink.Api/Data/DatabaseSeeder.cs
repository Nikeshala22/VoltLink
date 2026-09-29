// -----------------------------------------------------------------------------
// File        : DatabaseSeeder.cs
// Project     : VoltLink.Api - Smart Solar Microgrid Trading System
// Module      : Data
// Description : Seeds essential bootstrap data into an empty database: initial
//               Backoffice administrator, default microgrid nodes (stations),
//               and upcoming energy booking slots for prosumer reservations.
// Author      : <IT Number - Member Name>
// Created     : 2026-09-03
// -----------------------------------------------------------------------------

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using VoltLink.Api.Models;
using VoltLink.Api.Repositories;
using VoltLink.Api.Security;

namespace VoltLink.Api.Data;


public class DatabaseSeeder
{
    private readonly IUserRepository _users;
    private readonly MongoContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseSeeder> _logger;


    public DatabaseSeeder(
        IUserRepository users,
        MongoContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<DatabaseSeeder> logger)
    {
        _users = users;
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Coordinates seeding of initial administrator, solar microgrid hubs and slots.
    /// Safe to run on every start-up because it checks for existing records first.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Inline comment: Seed the default administrator account if none exists.
        await SeedAdminAsync(cancellationToken);

        // Inline comment: Seed default solar microgrid stations and booking slots if none exist.
        await SeedStationsAndSlotsAsync(cancellationToken);
    }

    /// <summary>
    /// Creates the bootstrap back-office account if, and only if, no
    /// Backoffice user exists yet. Running this on every start-up is therefore
    /// safe and never overwrites a changed password.
    /// </summary>
    private async Task SeedAdminAsync(CancellationToken cancellationToken)
    {
        // Inline comment: Check if an administrative staff member already exists.
        var existingAdmins = await _users.CountAsync(UserRoles.Backoffice, cancellationToken: cancellationToken);
        if (existingAdmins > 0)
        {
            _logger.LogInformation("Seed skipped: {Count} back-office account(s) already exist.", existingAdmins);
            return;
        }

        // Inline comment: Read credentials from configuration with fallback defaults.
        var email = _configuration["SeedAdmin:Email"] ?? "admin@voltlink.lk";
        var password = _configuration["SeedAdmin:Password"] ?? "Admin@123";
        var fullName = _configuration["SeedAdmin:FullName"] ?? "System Administrator";

        var now = DateTime.UtcNow;
        var admin = new User
        {
            Id = ObjectId.GenerateNewId().ToString(),
            FullName = fullName,
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(password),
            Role = UserRoles.Backoffice,
            IsActive = true,
            DeactivationRequested = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _users.InsertAsync(admin, cancellationToken);

        _logger.LogWarning(
            "Seeded initial back-office account '{Email}'. Change this password before deployment.",
            admin.Email);
    }

    /// <summary>
    /// Seeds initial microgrid hubs with GPS coordinates, battery storage slots,
    /// and generates active booking windows for the next 7 days.
    /// </summary>
    private async Task SeedStationsAndSlotsAsync(CancellationToken cancellationToken)
    {
        // Inline comment: Verify whether any stations have already been registered.
        var existingStationCount = await _context.SolarStations.CountDocumentsAsync(
            FilterDefinition<SolarStation>.Empty, cancellationToken: cancellationToken);

        if (existingStationCount > 0)
        {
            _logger.LogInformation("Stations already exist ({Count} nodes). Skipping station seed.", existingStationCount);
            return;
        }

        var now = DateTime.UtcNow;
        var stations = new List<SolarStation>
        {
            new SolarStation
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Code = "GRID-COL-001",
                Name = "Colombo Central Microgrid Hub",
                AddressLine = "45 Janadhipathi Mawatha, Colombo 01",
                City = "Colombo",
                Location = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
                    new GeoJson2DGeographicCoordinates(79.8612, 6.9271)),
                CapacityKwh = 350.0,
                TotalBatterySlots = 20,
                AvailableBatterySlots = 16,
                OperatingHours = new OperatingHours { OpenTime = "06:00", CloseTime = "22:00" },
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new SolarStation
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Code = "GRID-KDY-001",
                Name = "Kandy Hillside Microgrid Hub",
                AddressLine = "12 Dalada Veediya, Kandy",
                City = "Kandy",
                Location = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
                    new GeoJson2DGeographicCoordinates(80.6337, 7.2906)),
                CapacityKwh = 250.0,
                TotalBatterySlots = 15,
                AvailableBatterySlots = 12,
                OperatingHours = new OperatingHours { OpenTime = "06:00", CloseTime = "20:00" },
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new SolarStation
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Code = "GRID-GLE-001",
                Name = "Galle Coastal Microgrid Hub",
                AddressLine = "88 Main Street, Galle",
                City = "Galle",
                Location = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
                    new GeoJson2DGeographicCoordinates(80.2210, 6.0535)),
                CapacityKwh = 300.0,
                TotalBatterySlots = 18,
                AvailableBatterySlots = 15,
                OperatingHours = new OperatingHours { OpenTime = "06:00", CloseTime = "21:00" },
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        };

        // Inline comment: Insert all default microgrid nodes into MongoDB.
        await _context.SolarStations.InsertManyAsync(stations, cancellationToken: cancellationToken);
        _logger.LogInformation("Seeded {Count} initial microgrid nodes.", stations.Count);

        // Inline comment: Generate daily booking windows over the 7-day horizon for each station.
        var slots = new List<EnergyBookingSlot>();
        var baseDate = DateTime.UtcNow.Date;

        foreach (var station in stations)
        {
            for (var day = 0; day <= 6; day++)
            {
                var targetDate = baseDate.AddDays(day);
                int[] startHours = { 8, 10, 12, 14, 16 };

                foreach (var startHour in startHours)
                {
                    slots.Add(new EnergyBookingSlot
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        StationId = station.Id,
                        StartTimeUtc = DateTime.SpecifyKind(targetDate.AddHours(startHour), DateTimeKind.Utc),
                        EndTimeUtc = DateTime.SpecifyKind(targetDate.AddHours(startHour + 2), DateTimeKind.Utc),
                        Capacity = 5,
                        BookedCount = 0,
                        EnergyKwhPerSlot = 25.0,
                        IsActive = true,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now
                    });
                }
            }
        }

        // Inline comment: Insert energy booking slots into MongoDB.
        await _context.BookingSlots.InsertManyAsync(slots, cancellationToken: cancellationToken);
        _logger.LogInformation("Seeded {Count} bookable energy slots across the next 7 days.", slots.Count);
    }
}
