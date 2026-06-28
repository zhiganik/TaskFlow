using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Infrastructure.Repositories;

internal static class TaskQueryExtensions
{
    internal static IQueryable<WorkspaceTask> ApplyFilters(
        this IQueryable<WorkspaceTask> q, TaskFilterQuery filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            q = int.TryParse(s, out var num)
                ? q.Where(t => t.Number == num || t.Title.ToLower().Contains(s.ToLower()))
                : q.Where(t => t.Title.ToLower().Contains(s.ToLower()));
        }

        if (filter.AssigneeIds is { Count: > 0 })
        {
            var includeUnassigned = filter.AssigneeIds.Contains("unassigned");
            var userIds = filter.AssigneeIds.Where(id => id != "unassigned").ToList();

            if (includeUnassigned && userIds.Count > 0)
                q = q.Where(t => t.AssigneeId == null || userIds.Contains(t.AssigneeId));
            else if (includeUnassigned)
                q = q.Where(t => t.AssigneeId == null);
            else
                q = q.Where(t => t.AssigneeId != null && userIds.Contains(t.AssigneeId));
        }

        if (filter.Priorities is { Count: > 0 })
            q = q.Where(t => filter.Priorities.Contains(t.Priority));

        if (filter.LabelIds is { Count: > 0 })
        {
            var labelIds = filter.LabelIds.ToList();
            q = q.Where(t => t.Labels.Any(l => labelIds.Contains(l.Id)));
        }

        return q;
    }
}
