namespace CodeSample.Entities;

public class Student
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public DateTime? BirthDate { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? Note { get; set; }
}
