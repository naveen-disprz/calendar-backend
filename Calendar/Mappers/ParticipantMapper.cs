using Calendar.DTOs.Appointment;
using Calendar.Models;

namespace Calendar.Mappers;

public class ParticipantMapper
{
    public static ParticipantResponseDto ToDto(Participant participant)
    {
        return new ParticipantResponseDto
        {
            UserId = participant.UserId,
            User = participant.User,
            AppointmentId = participant.AppointmentId,
        };
    }

    public static Participant ToEntity(ParticipantRequestDto dto, Guid userId)
    {
        return new Participant
        {
            AppointmentId = dto.AppointmentId,
            UserId = userId,
        };
    }
}