using PetClinicSystem.Models;

public class Notification
{
    public int NotificationId { get; set; }

    // who owns this notification
    public int AccountId { get; set; }
    public Account Account { get; set; }

    public string Title { get; set; }          // e.g. "New appointment booked"
    public string Message { get; set; }        // more detailed text
    public string LinkUrl { get; set; }        // where to go when clicked (optional)

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
