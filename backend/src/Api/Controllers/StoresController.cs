using Microsoft.AspNetCore.Mvc;
using Spot.Api.DTOs;
using Spot.Domain.Common;
using Spot.Domain.Entities;
using Spot.Infrastructure.Repositories;

namespace Spot.Api.Controllers;

[ApiController]
[Route("api/v1/stores")]
public class StoresController : ControllerBase
{
    private readonly IStoreRepository _storeRepository;

    public StoresController(IStoreRepository storeRepository)
    {
        _storeRepository = storeRepository;
    }

    /// <summary>
    /// Hyper-local geofenced discovery: finds stores within a given radius using PostGIS spatial mechanics.
    /// </summary>
    /// <param name="latitude">User current latitude (WGS 84, -90 to 90)</param>
    /// <param name="longitude">User current longitude (WGS 84, -180 to 180)</param>
    /// <param name="radiusMeters">Proximity boundary in meters (default 1500m)</param>
    /// <param name="category">Optional category slug filter (e.g., 'groceries', 'coffee')</param>
    [HttpGet("nearby")]
    [ProducesResponseType(typeof(List<StoreDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<StoreDto>>> GetNearby(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] double radiusMeters = 1500,
        [FromQuery] string? category = null,
        CancellationToken ct = default)
    {
        if (latitude < -90 || latitude > 90)
        {
            return BadRequest("Latitude must be between -90 and 90 degrees.");
        }

        if (longitude < -180 || longitude > 180)
        {
            return BadRequest("Longitude must be between -180 and 180 degrees.");
        }

        if (radiusMeters <= 0 || radiusMeters > 100000)
        {
            return BadRequest("Radius must be between 1 and 100,000 meters.");
        }

        var results = await _storeRepository.GetNearbyStoresAsync(latitude, longitude, radiusMeters, category, ct);

        var dtos = results.Select(r => MapToDto(r.Store, r.DistanceMeters)).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StoreDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoreDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var store = await _storeRepository.GetByIdAsync(id, ct);
        if (store == null) return NotFound($"Store with ID {id} not found.");

        return Ok(MapToDto(store, null));
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<StoreDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<StoreDto>>> GetAll(CancellationToken ct = default)
    {
        var stores = await _storeRepository.GetAllAsync(ct);
        return Ok(stores.Select(s => MapToDto(s, null)).ToList());
    }

    private static StoreDto MapToDto(Store store, double? distance)
    {
        double avgRating = store.Reviews.Count != 0 
            ? Math.Round(store.Reviews.Average(r => r.RatingOverall), 1) 
            : 0.0;

        return new StoreDto(
            Id: store.Id,
            Name: store.Name,
            Category: new CategoryDto(
                store.Category.Id,
                store.Category.Name,
                store.Category.Slug,
                store.Category.Icon
            ),
            Latitude: store.Location.Y,
            Longitude: store.Location.X,
            RadiusMeters: store.RadiusMeters,
            Address: store.Address,
            IsPartner: store.IsPartner,
            DistanceMeters: distance,
            AverageRating: avgRating,
            ReviewCount: store.Reviews.Count
        );
    }
}
