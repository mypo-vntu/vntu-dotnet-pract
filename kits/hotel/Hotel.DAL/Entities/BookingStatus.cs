namespace Hotel.DAL.Entities;

/// <summary>Booked → CheckedIn → CheckedOut; Booked → Cancelled.</summary>
public enum BookingStatus
{
    Booked,
    CheckedIn,
    CheckedOut,
    Cancelled
}
