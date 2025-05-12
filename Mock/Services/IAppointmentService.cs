using Mock.Models.DTO;

namespace Mock.Services;

public interface IAppointmentService
{
    Task<AppointmentDTO> GetAppointmentInfo(int id);
    Task AddNewAppointment(CreateAppointmentRequestDTO app);
}