using Calendar.DTOs.Appointment;
using Calendar.Models;
using Calendar.Repositories;

namespace Calendar.Services
{
    public class ParticipantService
    {
        private readonly IParticipantRepository _participantRepository;
        private readonly IAppointmentRepository _appointmentRepository;

        public ParticipantService(IParticipantRepository participantRepository,
            IAppointmentRepository appointmentRepository)
        {
            _participantRepository = participantRepository;
            _appointmentRepository = appointmentRepository;
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _participantRepository.GetAllAsync();
        }

        public async Task<List<User>> GetByAppointmentIdAsync(Guid appointmentId)
        {
            return await _participantRepository.GetByAppointmentIdAsync(appointmentId);
        }

        public async Task<Boolean> checkAvailability(CheckAvailabilityRequestDto checkAvailabilityRequestDto)
        {
            var appointmentsList1 = await _appointmentRepository.GetByUserIdAsync(
                checkAvailabilityRequestDto.ParticipantId.ToString(), checkAvailabilityRequestDto.AppointmentDate,
                checkAvailabilityRequestDto.AppointmentDate);
            var appointmentsList2 =
                await _participantRepository.GetAppointmentsByIdAsync(
                    checkAvailabilityRequestDto.ParticipantId.ToString(), checkAvailabilityRequestDto.AppointmentDate,
                    checkAvailabilityRequestDto.AppointmentDate);

            foreach (var app in appointmentsList1)
            {
                if (app.AppointmentDate == checkAvailabilityRequestDto.AppointmentDate &&
                    (checkAvailabilityRequestDto.StartTime < app.EndTime &&
                     checkAvailabilityRequestDto.EndTime > app.StartTime))
                {
                    return false;
                }
            }

            foreach (var app in appointmentsList2)
            {
                if (app.AppointmentDate == checkAvailabilityRequestDto.AppointmentDate &&
                    (checkAvailabilityRequestDto.StartTime < app.EndTime &&
                     checkAvailabilityRequestDto.EndTime > app.StartTime))
                {
                    return false;
                }
            }

            return true;
        }
    }
}