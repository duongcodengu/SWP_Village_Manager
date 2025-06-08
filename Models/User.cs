using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace Village_Manager.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Username không được để trống")]
        [MaxLength(100, ErrorMessage = "Username tối đa 100 ký tự")]
        [Column("username")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password không được để trống")]
        [MaxLength(255, ErrorMessage = "Password tối đa 255 ký tự")]
        [Column("password")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [MaxLength(100, ErrorMessage = "Email tối đa 100 ký tự")]
        [Column("email")]
        public string Email { get; set; }

        [Required]
        [Column("role_id")]
        public int RoleId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        //// Navigation property (liên kết tới bảng Roles nếu có model Role)
        //[ForeignKey("RoleId")]
        //public virtual Role Role { get; set; }
    }
}
