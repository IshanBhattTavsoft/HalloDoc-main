﻿using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Data;
using ClosedXML.Excel;
using HalloDoc.DataLayer.Models;
using HalloDoc.DataLayer.ViewModels;
using DocumentFormat.OpenXml.InkML;
using System.Globalization;
using DocumentFormat.OpenXml.Office2010.Excel;
using HalloDocMvc.Entity.ViewModel;
using Microsoft.Office.Interop.Excel;
using HalloDoc.LogicLayer.Patient_Interface;
using static HalloDoc.DataLayer.Models.Enums;
using System.Collections;
using HalloDoc.LogicLayer.Patient_Repository;
using System.Net.Mail;
using System.Net;
using DocumentFormat.OpenXml.Spreadsheet;
//using System.Diagnostics;
//using HalloDoc.Data;

namespace HalloDoc.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAdminInterface _adminInterface;
        private readonly IHttpContextAccessor _sescontext;
        private readonly IJwtToken _jwtToken;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IAdminInterface adminInterface, IHttpContextAccessor sescontext, IJwtToken jwtToken)
        {
            _adminInterface = adminInterface;
            _sescontext = sescontext;
            _jwtToken = jwtToken;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PlatformLoginPage(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                AspNetUser user = new AuthManager().Login(model.UserName, model.PasswordHash);
                if (user != null)
                {
                    var token = _jwtToken.GenerateJwtToken(user);
                    if (model.PasswordHash == user.PasswordHash)
                    {
                        Admin ad = _adminInterface.ValidateUser(model);
                        HttpContext.Session.SetInt32("id", ad.AdminId);
                        HttpContext.Session.SetString("name", ad.FirstName);
                        Response.Cookies.Append("token", token.ToString());
                        HttpContext.Session.SetString("IsLoggedIn", "true");
                        
                    }
                    else
                    {
                        ModelState.AddModelError("PasswordHash", "Incorrect Password");
                    }
                }
                else
                {
                    ModelState.AddModelError("Username", "Incorrect Username");
                }
            }

            return View(model);
        }

        public IActionResult Logout()
        {
            _sescontext.HttpContext.Session.Clear();
            Response.Cookies.Delete("token");
            return RedirectToAction("PatientLoginPage", "Login");
        }

        public IActionResult PlatformForgotPassword()
        {
            return View();
        }
        [CustomAuthorize("Admin")]
        public IActionResult AdminDashboard(string? status)
        {
            //var count_new = _context.Requests.Count(r => r.Status == 1);
            //var count_pending = _context.Requests.Count(r => r.Status == 2);
            //var count_active = _context.Requests.Count(r => r.Status == 3);
            //var count_conclude = _context.Requests.Count(r => r.Status == 4);
            //var count_toclose = _context.Requests.Count(r => r.Status == 5);
            //var count_unpaid = _context.Requests.Count(r => r.Status == 6);

            //AdminDashboardTableView adminDashboardViewModel = new AdminDashboardTableView
            //{
            //    new_count = count_new,
            //    pending_count = count_pending,
            //    active_count = count_active,
            //    conclude_count = count_conclude,
            //    unpaid_count = count_unpaid,
            //    toclose_count = count_toclose,
            //    query_requests = _context.Requests.Include(r => r.RequestWiseFiles).Include(r => r.Physician).Include(r => r.RequestStatusLogs).Where(r => r.Status == 1),
            //    requests = _context.Requests.Include(r => r.RequestClient).Include(r => r.Physician).Include(r => r.RequestStatusLogs).Where(r => r.Status == 1).ToList(),
            //    regions = _context.Regions.ToList(),
            //    status = "New",
            //    caseTags = _context.CaseTags.ToList()
            //};
            var userId = HttpContext.Session.GetInt32("id");
            AdminDashboardTableView adminDashboardViewModel = _adminInterface.ModelOfAdminDashboard(status, (int)userId);

            
            return View(adminDashboardViewModel);
        }



        //[HttpPost]
        [CustomAuthorize("Admin")]
        public IActionResult New()
        {
            var userId = HttpContext.Session.GetInt32("id");
            AdminDashboardTableView adminDashboardViewModel = _adminInterface.ModelOfAdminDashboard("New", (int)userId);
            return PartialView("AdminDashboardTablePartialView", adminDashboardViewModel);
        }

        //[HttpPost]
        [CustomAuthorize("Admin")]
        public IActionResult Pending()
        {
            var userId = HttpContext.Session.GetInt32("id");
            AdminDashboardTableView adminDashboardViewModel = _adminInterface.ModelOfAdminDashboard("Pending", (int)userId);

            return PartialView("AdminDashboardTablePartialView", adminDashboardViewModel);
        }

        //[HttpPost]
        [CustomAuthorize("Admin")]
        public IActionResult Active()
        {
            var userId = HttpContext.Session.GetInt32("id");
            AdminDashboardTableView adminDashboardViewModel = _adminInterface.ModelOfAdminDashboard("Active", (int)userId);

            return PartialView("AdminDashboardTablePartialView", adminDashboardViewModel);
        }

        //[HttpPost]
        [CustomAuthorize("Admin")]
        public IActionResult Conclude()
        {
            var userId = HttpContext.Session.GetInt32("id");
            AdminDashboardTableView adminDashboardViewModel = _adminInterface.ModelOfAdminDashboard("Conclude", (int)userId);

            return PartialView("AdminDashboardTablePartialView", adminDashboardViewModel);
        }

        //[HttpPost]
        [CustomAuthorize("Admin")]
        public IActionResult Toclose()
        {
            var userId = HttpContext.Session.GetInt32("id");
            AdminDashboardTableView adminDashboardViewModel = _adminInterface.ModelOfAdminDashboard("ToClose", (int)userId);

            return PartialView("AdminDashboardTablePartialView", adminDashboardViewModel);
        }

        //[HttpPost]
        [CustomAuthorize("Admin")]
        public IActionResult Unpaid()
        {
            var userId = HttpContext.Session.GetInt32("id");
            AdminDashboardTableView adminDashboardViewModel = _adminInterface.ModelOfAdminDashboard("Unpaid", (int)userId);

            return PartialView("AdminDashboardTablePartialView", adminDashboardViewModel);
        }
        [CustomAuthorize("Admin")]
        public List<Request> GetTableData()
        {
            List<Request> data = new List<Request>();
            //var user_id = HttpContext.Session.GetInt32("id");
            //data = _context.Requests.Include(r => r.RequestClient).Where(u => u.UserId == user_id).ToList();
            data = _adminInterface.GetRequestDataInList();
            return data;
        }

        [CustomAuthorize("Admin")]
        public IActionResult DownloadAll()
        {
            try
            {
                List<Request> data = GetTableData();
                var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Data");


                worksheet.Cell(1, 1).Value = "Name";
                worksheet.Cell(1, 2).Value = "Date Of Birth";
                worksheet.Cell(1, 3).Value = "Requestor";
                worksheet.Cell(1, 4).Value = "Physician Name";
                worksheet.Cell(1, 5).Value = "Date of Service";
                worksheet.Cell(1, 6).Value = "Requested Date";
                worksheet.Cell(1, 7).Value = "Phone Number";
                worksheet.Cell(1, 8).Value = "Address";
                worksheet.Cell(1, 9).Value = "Notes";

                int row = 2;
                foreach (var item in data)
                {
                    var statusClass = "";
                    var dos = "";
                    var notes = "";
                    if (item.RequestTypeId == 1)
                    {
                        statusClass = "business";
                    }
                    else if (item.RequestTypeId == 4)
                    {
                        statusClass = "patient";
                    }
                    else if (item.RequestTypeId == 2)
                    {
                        statusClass = "family";
                    }
                    else
                    {
                        statusClass = "concierge";
                    }
                    foreach (var stat in item.RequestStatusLogs)
                    {
                        if (stat.Status == 2)
                        {
                            dos = stat.CreatedDate.ToString("MMMM dd,yyyy");
                            notes = stat.Notes ?? "";
                        }
                    }
                    worksheet.Cell(row, 1).Value = item.RequestClient.FirstName + item.RequestClient.LastName;
                    worksheet.Cell(row, 2).Value = DateTime.Parse($"{item.RequestClient.IntYear}-{item.RequestClient.StrMonth}-{item.RequestClient.IntDate}").ToString("MMMM dd,yyyy");
                    worksheet.Cell(row, 3).Value = statusClass.Substring(0, 1).ToUpper() + statusClass.Substring(1).ToLower() + item.FirstName + item.LastName;
                    worksheet.Cell(row, 4).Value = ("Dr." + item?.Physician == null ? "" : item?.Physician?.FirstName);
                    worksheet.Cell(row, 5).Value = item.CreatedDate.ToString("MMMM dd,yyyy");
                    worksheet.Cell(row, 6).Value = dos;
                    worksheet.Cell(row, 7).Value = item.RequestClient.PhoneNumber + "(Patient)" + (item.RequestTypeId != 4 ? item.PhoneNumber + statusClass.Substring(0, 1).ToUpper() + statusClass.Substring(1).ToLower() : "");
                    worksheet.Cell(row, 8).Value = (item.RequestClient.Address == null ? item.RequestClient.Address + item.RequestClient.Street + item.RequestClient.City + item.RequestClient.State + item.RequestClient.ZipCode : item.RequestClient.Street + item.RequestClient.City + item.RequestClient.State + item.RequestClient.ZipCode);
                    worksheet.Cell(row, 9).Value = item.RequestClient.Notes;
                    row++;
                }
                worksheet.Columns().AdjustToContents();

                var memoryStream = new MemoryStream();
                workbook.SaveAs(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
                return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "data.xlsx");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        [CustomAuthorize("Admin")]
        public IActionResult ViewCase(int requestId)
        {
            //var data = _context.Requests.Include(u => u.RequestClient).FirstOrDefault(u => u.RequestId == requestId);
            //var user = _context.RequestClients.Include(v => v.Requests).FirstOrDefault(v => v.RequestClientId == data.RequestClientId);
            Request request = _adminInterface.ValidateRequest(requestId);
            RequestClient user = _adminInterface.ValidateRequestClient(request.RequestClientId);
            int intYear = (int)user.IntYear;
            int intDate = (int)user.IntDate;
            string month = user.StrMonth;
            int mon = 0;
            if (month.Length > 1)
            {

                if (month == "January")
                {
                    mon = 1;
                }
                else if (month == "February")
                {
                    mon = 2;
                }
                else if (month == "March")
                {
                    mon = 3;
                }
                else if (month == "April")
                {
                    mon = 4;
                }
                else if (month == "May")
                {
                    mon = 5;
                }
                else if (month == "June")
                {
                    mon = 6;
                }
                else if (month == "July")
                {
                    mon = 7;
                }
                else if (month == "August")
                {
                    mon = 8;
                }
                else if (month == "September")
                {
                    mon = 9;
                }
                else if (month == "October")
                {
                    mon = 10;
                }
                else if (month == "November")
                {
                    mon = 11;
                }
                else if (month == "December")
                {
                    mon = 12;
                }
            }
            int mon1 = 0;
            if (month.Length == 1)
            {
                mon1 = int.Parse(month);
            }
            DateTime date = new DateTime();
            if (month.Length == 1)
            {
                date = new DateTime(intYear, mon1, intDate);
            }
            if (month.Length > 1)
            {
                date = new DateTime(intYear, mon, intDate);
            }
            ViewCaseModel viewCase = new ViewCaseModel
            {
                RequestId = requestId,
                PatientNotes = user.Notes,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DOB = date,
                ConfirmationNo = request.ConfirmationNumber,
                reqTypeId = request.RequestTypeId,
                regions = _adminInterface.GetAllRegion(),
                Status = request.Status,
                caseTags = _adminInterface.GetAllCaseTags()
            };

            return View(viewCase);
        }


        [HttpPost]
        [CustomAuthorize("Admin")]
        public IActionResult EditViewCase(ViewCaseModel userProfile)
        {
            int requestId = (int)userProfile.RequestId;
            if (requestId != null)
            {
                Request rid = _adminInterface.ValidateRequest(requestId);
                RequestClient userToUpdate = _adminInterface.ValidateRequestClient(rid.RequestClientId);
                if (userToUpdate != null)
                {
                    //userToUpdate.FirstName = userProfile.FirstName;
                    //userToUpdate.LastName = userProfile.LastName;
                    //userToUpdate.PhoneNumber = userProfile.PhoneNumber;
                    //userToUpdate.Email = userProfile.Email;
                    //userToUpdate.IntDate = userProfile.DOB.Day;
                    //userToUpdate.IntYear = userProfile.DOB.Year;
                    //userToUpdate.StrMonth = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(userProfile.DOB.Month);
                    //_context.RequestClients.Update(userToUpdate);
                    //_context.SaveChanges();
                    _adminInterface.EditViewCaseAction(userProfile, userToUpdate);
                }
            }
            return RedirectToAction("ViewCase", new { requestId = requestId });
        }

        [CustomAuthorize("Admin")]
        public IActionResult ViewNotes(int requestId)
        {
            Request r = _adminInterface.ValidateRequest(requestId);
            RequestNote rn = _adminInterface.FetchRequestNote(requestId);
            RequestStatusLog rsl = _adminInterface.FetchRequestStatusLogs(requestId);

            int id = (int)rsl.PhysicianId;

            Physician py = _adminInterface.FetchPhysician(id);

            var viewModel = new ViewNotes
            {
                AdminNotes = rn.AdminNotes,
                PhysicianNotes = rn.PhysicianNotes,
                PhyName = py.FirstName,
                Notes = rsl.Notes,
                CreatedDate = rsl.CreatedDate,
                RequestId = requestId,
            };
            return View(viewModel);
        }

        [HttpPost]
        [CustomAuthorize("Admin")]
        public IActionResult EditViewNotes(ViewNotes model)
        {
            //int id = model.RequestId;
            RequestNote rn = _adminInterface.FetchRequestNote(model.RequestId);

            //rn.AdminNotes = model.AdminNotes;
            //_context.RequestNotes.Update(rn);
            //_context.SaveChanges();
            _adminInterface.EditViewNotesAction(rn, model);

            return RedirectToAction("ViewNotes", new { requestId = model.RequestId });

        }

        [HttpPost]
        [CustomAuthorize("Admin")]
        public IActionResult CancelCase(AdminDashboardTableView model, int selectedCaseTagId, string additionalNotes)
        {
            CaseTag ct = _adminInterface.FetchCaseTag(selectedCaseTagId);
            Request r = _adminInterface.ValidateRequest(model.RequestId);
            r.CaseTag = ct.Name;
            r.Status = 3;
            RequestStatusLog rs = new RequestStatusLog();
            rs.RequestId = model.RequestId;
            rs.Notes = additionalNotes;
            rs.Status = 3;
            rs.CreatedDate = DateTime.Now;
            //_context.RequestStatusLogs.Add(rs);
            //_context.SaveChanges();
            _adminInterface.AddRequestStatusLogFromCancelCase(rs);
            TempData["success"] = "Case cancelled successfully";

            return RedirectToAction("AdminDashboard");
        }

        [CustomAuthorize("Admin")]
        public List<Physician> GetPhysicianByRegion(AdminDashboardTableView model, int RegionId)
        {
            List<Physician> p = _adminInterface.FetchPhysicianByRegion(RegionId);
            return p;
        }

        [CustomAuthorize("Admin")]
        public int sendAgreement2(int reqId)
        {
            Request r = _adminInterface.GetReqFromReqType(reqId);
            return r.RequestTypeId;
        }

        [HttpPost]
        [CustomAuthorize("Admin")]
        public IActionResult AssignCaseSubmitAction(AdminDashboardTableView model, string assignCaseDescription, int selectedPhysicianId)
        {
            RequestStatusLog rsl = new RequestStatusLog();
            Request r = _adminInterface.ValidateRequest(model.RequestId);
            r.Status = 2; //when a case is assigned, status is set to 1 currently
            // but when the assigned case gets accepted, then its status can be 2 and will be shown in Pending state.
            r.PhysicianId = selectedPhysicianId;
            rsl.RequestId = model.RequestId;
            rsl.Notes = assignCaseDescription;
            rsl.Status = 2;
            rsl.CreatedDate = DateTime.Now;
            rsl.TransToPhysicianId = selectedPhysicianId;
            rsl.PhysicianId = selectedPhysicianId;
            //_context.RequestStatusLogs.Add(rsl);
            //_context.SaveChanges();
            _adminInterface.AddRequestStatusLogFromCancelCase(rsl);
            _adminInterface.UpdateRequest(r);
            TempData["success"] = "Successfully requested to assign the case";
            return RedirectToAction("AdminDashboard");
        }

        [HttpPost]
        [CustomAuthorize("Admin")]
        public IActionResult TransferCaseSubmitAction(AdminDashboardTableView model, string assignCaseDescription, int selectedPhysicianId)
        {
            RequestStatusLog rsl = new RequestStatusLog();
            Request r = _adminInterface.ValidateRequest(model.RequestId);
            r.Status = 2; //when a case is assigned, status is set to 1 currently
            // but when the assigned case gets accepted, then its status can be 2 and will be shown in Pending state.
            r.PhysicianId = selectedPhysicianId;
            rsl.RequestId = model.RequestId;
            rsl.Notes = assignCaseDescription;
            rsl.Status = 2;
            rsl.CreatedDate = DateTime.Now;
            rsl.TransToPhysicianId = selectedPhysicianId;
            //rsl.PhysicianId = selectedPhysicianId;
            //_context.RequestStatusLogs.Add(rsl);
            //_context.SaveChanges();
            _adminInterface.AddRequestStatusLogFromCancelCase(rsl);
            _adminInterface.UpdateRequest(r);
            TempData["success"] = "Case transferred!!";
            return RedirectToAction("AdminDashboard");
        }

        [HttpPost]
        [CustomAuthorize("Admin")]
        public IActionResult ClearCaseSubmitAction(AdminDashboardTableView model)
        {
            Request r = _adminInterface.GetReqFromModel(model);
            if(r!=null)
            {
                r.Status = 10;
                TempData["success"] = "Case cleared successfully";
                _adminInterface.UpdateRequest(r);
            }
            return RedirectToAction("AdminDashboard");
        }

        [HttpPost]
        [CustomAuthorize("Admin")]
        public IActionResult BlockCase(AdminDashboardTableView model, string reasonForBlockRequest)
        {
            Request r = _adminInterface.ValidateRequest(model.RequestId);
            RequestStatusLog rs = new RequestStatusLog();
            r.Status = 11;
            rs.Status = 11;
            rs.CreatedDate = DateTime.Now;
            rs.Notes = reasonForBlockRequest;
            rs.RequestId = model.RequestId;
            //_context.RequestStatusLogs.Add(rs);
            //_context.SaveChanges();
            _adminInterface.AddRequestStatusLogFromCancelCase(rs);
            BlockRequest br = new BlockRequest();
            br.RequestId = model.RequestId;
            br.Email = r.Email;
            br.IsActive = new BitArray(1, true);
            br.Reason = reasonForBlockRequest;
            br.CreatedDate = DateTime.Now;
            _adminInterface.AddBlockRequestData(br);


            TempData["success"] = "Case blocked successfully";
            return RedirectToAction("AdminDashboard");
        }

        [HttpPost]
        [CustomAuthorize("Admin")]
        public async Task<IActionResult> CreateRequest(AdminCreateRequestModel model)
        {
            var region = _adminInterface.ValidateRegion(model);
            if (region == null)
            {
                ModelState.AddModelError("State", "Currently we are not serving in this region");
                return View(model);
            }

            var blockedUser = _adminInterface.ValidateBlockRequest(model);
            if (blockedUser != null)
            {
                ModelState.AddModelError("Email", "This patient is blocked.");
                return View(model);
            }

            var existingUser = _adminInterface.ValidateAspNetUser(model);

            if (ModelState.IsValid)
            {
                _adminInterface.InsertDataOfRequest(model);
            }
            TempData["success"] = "Request created successfully";
            return View("CreateRequest");
        }

        [CustomAuthorize("Admin")]
        public IActionResult VerifyLocation(string state)
        {
            if (state == null)
            {
                return Json(new { isVerified = 2 });
            }
            bool isVerified = _adminInterface.VerifyLocation(state);
            if (isVerified)
            {
                return Json(new { isVerified = 1 });
            }
            else
            {
                return Json(new { isVerified = 2 });
            }
        }

        public IActionResult CreateRequest()
        {
            return View();
        }

        public IActionResult PlatformLoginPage()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult modal_check()
        {
            return View();
        }

        [CustomAuthorize("Admin")]
        public IActionResult ViewUploads(int requestid)
        {
            Request request = _adminInterface.ValidateRequest(requestid);
            User user = _adminInterface.ValidateUserByRequestId(request);
            List<RequestWiseFile> rwf = _adminInterface.GetFileData(requestid);
            ViewUploadsModel vum = new ViewUploadsModel()
            {
                confirmation_number = request.ConfirmationNumber,
                requestId = requestid,
                user = user,
                requestWiseFiles = rwf
            };
            return View(vum);
        }

        [HttpPost]
        [CustomAuthorize("Admin")]
        public IActionResult SetImageContent(ViewUploadsModel model, int requestId)
        {
            var request = _adminInterface.GetRequestWithUser(requestId);


            ViewUploadsModel viewModel = new ViewUploadsModel
            {
                ImageContent = model.ImageContent,
            };
            if (model.ImageContent != null && model.ImageContent.Length > 0)
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\uploads", model.ImageContent.FileName);
                using (var stream = System.IO.File.Create(filePath))
                {
                    model.ImageContent.CopyTo(stream);

                }
            }

            if (model.ImageContent != null)
            {
                RequestWiseFile requestWiseFile = new RequestWiseFile
                {

                    FileName = model.ImageContent.FileName,
                    CreatedDate = DateTime.Now,
                    RequestId = request.RequestId,
                    IsDeleted = new BitArray(1, false)
                };
                //_context.RequestWiseFiles.Add(requestWiseFile);
                //_context.SaveChanges();
                _adminInterface.AddFile(requestWiseFile);
            }

            return RedirectToAction("ViewUploads", new { requestID = model.requestId });
        }

        [CustomAuthorize("Admin")]
        public IActionResult DeleteIndividual(int id)
        {
            int reqId = _adminInterface.SingleDelete(id);
            return RedirectToAction("ViewUploads", new { requestID = reqId });
        }

        [CustomAuthorize("Admin")]
        public IActionResult DeleteMultiple(int requestid, string fileId)
        {
            _adminInterface.MultipleDelete(requestid, fileId);
            TempData["success"] = "File(s) deleted successfully";
            return RedirectToAction("ViewUploads", new { requestID = requestid });
        }

        [CustomAuthorize("Admin")]
        public IActionResult SendSelectedFiles(int requestid, string fileName)
        {
            try
            {
                string[] files = fileName.Split(',').Select(x => x.Trim()).ToArray();
                string senderEmail = "tatva.dotnet.ishanbhatt@outlook.com";
                string senderPassword = "Ishan@1503";

                SmtpClient client = new SmtpClient("smtp.office365.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };
                Request r = _adminInterface.ValidateRequest(requestid);
                RequestClient rc = _adminInterface.ValidateRequestClient(r.RequestClientId);
                string name = rc.FirstName + " " + rc.LastName;


                MailMessage mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, "HalloDoc"),
                    Subject = "Documents uploaded for patient request",
                    IsBodyHtml = true,
                    Body = $"<h2>Documents</h2><p>Here are the documents uploaded for the request of Patient: {name}</p>"
                };
                var user = _adminInterface.ValidAspNetUser(rc.Email);
                foreach (var file in files)
                {
                    var filePath = Path.Combine("wwwroot/uploads", file);
                    var attachment = new Attachment(filePath);
                    mailMessage.Attachments.Add(attachment);
                }
                if (user != null)
                {
                    mailMessage.To.Add(rc.Email);
                    client.SendMailAsync(mailMessage);
                    TempData["success"] = "Mail sent successfully";
                    return RedirectToAction("ViewUploads", new { requestID = requestid });
                }
                else
                {
                    ModelState.AddModelError("Email", "Invalid Email");
                    TempData["error"] = "Unable to send the mail";
                    return RedirectToAction("ViewUploads", new { requestID = requestid });
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("ForgotPassword");
            }
        }

        [CustomAuthorize("Admin")]
        public async Task<IActionResult> SendLink(AdminDashboardTableView model)
        {

            try
            {

                string senderEmail = "tatva.dotnet.ishanbhatt@outlook.com";
                string senderPassword = "Ishan@1503";

                SmtpClient client = new SmtpClient("smtp.office365.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };
                string resetToken = Guid.NewGuid().ToString();
                string resetLink = $"{Request.Scheme}://{Request.Host}/Login/SubmitRequestScreen?token={resetToken}";

                

                MailMessage mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, "HalloDoc"),
                    Subject = "Create Request For Patient",
                    IsBodyHtml = true,
                    Body = $"Hey {model.FirstName + " " + model.LastName} !! Please click the following link to reset your password: <a href='{resetLink}'>Click Here</a>"
                };
                AspNetUser user = _adminInterface.ValidAspNetUser(model.email);
                if (user != null)
                {
                    mailMessage.To.Add(model.email);
                    await client.SendMailAsync(mailMessage);
                    TempData["success"] = "Mail Sent Successfully";
                    return RedirectToAction("AdminDashboard");
                }
                else
                {
                    ModelState.AddModelError("Email", "Invalid Email");
                    return RedirectToAction("AdminDashboard");
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("AdminDashboard");
            }
        }

        [CustomAuthorize("Admin")]
        public IActionResult Orders(int id)
        {
            List<HealthProfessionalType> hPT = _adminInterface.GetHealthProfessionalType();
            List<HealthProfessional> hP = _adminInterface.GetHealthProfessional();
            SendOrder so = new SendOrder
            {
                hpType = hPT,
                hp = hP,
                requestId = id
            };
            return View(so);
        }

        [CustomAuthorize("Admin")]
        public List<HealthProfessional> GetBusinessData(int professionId, SendOrder model)
        
        {
            List<HealthProfessional> healthProfessionals = _adminInterface.GetBusinessDataFromProfession(professionId);
            return healthProfessionals;
        }

        [CustomAuthorize("Admin")]
        public HealthProfessional GetOtherData(int businessId)
        {
            HealthProfessional hp = _adminInterface.GetOtherDataFromBId(businessId);
            return hp;
        }
        //[HttpPost]
        [CustomAuthorize("Admin")]
        public IActionResult GetAgreementData(int reqId)
        {
            RequestClient rc = _adminInterface.GetPatientData(reqId);
            return Json(new {response=rc });
        }

        [HttpPost]
        [CustomAuthorize("Admin")]
        public IActionResult SendOrder(SendOrder model, int vendorId, int noOfRefill)
        {
            OrderDetail orderDetail = new OrderDetail();
            orderDetail.VendorId = vendorId;
            orderDetail.RequestId = model.requestId;
            orderDetail.FaxNumber = model.faxNumber;
            orderDetail.Email = model.email;
            orderDetail.BusinessContact = model.businessContact;
            orderDetail.Prescription = model.prescription;
            orderDetail.NoOfRefill = noOfRefill;
            orderDetail.CreatedDate = DateTime.Now;
            _adminInterface.AddOrderDetails(orderDetail);
            TempData["success"] = "Order sent successfully";
            return RedirectToAction("AdminDashboard");
        }

        public async Task<IActionResult> SendMailForSetUpAccount(LoginViewModel model)
        {
            PasswordReset passwordReset = new PasswordReset();

            try
            {

                string senderEmail = "tatva.dotnet.ishanbhatt@outlook.com";
                string senderPassword = "Ishan@1503";

                SmtpClient client = new SmtpClient("smtp.office365.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };
                string resetToken = Guid.NewGuid().ToString();
                string resetLink = $"{Request.Scheme}://{Request.Host}/Admin/CreatePassword?token={resetToken}";

                passwordReset.Token = resetToken;
                passwordReset.CreatedDate = DateTime.Now;
                passwordReset.Email = model.UserName;
                passwordReset.IsModified = false;

                MailMessage mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, "HalloDoc"),
                    Subject = "Set up your Account",
                    IsBodyHtml = true,
                    Body = $"Please click the following link to reset your password: <a href='{resetLink}'>Click Here</a>"
                };
                var user = _adminInterface.ValidateAspNetUser(model);
                if (user != null)
                {
                    mailMessage.To.Add(model.UserName);
                    _sescontext.HttpContext.Session.SetString("Token", resetToken);
                    _sescontext.HttpContext.Session.SetString("UserEmail", model.UserName);
                    await client.SendMailAsync(mailMessage);
                    TempData["success"] = "Mail sent successfully. Please check it";
                    return RedirectToAction("PlatfromLoginPage");
                }
                else
                {
                    ModelState.AddModelError("Email", "Invalid Email");
                    return RedirectToAction("PlatformForgotPassword");
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("PlatformForgotPassword");
            }
        }

        public async Task<IActionResult> SendMailOfAgreement(AdminDashboardTableView model)
        {
            string email = _adminInterface.GetMailToSentAgreement(model.RequestId);
            RequestClient rc = _adminInterface.GetPatientData(model.RequestId);
            string url = $"{Request.Scheme}://{Request.Host}/Admin/ReviewAgreement?id={rc.RequestClientId}";
            try
            {
                string senderEmail = "tatva.dotnet.ishanbhatt@outlook.com";
                string senderPassword = "Ishan@1503";

                SmtpClient client = new SmtpClient("smtp.office365.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                MailMessage mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, "HalloDoc"),
                    Subject = "Review the agreement",
                    IsBodyHtml = true,
                    Body = $"Please click the following link to reset your password: <br><br><a href='{url}'>Click Here</a>"
                };

                AspNetUser anu = _adminInterface.ValidAspNetUser(email);
                if (anu != null)
                {
                    mailMessage.To.Add(anu.Email);
                    _sescontext.HttpContext.Session.SetString("UserEmail", anu.Email);
                    await client.SendMailAsync(mailMessage);
                    TempData["success"] = "Mail sent successfully. Please check it";
                    return RedirectToAction("AdminDashboard");
                }
                else
                {
                    ModelState.AddModelError("Email", "Invalid Email");
                    return RedirectToAction("AdminDashboard");
                }

            }
            catch (Exception ex)
            {
                TempData["error"] = "Failed to send the agreement to the provided mail";
                return RedirectToAction("AdminDashboard");
            }
        }

        [HttpPost]
        public IActionResult PlatformCreatePassword(string token)
        {

            var useremail = _sescontext.HttpContext.Session.GetString("Token");
            PasswordReset pr = _adminInterface.ValidateToken(token);

            if (pr == null || pr.IsModified == true)
            {
                return NotFound();
            }

            TimeSpan diff = DateTime.Now.Subtract(pr.CreatedDate);
            double hours = diff.TotalHours;
            if (hours > 24)
            {
                return NotFound();
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {

            var useremail = _sescontext.HttpContext.Session.GetString("UserEmail");
            AspNetUser user = _adminInterface.ValidateUserForResetPassword(model, useremail);
            if (user != null && model.Password == model.ConfirmPassword)
            {
                //user.PasswordHash = model.Password;
                //_context.SaveChanges();
                _adminInterface.SetPasswordForResetPassword(user, model);
                return RedirectToAction("PlatformLoginPage");
            }
            else
            {
                ModelState.AddModelError("Password", "Password Missmatched");
                return RedirectToAction("PlatformForgotPassword");
            }

        }

        public IActionResult ReviewAgreement(int id)
        {
            RequestClient rc = _adminInterface.GetRequestClientFromId(id);
            return View(rc);
        }

        public IActionResult AcceptAgreement(int id)
        {
            Request r = _adminInterface.GetReqFromReqClient(id);
            RequestStatusLog rsl = new RequestStatusLog();
            rsl.RequestId = r.RequestId;
            rsl.Status = 4;
            rsl.CreatedDate = DateTime.Now;
            rsl.RequestId = r.RequestId;
            _adminInterface.AddRequestStatusLogFromAgreement(rsl);
            _adminInterface.UpdateRequest(r);
            TempData["success"] = "Agreement accepted successfully";
            return RedirectToAction("PatientLoginPage", "Login");
        }

        public IActionResult CancelAgreement(int id, string desc)
        {
            Request r = _adminInterface.GetReqFromReqClient(id);
            RequestStatusLog rsl = _adminInterface.GetLogFromReqId(r.RequestId);
            rsl.Status = 3;
            rsl.Notes = desc;
            rsl.CreatedDate = DateTime.Now;
            rsl.RequestId = r.RequestId;
            _adminInterface.AddRequestStatusLogFromCancelCase(rsl);
            _adminInterface.UpdateRequest(r);
            TempData["success"] = "Agreement cancelled successfully";
            return RedirectToAction("PatientLoginPage", "Login");
        }

        public IActionResult PlatformCreatePassword()
        {
            return View();
        }
    }
}
