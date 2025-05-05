using Microsoft.AspNetCore.Mvc;
using TravelApp.Models.DTOs;
using TravelApp.Services;

namespace TravelApp.Controllers;

[ApiController]
[Route("api")]
public class TravelsController(IDbService dbService) : ControllerBase
{
    
    /// <summary>
    /// zwraca informacje o wszystkich wycieczkach
    /// </summary>

    
    [HttpGet("trips")]
    public async Task<IActionResult> GetAllTrips()
    {
        return Ok(await dbService.GetTripsAsync());
    }
    
    /// <summary>
    /// zwraca wszystkie informacje o wycieczkach dla klienta o podanym id
    /// </summary>
    /// <param name="id">Id kienta</param>
    
    
    [HttpGet("clients/{id}/trips")]
    public async Task<IActionResult> GetClientTrips(int id)
    {
        var trips = await dbService.GetClientTripsAsync(id);

        if (!trips.Any())
        {
            return NotFound($"Klient o ID {id} nie istnieje lub nie ma zarejestrowanych wycieczek.");
        }

        return Ok(trips);
    }
    
    /// <summary>
    /// Tworzy nowego klienta w systemie.
    /// </summary>
    /// <param name="clientDto">Obiekt DTO zawierający dane nowego klienta.</param>
    /// <returns>Id nowo utworzonego klienta.</returns>

    
    [HttpPost("clients")]
    public async Task<IActionResult> CreateClient([FromBody] ClientCreateDTO clientDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var newClientId = await dbService.CreateClientAsync(clientDto);
            return CreatedAtAction(nameof(GetClientTrips), new { id = newClientId }, new { Id = newClientId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Wystąpił błąd podczas tworzenia klienta: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Rejestruje klienta na wycieczkę o podanym ID.
    /// </summary>
    /// <param name="clientId">Id klienta.</param>
    /// <param name="tripId">Id wycieczki.</param>
    /// <returns>Komunikat o wyniku operacji (np. sukces, brak klienta, brak wycieczki).</returns>

    
    [HttpPut("clients/{id}/trips/{tripId}")]
    public async Task<IActionResult> RegisterClientForTrip(int id, int tripId)
    {
        try
        {
            var result = await dbService.RegisterClientForTripAsync(id, tripId);

            if (result == "ClientNotFound")
                return NotFound($"Klient o ID {id} nie istnieje.");
            if (result == "TripNotFound")
                return NotFound($"Wycieczka o ID {tripId} nie istnieje.");
            if (result == "MaxParticipantsReached")
                return BadRequest("Osiągnięto maksymalną liczbę uczestników dla tej wycieczki.");
            if (result == "AlreadyRegistered")
                return BadRequest("Klient jest już zarejestrowany na tę wycieczkę.");

            return Ok("Klient został pomyślnie zarejestrowany na wycieczkę.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Wystąpił błąd podczas rejestracji: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Usuwa rejestrację klienta z wycieczki o podanym ID.
    /// </summary>
    /// <param name="clientId">Id klienta.</param>
    /// <param name="tripId">Id wycieczki.</param>
    /// <returns>Komunikat o wyniku operacji (np. sukces, brak rejestracji).</returns>

    
    [HttpDelete("clients/{id}/trips/{tripId}")]
    public async Task<IActionResult> UnregisterClientFromTrip(int id, int tripId)
    {
        try
        {
            var result = await dbService.UnregisterClientFromTripAsync(id, tripId);

            if (result == "RegistrationNotFound")
                return NotFound($"Rejestracja klienta o ID {id} na wycieczkę o ID {tripId} nie istnieje.");

            return Ok("Rejestracja klienta została pomyślnie usunięta.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Wystąpił błąd podczas usuwania rejestracji: {ex.Message}");
        }
    }
    
    
}