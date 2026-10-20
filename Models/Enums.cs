namespace GroupProject.Models;

public enum UserRole
{
    Admin,
    Staff,
    Member,
    SuspendedMember
}

public enum RoomTag
{
    LivingRoom,
    Kitchen,
    Bedroom,
    Office,
    Other
}

//TL - Unused DisposalMethod left here because it was in the schema
//public enum DisposalMethod { }

// TQ- Tracks status of User requested items to be added
public enum RequestStatus
{
    Pending,
    Approved,
    Rejected
}