using BackBuddy.Api.Service.V1.Device.Entities;
using BackBuddy.Api.Service.V1.Device.Enums;
using BackBuddy.Api.Service.V1.Device.Exceptions;
using BackBuddy.Api.Service.V1.Device.Repositories;
using BackBuddy.Api.Service.V1.Utilities;

namespace BackBuddy.Api.Service.V1.Device.Services
{
    public interface IReportLikeService
    {
        Task AddLike(string userId, ReportEntity report, IEnumerable<ReportVisibilityType> visibilityTypes, CancellationToken cancellationToken = default);
        Task DeleteAllLikesFromUser(string userId, CancellationToken cancellationToken = default);
        Task DeleteAllLikesFromReport(Guid reportId, CancellationToken cancellationToken = default);
        Task DeleteLike(string userId, Guid reportId, CancellationToken cancellationToken = default);
        Task<bool> HasLikedReport(string userId, Guid reportId, CancellationToken cancellationToken = default);
        Task<Page<List<Guid>>> GetReportLikesFromUser(string userid, PageRequestDto page, CancellationToken cancellationToken = default);
        Task<Page<List<string>>> GetReportLikesFromReport(ReportEntity report, IEnumerable<ReportVisibilityType> visibilityTypes, PageRequestDto page, CancellationToken cancellationToken = default);
        Task<long> CountLikesFromUser(string userId, CancellationToken cancellationToken = default);
        Task<long> CountLikesFromReport(Guid reportId, CancellationToken cancellationToken = default);
    }
    public class ReportLikeService(IReportLikeRepository repository) : IReportLikeService
    {
        private readonly IReportLikeRepository _repository = repository;

        public async Task AddLike(string userId, ReportEntity report, IEnumerable<ReportVisibilityType> visibilityTypes, CancellationToken cancellationToken = default)
        {
            if (!visibilityTypes.Contains(report.VisibilityType))
                throw new ReportNotFoundException();
            if (userId == report.UserId)
                throw new ReportLikeCannotLikeOwnReportException();

            bool alreadyLiked = await HasLikedReport(userId, report.Id, cancellationToken);
            if (alreadyLiked)
                throw new ReportLikeAlreadyLikedException();

            ReportLikeEntity reportLikeEntity = new()
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                ReportId = report.Id
            };

            await _repository.AddLike(reportLikeEntity, cancellationToken);
        }

        public async Task<long> CountLikesFromReport(Guid reportId, CancellationToken cancellationToken = default)
        {
            return await _repository.CountLikesFromReport(reportId, cancellationToken);
        }

        public async Task<long> CountLikesFromUser(string userId, CancellationToken cancellationToken = default)
        {
            return await _repository.CountLikesFromUser(userId, cancellationToken);
        }

        public async Task DeleteAllLikesFromReport(Guid reportId, CancellationToken cancellationToken = default)
        {
            await _repository.DeleteAllLikesFromReport(reportId, cancellationToken);
        }

        public async Task DeleteAllLikesFromUser(string userId, CancellationToken cancellationToken = default)
        {
            await _repository.DeleteAllLikesFromUser(userId, cancellationToken);
        }

        public async Task DeleteLike(string userId, Guid reportId, CancellationToken cancellationToken = default)
        {
            await _repository.DeleteLike(userId, reportId, cancellationToken);
        }

        public async Task<Page<List<string>>> GetReportLikesFromReport(ReportEntity report, IEnumerable<ReportVisibilityType> visibilityTypes, PageRequestDto page, CancellationToken cancellationToken = default)
        {
            if (!visibilityTypes.Contains(report.VisibilityType))
                throw new ReportNotFoundException();
            Page<List<ReportLikeEntity>> result = await _repository.GetReportLikesFromReport(report.Id, page, cancellationToken);
            return new Page<List<string>>
            {
                Items = [.. result.Items.Select(x => x.UserId)],
                HasMoreEntries = result.HasMoreEntries
            };
        }

        public async Task<Page<List<Guid>>> GetReportLikesFromUser(string userid, PageRequestDto page, CancellationToken cancellationToken = default)
        {
            Page<List<ReportLikeEntity>> result = await _repository.GetReportLikesFromUser(userid, page, cancellationToken);
            return new Page<List<Guid>>
            {
                Items = [.. result.Items.Select(x => x.ReportId)],
                HasMoreEntries = result.HasMoreEntries
            };
        }

        public async Task<bool> HasLikedReport(string userId, Guid reportId, CancellationToken cancellationToken = default)
        {
            return await _repository.HasLikedReport(userId, reportId, cancellationToken);
        }
    }
}
