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
    
    public async Task<BookingDetailsDto> GetBookingByIdAsync(int bookingId)
    {
        var query =
            @"SELECT b.date, g.first_name, g.last_name, g.date_of_birth, e.first_name, e.last_name, e.employee_number
            FROM Booking b
            JOIN Guest g ON b.guest_id = g.guest_id
            JOIN Employee e ON b.employee_id = e.employee_id
            WHERE b.booking_id = @bookingId;";
        
        await using SqlConnection connection = new SqlConnection(_connectionString);
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        command.CommandText = query;
        await connection.OpenAsync();
        
        command.Parameters.AddWithValue("@bookingId", bookingId);
        var reader = await command.ExecuteReaderAsync();
        
        BookingDetailsDto? booking = null;
        
        while (await reader.ReadAsync())
        {
            if (booking is null)
            {
                booking = new BookingDetailsDto
                {
                    BookingDate = reader.GetDateTime(0),
                    Guest = new Guest()
                    {
                        FirstName = reader.GetString(1),
                        LastName = reader.GetString(2),
                        DateOfBirth = reader.GetDateTime(3)
                    },
                    Employee = new Employee()
                    {
                        FirstName = reader.GetString(4),
                        LastName = reader.GetString(5),
                        EmployeeNumber = reader.GetString(6),
                    },
                    Attractions = new List<Attraction>()
                };
            }
        }       
        
        query =
            @"SELECT a.name, a.price, ba.amount
            FROM Booking_Attraction ba
            JOIN Attraction a ON a.attraction_id = ba.attraction_id
            WHERE ba.booking_id = @bookingId;";
        
        await using SqlConnection connection1 = new SqlConnection(_connectionString);
        await using SqlCommand command1 = new SqlCommand();
        
        command1.Connection = connection1;
        command1.CommandText = query;
        await connection1.OpenAsync();
        
        command1.Parameters.AddWithValue("@bookingId", bookingId);
        var reader1 = await command1.ExecuteReaderAsync();
        
        while (await reader1.ReadAsync())
        {
            booking.Attractions.Add(new Attraction()
            {
                Name = reader1.GetString(0),
                Price = reader1.GetDecimal(1),
                Amount = reader1.GetInt32(2),
            });
        }       
        
        return booking;
    }

    public async Task AddNewBookingAsync(CreateBookingRequestDto bookingRequest)
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
            command.CommandText = "SELECT 1 FROM Guest WHERE guest_id = @GuestId;";
            command.Parameters.AddWithValue("@GuestId", bookingRequest.GuestId);
            if ((await command.ExecuteScalarAsync()) is null)
                throw new NotFoundException($"Guest with ID {bookingRequest.GuestId} not found.");
            
            command.Parameters.Clear();
            command.CommandText = "SELECT employee_id FROM Employee WHERE employee_number = @EmployeeNumber;";
            command.Parameters.AddWithValue("@EmployeeNumber", bookingRequest.EmployeeNumber);
            var employeeId = await command.ExecuteScalarAsync();
            if (employeeId is null)
                throw new NotFoundException($"Employee {bookingRequest.EmployeeNumber} not found.");

            command.Parameters.Clear();
            command.CommandText = 
                @"INSERT INTO Booking (booking_id, guest_id, employee_id, date)
                        VALUES(@BookingId, @GuestId, @EmployeeId, @BookingDate);";
            command.Parameters.AddWithValue("@BookingId", bookingRequest.BookingId);
            command.Parameters.AddWithValue("@GuestId", bookingRequest.GuestId);
            command.Parameters.AddWithValue("@EmployeeId", employeeId);
            command.Parameters.AddWithValue("@BookingDate", DateTime.Now);
            
            await command.ExecuteNonQueryAsync();
            
            foreach (var attraction in bookingRequest.Attractions)
            {
                command.Parameters.Clear();
                command.CommandText = "SELECT attraction_id FROM Attraction WHERE name = @AttractionName;";
                command.Parameters.AddWithValue("@AttractionName", attraction.Name);
                
                var attractionId = await command.ExecuteScalarAsync();
                if(attractionId is null)
                    throw new NotFoundException($"Attraction - {attraction.Name} - not found.");
                
                command.Parameters.Clear();
                command.CommandText = 
                    @"INSERT INTO Booking_Attraction (booking_id, attraction_id, amount)
                        VALUES(@BookingId, @AttractionId, @AttractionAmount);";
        
                command.Parameters.AddWithValue("@BookingId", bookingRequest.BookingId);
                command.Parameters.AddWithValue("@AttractionId", attractionId);
                command.Parameters.AddWithValue("@AttractionAmount", attraction.Amount);
                
                await command.ExecuteNonQueryAsync();
            }
            
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }
        

    }
}