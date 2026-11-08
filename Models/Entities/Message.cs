namespace KicksMvc.Models.Entities;

public class Message
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public int RecipientId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset? ReadAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User Sender { get; set; } = null!;
    public User Recipient { get; set; } = null!;
}
