using Calendar.DTOs.Appointment;
using Calendar.Mappers;
using Calendar.Models;
using Calendar.Repositories;
using Type = Calendar.Models.Type;

namespace Calendar.Services;

public class AppointmentService
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IParticipantRepository _participantRepository;
    private readonly ITypeRepository _typeRepository;

    public AppointmentService(
        IAppointmentRepository appointmentRepository,
        IParticipantRepository participantRepository,
        ITypeRepository typeRepository
    )
    {
        _appointmentRepository = appointmentRepository;
        _participantRepository = participantRepository;
        _typeRepository = typeRepository;
    }

    public async Task<List<Appointment>> GetAsync(GetAppointmentRequestDto getAppointmentRequestDto, string userId)
    {
        List<Appointment> appointments1 = await _appointmentRepository.GetByUserIdAsync(userId);
        List<Appointment> appointments2 = await _participantRepository.GetAppointmentsByIdAsync(userId);

        // Merge the lists
        List<Appointment> allAppointments = appointments1.Concat(appointments2).ToList();

        // If you need distinct appointments, use Distinct
        // List<Appointment> allAppointments = appointments1.Concat(appointments2).Distinct().ToList();

        return allAppointments;
    }

    public async Task<Appointment> AddAsync(AddAppointmentRequestDto addAppointmentRequestDto, string userId)
    {
        var appointmentsList1 = await _appointmentRepository.GetByUserIdAsync(userId);
        var appointmentsList2 = await _participantRepository.GetAppointmentsByIdAsync(userId);

        foreach (var app in appointmentsList1)
        {
            if (app.AppointmentDate == addAppointmentRequestDto.AppointmentDate &&
                (addAppointmentRequestDto.StartTime < app.EndTime && addAppointmentRequestDto.EndTime > app.StartTime))
            {
                throw new Exception("Conflict detected");
            }
        }

        foreach (var app in appointmentsList2)
        {
            if (app.AppointmentDate == addAppointmentRequestDto.AppointmentDate &&
                (addAppointmentRequestDto.StartTime < app.EndTime && addAppointmentRequestDto.EndTime > app.StartTime))
            {
                throw new Exception("Conflict detected");
            }
        }

        var appointment = AppointmentMapper.ToEntity(addAppointmentRequestDto, Guid.Parse(userId));

        await _appointmentRepository.AddAsync(appointment);

        // Add participants
        var participants = addAppointmentRequestDto.Participants.Select(participantId => new Participant
        {
            AppointmentId = appointment.AppointmentId,
            UserId = Guid.Parse(participantId)
        }).ToList();

        await _participantRepository.BulkAddAsync(participants);

        return appointment;
    }

    public async Task<List<Type>> GetTypesAsync()
    {
        return await _typeRepository.GetAllAsync();
    }

    public async Task DeleteAsync(Guid appointmentId)
    {
        await _appointmentRepository.DeleteByIdAsync(appointmentId);
    }

    public async Task<bool> EditAsync(Guid appointmentId, AddAppointmentRequestDto appointmentDto)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(appointmentId);
        if (appointment == null)
        {
            return false;
        }

        // Update appointment properties
        appointment.Title = appointmentDto.Title;
        appointment.Description = appointmentDto.Description;
        appointment.AppointmentDate = appointmentDto.AppointmentDate;
        appointment.StartTime = appointmentDto.StartTime;
        appointment.EndTime = appointmentDto.EndTime;
        appointment.AppointmentTypeId = appointmentDto.AppointmentTypeId;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _appointmentRepository.UpdateAsync(appointment);

        // Update participants
        var existingParticipants = await _participantRepository.GetByAppointmentIdAsync(appointmentId);
        var existingParticipantIds = existingParticipants.Select(p => p.UserId).ToHashSet();

        var newParticipantIds = appointmentDto.Participants.Select(Guid.Parse).ToHashSet();

        // Determine participants to add and remove
        var participantsToAdd = newParticipantIds.Except(existingParticipantIds)
            .Select(userId => new Participant { AppointmentId = appointmentId, UserId = userId }).ToList();

        var participantsToRemove = existingParticipantIds.Except(newParticipantIds).ToList();

        // Add new participants
        await _participantRepository.BulkAddAsync(participantsToAdd);

        // Remove old participants
        await _participantRepository.BulkRemoveAsync(appointmentId, participantsToRemove);

        return true;
    }
}