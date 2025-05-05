using Microsoft.Data.SqlClient;
using TravelApp.Models.DTOs;

namespace TravelApp.Services;

public interface IDbService
{
    public Task<IEnumerable<TripGetDTO>> GetTripsAsync();
    Task<IEnumerable<ClientTripDTO>> GetClientTripsAsync(int clientId);
    Task<int> CreateClientAsync(ClientCreateDTO clientDto);
    Task<string> RegisterClientForTripAsync(int clientId, int tripId);
    public Task<string> UnregisterClientFromTripAsync(int clientId, int tripId);
}

public class DbService(IConfiguration config): IDbService
{
    private readonly string? _connectionString = config.GetConnectionString("Default");


    public async Task<IEnumerable<TripGetDTO>> GetTripsAsync()
    {
        await using var con = new SqlConnection(_connectionString);
        
        var result = new List<TripGetDTO>();
        const string getAllTripsInfoSql = @"select t.IdTrip,
                            t.Name,
                            t.Description,
                            t.DateFrom,
                            t.DateTo,
                            t.MaxPeople,
                            c.Name
                            from Trip t
                            JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
                            JOIN Country c ON ct.IdCountry = c.IdCountry";
        
        await using var cmd = new SqlCommand(getAllTripsInfoSql, con);
        await con.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new TripGetDTO
            {
                IdTrip = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                DateFrom = reader.GetDateTime(3),
                DateTo = reader.GetDateTime(4),
                MaxPeople = reader.GetInt32(5),
                CountryName = reader.GetString(6)
            });
        }
        return result;
    }

    public async Task<IEnumerable<ClientTripDTO>> GetClientTripsAsync(int clientId)
    {
        await using var con = new SqlConnection(_connectionString);

        var result = new List<ClientTripDTO>();
        const string getClientTripsSql = @"
        SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, c.Name AS CountryName, 
               ct.PaymentDate, ct.RegisteredAt
        FROM Client_Trip ct
        JOIN Trip t ON ct.IdTrip = t.IdTrip
        JOIN Country_Trip ctr ON t.IdTrip = ctr.IdTrip
        JOIN Country c ON ctr.IdCountry = c.IdCountry
        WHERE ct.IdClient = @IdClient";

        await using var cmd = new SqlCommand(getClientTripsSql, con);
        cmd.Parameters.AddWithValue("@IdClient", clientId);

        await con.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new ClientTripDTO
            {
                IdTrip = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                DateFrom = reader.GetDateTime(3),
                DateTo = reader.GetDateTime(4),
                MaxPeople = reader.GetInt32(5),
                CountryName = reader.GetString(6),
                PaymentDate = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                RegisteredAt = reader.GetInt32(8)
            });
        }

        return result;
    }
    
    public async Task<int> CreateClientAsync(ClientCreateDTO clientDto)
    {
        await using var con = new SqlConnection(_connectionString);
        const string insertClientSql = @"
        INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
        OUTPUT INSERTED.IdClient
        VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)";

        await using var cmd = new SqlCommand(insertClientSql, con);
        cmd.Parameters.AddWithValue("@FirstName", clientDto.FirstName);
        cmd.Parameters.AddWithValue("@LastName", clientDto.LastName);
        cmd.Parameters.AddWithValue("@Email", clientDto.Email);
        cmd.Parameters.AddWithValue("@Telephone", clientDto.Telephone);
        cmd.Parameters.AddWithValue("@Pesel", clientDto.Pesel);

        await con.OpenAsync();
        var newClientId = (int)await cmd.ExecuteScalarAsync();
        return newClientId;
    }
    
    
    public async Task<string> RegisterClientForTripAsync(int clientId, int tripId)
    {
    await using var con = new SqlConnection(_connectionString);

    // czy klient istnieje
    const string checkClientSql = "SELECT COUNT(1) FROM Client WHERE IdClient = @ClientId";
    await using var checkClientCmd = new SqlCommand(checkClientSql, con);
    checkClientCmd.Parameters.AddWithValue("@ClientId", clientId);

    await con.OpenAsync();
    var clientExists = (int)await checkClientCmd.ExecuteScalarAsync() > 0;
    if (!clientExists) return "ClientNotFound";

    // czy wycieczka istnieje
    const string checkTripSql = "SELECT COUNT(1) FROM Trip WHERE IdTrip = @TripId";
    await using var checkTripCmd = new SqlCommand(checkTripSql, con);
    checkTripCmd.Parameters.AddWithValue("@TripId", tripId);

    var tripExists = (int)await checkTripCmd.ExecuteScalarAsync() > 0;
    if (!tripExists) return "TripNotFound";

    // czy osiągnięto maksymalną liczbę uczestnikóws
    const string checkMaxParticipantsSql = @"
        SELECT COUNT(1) 
        FROM Client_Trip 
        WHERE IdTrip = @TripId";
    const string getMaxPeopleSql = @"
        SELECT MaxPeople 
        FROM Trip 
        WHERE IdTrip = @TripId";

    await using var checkMaxParticipantsCmd = new SqlCommand(checkMaxParticipantsSql, con);
    checkMaxParticipantsCmd.Parameters.AddWithValue("@TripId", tripId);
    var currentParticipants = (int)await checkMaxParticipantsCmd.ExecuteScalarAsync();

    await using var getMaxPeopleCmd = new SqlCommand(getMaxPeopleSql, con);
    getMaxPeopleCmd.Parameters.AddWithValue("@TripId", tripId);
    var maxPeople = (int)await getMaxPeopleCmd.ExecuteScalarAsync();

    if (currentParticipants >= maxPeople) return "MaxParticipantsReached";

    // czy klient jest już zarejestrowany
    const string checkRegistrationSql = @"
        SELECT COUNT(1) 
        FROM Client_Trip 
        WHERE IdClient = @ClientId AND IdTrip = @TripId";
    await using var checkRegistrationCmd = new SqlCommand(checkRegistrationSql, con);
    checkRegistrationCmd.Parameters.AddWithValue("@ClientId", clientId);
    checkRegistrationCmd.Parameters.AddWithValue("@TripId", tripId);

    var alreadyRegistered = (int)await checkRegistrationCmd.ExecuteScalarAsync() > 0;
    if (alreadyRegistered) return "AlreadyRegistered";

    // Rejestracja
    const string registerClientSql = @"
    INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt)
    VALUES (@ClientId, @TripId, @RegisteredAt)";
    await using var registerClientCmd = new SqlCommand(registerClientSql, con);
    registerClientCmd.Parameters.AddWithValue("@ClientId", clientId);
    registerClientCmd.Parameters.AddWithValue("@TripId", tripId);
    
    int registeredAt = int.Parse(DateTime.Now.ToString("yyyyMMdd"));
    
    // Console.WriteLine($"RegisteredAt: {registeredAt}");
        
    registerClientCmd.Parameters.AddWithValue("@RegisteredAt", registeredAt);

    await registerClientCmd.ExecuteNonQueryAsync();
    return "Success";
    }
    public async Task<string> UnregisterClientFromTripAsync(int clientId, int tripId)
    {
        await using var con = new SqlConnection(_connectionString);

        // czy rejestracja istnieje
        const string checkRegistrationSql = @"
        SELECT COUNT(1)
        FROM Client_Trip
        WHERE IdClient = @ClientId AND IdTrip = @TripId";
        await using var checkRegistrationCmd = new SqlCommand(checkRegistrationSql, con);
        checkRegistrationCmd.Parameters.AddWithValue("@ClientId", clientId);
        checkRegistrationCmd.Parameters.AddWithValue("@TripId", tripId);

        await con.OpenAsync();
        var registrationExists = (int)await checkRegistrationCmd.ExecuteScalarAsync() > 0;

        if (!registrationExists) return "RegistrationNotFound";

        // Usunięcie
        const string deleteRegistrationSql = @"
        DELETE FROM Client_Trip
        WHERE IdClient = @ClientId AND IdTrip = @TripId";
        await using var deleteRegistrationCmd = new SqlCommand(deleteRegistrationSql, con);
        deleteRegistrationCmd.Parameters.AddWithValue("@ClientId", clientId);
        deleteRegistrationCmd.Parameters.AddWithValue("@TripId", tripId);

        await deleteRegistrationCmd.ExecuteNonQueryAsync();
        return "Success";
    }
    
}