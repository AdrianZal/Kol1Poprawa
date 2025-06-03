using System.Data.Common;
using WebAPI.Exceptions;
using WebAPI.Models.DTOs;
using Microsoft.Data.SqlClient;

namespace WebAPI.Services;

public class DbService : IDbService
{
    private readonly string _connectionString;
    public DbService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default") ?? string.Empty;
    }
    
    public async Task<ClientDetailsDto> GetClientDetailsByIdAsync(int id)
    {
        var query =
            @"SELECT ID, FirstName, LastName, Address,  
            FROM clients
            WHERE ID = @id;";
        
        await using SqlConnection connection = new SqlConnection(_connectionString);
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        command.CommandText = query;
        await connection.OpenAsync();
        
        command.Parameters.AddWithValue("@id", id);
        var reader = await command.ExecuteReaderAsync();
        
        ClientDetailsDto? clientDetails = null;
        
        while (await reader.ReadAsync())
        {
            if (clientDetails is null)
            {
                clientDetails = new ClientDetailsDto
                {
                    Id = reader.GetInt32(0),
                    FirstName = reader.GetString(1),
                    LastName = reader.GetString(2),
                    Address = reader.GetString(3),
                    Rentals = new List<Rental>()
                };
            }
        }       
        
        query =
            @"SELECT c.VIN, cl.Name, m.Name, cr.DateFrom, cr.DateTo, cr.TotalPrice
            FROM car_rentals cr
            JOIN cars c ON c.CarID = cr.CarID
            JOIN colors cl ON c.ColorID = cl.ColorID
            JOIN models m ON m.ModelID = c.ModelID
            WHERE cr.ClientID = @id;";
        
        await using SqlConnection connection1 = new SqlConnection(_connectionString);
        await using SqlCommand command1 = new SqlCommand();
        
        command1.Connection = connection1;
        command1.CommandText = query;
        await connection1.OpenAsync();
        
        command1.Parameters.AddWithValue("@id", id);
        var reader1 = await command1.ExecuteReaderAsync();
        
        while (await reader1.ReadAsync())
        {
            clientDetails.Rentals.Add(new Rental()
            {
                Vin = reader1.GetString(0),
                Color = reader1.GetString(1),
                Model = reader1.GetString(2),
                DateFrom = reader1.GetDateTime(3),
                DateTo = reader1.GetDateTime(4),
                TotalPrice = reader1.GetInt32(5),
            });
        }       
        if (clientDetails is null)
            throw new NotFoundException($"Client with ID {id} not found.");
        
        return clientDetails;
    }

    public async Task AddNewClientWithRentalAsync(CreateClientWithRentalRequestDto clientWithRentalRequest)
    {
        await using SqlConnection connection = new SqlConnection(_connectionString);
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();
        
        DbTransaction transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;

        try
        {
            command.Parameters.Clear();
            command.CommandText = "SELECT PricePerDay FROM cars WHERE ID = @CarId;";
            command.Parameters.AddWithValue("@CarId", clientWithRentalRequest.CarId);
            var pricePerDay = await command.ExecuteScalarAsync();
            if ((await command.ExecuteScalarAsync()) is null)
                throw new NotFoundException($"Car with ID {clientWithRentalRequest.CarId} not found.");
            
            var totalPrice = (clientWithRentalRequest.DateTo - clientWithRentalRequest.DateFrom).Days * (int)pricePerDay;
            
            command.Parameters.Clear();
            command.CommandText = 
                @"INSERT INTO clients (FirstName, LastName, Address)
                        VALUES(@FirstName, @LastName, @Address);";
            command.Parameters.AddWithValue("@FirstName", clientWithRentalRequest.Client.FirstName);
            command.Parameters.AddWithValue("@LastName", clientWithRentalRequest.Client.LastName);
            command.Parameters.AddWithValue("@Address", clientWithRentalRequest.Client.Address);
            
            command.Parameters.Clear();
            command.CommandText = "SELECT ID FROM clients WHERE FirstName = @FirstName AND LastName = @LastName AND Address = @Address;";
            command.Parameters.AddWithValue("@FirstName", clientWithRentalRequest.Client.FirstName);
            command.Parameters.AddWithValue("@LastName", clientWithRentalRequest.Client.LastName);
            command.Parameters.AddWithValue("@Address", clientWithRentalRequest.Client.Address);
            var clientId = await command.ExecuteScalarAsync();
            
            command.Parameters.Clear();
            command.CommandText = 
                @"INSERT INTO car_rentals (ClientID, CarID, @DateFrom, @DateTo, @TotalPrice)
                        VALUES(@ClientID, @CarID, @DateFrom, @DateTo, @TotalPrice);";
            command.Parameters.AddWithValue("@ClientID", clientId);
            command.Parameters.AddWithValue("@CarID", clientWithRentalRequest.CarId);
            command.Parameters.AddWithValue("@DateFrom", clientWithRentalRequest.DateFrom);
            command.Parameters.AddWithValue("@DateTo", clientWithRentalRequest.DateTo);
            command.Parameters.AddWithValue("@TotalPrice", totalPrice);
            
            
            
            await command.ExecuteNonQueryAsync();
            
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }
        

    }
}