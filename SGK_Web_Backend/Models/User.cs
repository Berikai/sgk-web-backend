using SGK_Web_Backend.Enums;

namespace SGK_Web_Backend.Models;

/**
 * User model
 *
 * Required properties: student_id, username, email, password
 */
public class User
{
    public Guid id { get; set; }
    public required string student_id { get; set; }
    public required string username { get; set; }
    public required string email { get; set; } // School email
    public required string password { get; set; } // Hashed password with a salt: hash(password + salt) 
    public UserRole role { get; set; }
    public string? name { get; set; }
    public string? surname { get; set; }
    public string? phone_number { get; set; }
    public string? studied_year { get; set; }
    public string? studied_department { get; set; }
    public string? created_at { get; set; }
    public string? ip_created_at { get; set; }
    public string? ip_last { get; set; }
    public bool verified { get; set; }
}