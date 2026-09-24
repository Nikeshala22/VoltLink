using VoltLink.Api.Dtos;
using VoltLink.Api.Models;
using VoltLink.Api.Repositories;

namespace VoltLink.Api.Services;


public record CallerContext(string UserId, string Role)
{
    
    public bool IsStaff =>
        Role == UserRoles.Backoffice || Role == UserRoles.GridOperator;
}


public interface IReservationService
{
    
    Task<ReservationSummaryResponse> CreateAsync(
        CreateReservationRequest request,
        CallerContext caller,
        CancellationToken cancellationToken = default);

    
    Task<ReservationSummaryResponse> UpdateAsync(
        string id,
        UpdateReservationRequest request,
        CallerContext caller,
        CancellationToken cancellationToken = default);

   
    Task<ReservationSummaryResponse> CancelAsync(
        string id, CallerContext caller, CancellationToken cancellationToken = default);

   
    Task<ReservationResponse> ApproveAsync(
        string id, CancellationToken cancellationToken = default);

   
    Task<ReservationResponse> RejectAsync(
        string id, CancellationToken cancellationToken = default);

    
    Task<ReservationResponse> GetByIdAsync(
        string id, CallerContext caller, CancellationToken cancellationToken = default);

    
    Task<IReadOnlyList<ReservationResponse>> SearchAsync(
        ReservationQuery query, CallerContext caller, CancellationToken cancellationToken = default);

       Task<QrCodeResponse> GetQrCodeAsync(
        string id, CallerContext caller, CancellationToken cancellationToken = default);

    
    Task<ReservationResponse> VerifyQrAsync(
        string token, CancellationToken cancellationToken = default);

    
    Task<ReservationSummaryResponse> CompleteAsync(
        string id, CallerContext caller, CancellationToken cancellationToken = default);
}
