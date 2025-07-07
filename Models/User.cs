using System;
using System.Collections.Generic;

namespace Village_Manager.Models;

public partial class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string Email { get; set; } = null!;

    public int RoleId { get; set; }

    public bool HasAcceptedGeolocation { get; set; } = false;

    public string? Phone { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();

    public virtual Cart? Cart { get; set; }

    public virtual ICollection<FarmerRegistrationRequest> FarmerRegistrationRequestReviewedByNavigations { get; set; } = new List<FarmerRegistrationRequest>();

    public virtual ICollection<FarmerRegistrationRequest> FarmerRegistrationRequestUsers { get; set; } = new List<FarmerRegistrationRequest>();

    public virtual ICollection<Farmer> Farmers { get; set; } = new List<Farmer>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Log> Logs { get; set; } = new List<Log>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<RetailOrder> RetailOrders { get; set; } = new List<RetailOrder>();

    public virtual ICollection<ReturnOrder> ReturnOrders { get; set; } = new List<ReturnOrder>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

    public virtual ICollection<ShipperRegistrationRequest> ShipperRegistrationRequestReviewedByNavigations { get; set; } = new List<ShipperRegistrationRequest>();

    public virtual ICollection<ShipperRegistrationRequest> ShipperRegistrationRequestUsers { get; set; } = new List<ShipperRegistrationRequest>();

    public virtual ICollection<Shipper> Shippers { get; set; } = new List<Shipper>();

    public virtual ICollection<Staff> Staff { get; set; } = new List<Staff>();

    public virtual ICollection<SupplyRequest> SupplyRequestReceivers { get; set; } = new List<SupplyRequest>();

    public virtual ICollection<SupplyRequest> SupplyRequestRequesters { get; set; } = new List<SupplyRequest>();

    public virtual ICollection<UserLocation> UserLocations { get; set; } = new List<UserLocation>();
}
