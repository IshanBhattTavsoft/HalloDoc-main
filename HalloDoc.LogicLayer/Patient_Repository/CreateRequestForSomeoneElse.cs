﻿using HalloDoc.DataLayer.Models;
using HalloDoc.DataLayer.ViewModels;
using HalloDoc.LogicLayer.Patient_Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalloDoc.LogicLayer.Patient_Repository
{
    public class CreateRequestForSomeoneElse : ICreateRequestForSomeoneElse
    {
        private readonly ApplicationDbContext _context;
        public CreateRequestForSomeoneElse(ApplicationDbContext context)
        {
            _context = context;
        }

        public Region ValidateRegion(PatientRequestSomeoneElse model)
        {
            string state = model.State.Trim();
            return _context.Regions.FirstOrDefault(r => r.Name == state);
        }

        public User ValidateUser(PatientRequestSomeoneElse model, int user_id)
        {
            return _context.Users.FirstOrDefault(u => u.UserId == user_id);
        }

        public BlockRequest CheckForBlockedRequest(PatientRequestSomeoneElse model)
        {
            return _context.BlockRequests.FirstOrDefault(u => u.Email == model.Email);
        }

        public void RequestForSomeoneElse(PatientRequestSomeoneElse model, int user, User users, Region region1)
        {
            Request request = new Request();
            DataLayer.Models.Region region = new DataLayer.Models.Region();
            RequestClient requestClient = new RequestClient();
            RequestWiseFile requestWiseFile = new RequestWiseFile();
            RequestStatusLog requestStatusLog = new RequestStatusLog();

            requestClient.FirstName = model.FirstName;
            requestClient.LastName = model.LastName;
            requestClient.PhoneNumber = model.PhoneNumber;
            requestClient.Location = model.City;
            requestClient.Address = model.Street;
            requestClient.RegionId = 1;
            if (model.Symptoms != null)
            {
                requestClient.Notes = model.Symptoms;
            }
            requestClient.Email = model.Email;
            requestClient.IntDate = model.DOB.Day;
            requestClient.StrMonth = model.DOB.Month.ToString();
            requestClient.IntYear = model.DOB.Year;
            requestClient.Street = model.Street;
            requestClient.City = model.City;
            requestClient.State = model.State;
            requestClient.ZipCode = model.ZipCode;
            _context.RequestClients.Add(requestClient);
            _context.SaveChanges();

            int requests = _context.Requests.Where(u => u.CreatedDate.Date == DateTime.Now.Date).Count();
            Region r = _context.Regions.Where(re => re.Name.ToLower() == model.State).FirstOrDefault();
            string ConfirmationNumber = string.Concat(r.Abbreviation, DateTime.Now.Date.ToString("yyyyMMdd").Substring(0, 4), model.LastName.Substring(0, 2).ToUpper(), model.FirstName.Substring(0, 2).ToUpper(), requests.ToString("D" + 4));
            request.RequestTypeId = 1;

            request.CreatedUserId = users.UserId;
            request.FirstName = users.FirstName;
            request.LastName = users.LastName;
            request.Email = users.Email;
            request.PhoneNumber = users.Mobile;
            request.Status = 1;
            request.CreatedDate = DateTime.Now;
            request.RequestClientId = requestClient.RequestClientId;
            request.ConfirmationNumber = ConfirmationNumber;
            request.RelationName = model.Relation;
            request.IsDeleted = new System.Collections.BitArray(1, false);
            _context.Requests.Add(request);
            _context.SaveChangesAsync();

            if (model.File != null)
            {
                requestWiseFile.RequestId = request.RequestId;
                requestWiseFile.FileName = model.File.FileName;
                requestWiseFile.CreatedDate = DateTime.Now;
                requestWiseFile.IsDeleted = new System.Collections.BitArray(1, false);
                _context.RequestWiseFiles.Add(requestWiseFile);
                _context.SaveChangesAsync();
            }

           
            _context.SaveChangesAsync();
        }
    }
}
