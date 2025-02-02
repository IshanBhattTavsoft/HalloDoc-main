﻿using HalloDoc.DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
namespace HalloDoc.DataLayer.ViewModels;
public class AdminDashboardTableView
{
    public int new_count { get; set; }
    public int pending_count { get; set; }
    public int active_count { get; set; }
    public int conclude_count { get; set; }
    public int toclose_count { get; set; }
    public int unpaid_count { get; set; }
    //public AdminNavbarViewModel? adminNavbarViewModel { get; set; }
    public List<Request> requests { get; set; } = new List<Request>();
    public IQueryable<Request> query_requests { get; set; }
    public List<Region> regions { get; set; } = new List<Region>();
    public string status { get; set; }
    public string PatientName { get; set; }
    public string? Reason { get; set; }
    public string? AdditionalNotes { get; set; }
    public int RequestId { get; set; }
    public List<CaseTag> caseTags { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    [RegularExpression(@"^[1-9]\d{9}$", ErrorMessage = "Please enter valid phone number")]
    public string PhoneNumber { get; set; }
    [Required(ErrorMessage = "Email is required")]
    public required string email { get; set; }
    public int? requestTypeId { get; set; }
    [Required(ErrorMessage = "Please enter the Email")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Please enter valid Email")]
    public string sendAgreeEmail { get; set; }
    public AdminNavbarModel? an { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public DateOnly? waitTime { get; set; }
    public string? sendAgreementEmail { get; set; }
    public string? providerTransferDescription { get; set; }
    public string cancelCaseNotes { get; set; }
    public string phoneNo { get; set; }
    public string requestDTY { get; set; }
    public int? aspNetUserId { get; set; }
    public List<AspNetUser> anu { get; set; }
    public List<Physician> allPhysicians { get; set; }
}
