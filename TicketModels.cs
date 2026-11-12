namespace SupportDemo;

public sealed record Ticket(int Id, string Title, string Description, string Status, DateTime CreatedAt);
public sealed record CreateTicketRequest(string? Title, string? Description);
public sealed record UpdateTicketStatusRequest(string? Status);

public static class TicketStatuses
{
    public static bool IsValid(string? status) => status is "New" or "InProgress" or "Closed";
}
