using System.Data.Common;
using Microsoft.Data.SqlClient;
using Mock.Exception;
using Mock.Models.DTO;

namespace Mock.Services;

public class AppointmentService : IAppointmentService
{
    private readonly string _connectionString;

    public AppointmentService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default") ?? string.Empty;
    }

    public async Task<AppointmentDTO> GetAppointmentInfo(int appId)
    {
        var query =
            @"SELECT a.date as AppointmentDate, p.first_name as PatientFirstName, p.last_name as PatientLastName, p.date_of_birth as PatientDateOfBirth, d.doctor_id, d.PWZ, s.name as ServiceName, aps.service_fee
              from Appointment a
              join Patient p on a.patient_id = p.patient_id
              join Doctor d on a.doctor_id = d.doctor_id
              JOIN Appointment_Service aps ON a.appointment_id = aps.appointment_id
              JOIN Service s ON aps.service_id = s.service_id
              WHERE a.appointment_id = @AppId";

        await using SqlConnection conn = new SqlConnection(_connectionString);
        await using SqlCommand cmd = new SqlCommand(query, conn);

        cmd.Parameters.AddWithValue("@AppId", appId);
        
        await conn.OpenAsync();
        var reader = await cmd.ExecuteReaderAsync();

        AppointmentDTO? app = null;

        while (await reader.ReadAsync())
        {
            if (app is null)
            {
                app = new AppointmentDTO
                {
                    Date = reader.GetDateTime(reader.GetOrdinal("AppointmentDate")),
                    Patient = new PatientDTO
                    {
                        FirstName = reader.GetString(reader.GetOrdinal("PatientFirstName")),
                        LastName = reader.GetString(reader.GetOrdinal("PatientLastName")),
                        dateOfBirth = reader.GetDateTime(reader.GetOrdinal("PatientDateOfBirth"))
                    },
                    Doctor = new DoctorDTO
                    {
                        DoctorId = reader.GetInt32(reader.GetOrdinal("doctor_id")),
                        PWZ = reader.GetString(reader.GetOrdinal("PWZ"))
                    }
                };
            }
            app.Services.Add(new ServiceDTO
            {
                Name = reader.GetString(reader.GetOrdinal("ServiceName")),
                ServiceFee = reader.GetDecimal(reader.GetOrdinal("service_fee"))
            });
        }
        
        if (app is null)
        {
            throw new NotFoundException($"No Appointment with this ID #{appId}");
        }

        return app;
    }

    public async Task AddNewAppointment(CreateAppointmentRequestDTO app)
    {
        await using SqlConnection conn = new SqlConnection(_connectionString);
        await using SqlCommand cmd = new SqlCommand();
        
        cmd.Connection = conn;
        await conn.OpenAsync();

        DbTransaction transaction = await conn.BeginTransactionAsync();
        cmd.Transaction = transaction as SqlTransaction;

        try
        {
            cmd.Parameters.Clear();
            cmd.CommandText = @"select 1 from Appointment where appointment_id = @appId";
            cmd.Parameters.AddWithValue("@appId", app.AppointmentId);

            var appIdRes = await cmd.ExecuteScalarAsync();
            if (appIdRes is not null)
                throw new AlreadyExistException($"Appointment with ID #{app.AppointmentId} is already exist");
            
            cmd.Parameters.Clear();
            cmd.CommandText = @"select 1 from Patient where patient_id = @patId";
            cmd.Parameters.AddWithValue("@patId", app.PatientId);

            var patIdRes = await cmd.ExecuteScalarAsync();
            if (patIdRes is null)
                throw new NotFoundException($"Patient with ID #{app.PatientId} is Not Found");
            
            cmd.Parameters.Clear();
            cmd.CommandText = @"select doctor_id from Doctor where PWZ = @pwz";
            cmd.Parameters.AddWithValue("@pwz", app.PWZ);

            var docPwzRes = await cmd.ExecuteScalarAsync();
            if (docPwzRes is null)
                throw new NotFoundException($"Doctor with PWZ #{app.PWZ} is Not Found");
            
            cmd.Parameters.Clear();
            cmd.CommandText =
                @"Insert INTO Appointment 
                  VALUES(@appId, @patId, @docId, @date)";
            cmd.Parameters.AddWithValue("@appId", app.AppointmentId);
            cmd.Parameters.AddWithValue("@patId", app.PatientId);
            cmd.Parameters.AddWithValue("@docId", docPwzRes);
            cmd.Parameters.AddWithValue("@date", DateTime.Now);

            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (AlreadyExistException e)
            {
                throw new AlreadyExistException("A appointment with the same ID already exists.");
            }

            foreach (var service in app.Services)
            {
                cmd.Parameters.Clear();
                cmd.CommandText = "select service_id from service where name = @name";
                cmd.Parameters.AddWithValue("@name", service.Name);

                var serNameRes = await cmd.ExecuteScalarAsync();
                if (serNameRes is null)
                    throw new NotFoundException($"\'{service.Name}\' service is Not Found");
                
                cmd.Parameters.Clear();
                cmd.CommandText =
                    @"Insert Into Appointment_Service
                      VALUES(@appId, @serId, @serFee)";
                cmd.Parameters.AddWithValue("@appId", app.AppointmentId);
                cmd.Parameters.AddWithValue("@serId", serNameRes);
                cmd.Parameters.AddWithValue("@serFee", service.ServiceFee);
                
                await cmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();

        }
        catch (System.Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}