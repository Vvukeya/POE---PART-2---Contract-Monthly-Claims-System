using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using POE___PART_2___Contract_Monthly_Claims_System.Models;
using POE___PART_2___Contract_Monthly_Claims_System.Data;

namespace POE___PART_2___Contract_Monthly_Claims_System.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AllQueries _queries;
        private readonly IWebHostEnvironment _environment;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;

            try
            {
                _queries = new AllQueries();

                // Test database connection on startup
                if (!_queries.TestDatabaseConnection())
                {
                    _logger.LogWarning("Database connection test failed on startup.");
                }
                else
                {
                    _logger.LogInformation("Database connection established successfully.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize database queries.");
                throw;
            }
        }

        // Home Page
        public IActionResult Index()
        {
            return View();
        }

        // Login Page (GET)
        public IActionResult Login()
        {
            HttpContext.Session.Clear();
            return View();
        }

        // Login Page (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string role)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(role))
                {
                    TempData["Error"] = "Please fill in all fields.";
                    return View();
                }

                var user = await _queries.GetUserByCredentialsAsync(email, password, role);

                if (user != null)
                {
                    HttpContext.Session.SetInt32("UserId", user.UserID);
                    HttpContext.Session.SetString("UserRole", user.Role);
                    HttpContext.Session.SetString("UserName", $"{user.FirstName} {user.Surname}");
                    HttpContext.Session.SetString("UserEmail", user.Email);

                    TempData["Success"] = $"Welcome back, {user.FirstName}!";

                    return role switch
                    {
                        "Lecturer" => RedirectToAction("LecturerDashboard"),
                        "Coordinator" => RedirectToAction("CoordinatorDashboard"),
                        "Manager" => RedirectToAction("ManagerDashboard"),
                        _ => RedirectToAction("Index")
                    };
                }
                else
                {
                    TempData["Error"] = "Invalid email, password, or role selection. Please try again or register.";
                    return View();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                TempData["Error"] = "An error occurred during login. Please try again.";
                return View();
            }
        }

        // Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "You have been logged out successfully.";
            return RedirectToAction("Index");
        }

        // Check if user is logged in and has correct role
        private bool IsUserLoggedIn(string requiredRole)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");
            return userId.HasValue && userRole == requiredRole;
        }

        // Lecturer Dashboard
        public async Task<IActionResult> LecturerDashboard()
        {
            if (!IsUserLoggedIn("Lecturer"))
            {
                TempData["Error"] = "Please login as a Lecturer to access this page.";
                return RedirectToAction("Login");
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            var user = await _queries.GetUserByIdAsync(userId.Value);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Logout");
            }

            ViewBag.UserName = $"{user.FirstName} {user.Surname}";
            ViewBag.UserEmail = user.Email;

            // Get dashboard statistics
            var totalClaims = await _queries.GetClaimsCountAsync(userId);
            var pendingClaims = await _queries.GetClaimsCountAsync(userId, "Pending");
            var approvedClaims = await _queries.GetClaimsCountAsync(userId, "Approved");

            ViewBag.TotalClaims = totalClaims;
            ViewBag.PendingClaims = pendingClaims;
            ViewBag.ApprovedClaims = approvedClaims;

            // Get recent claims
            var recentClaims = await _queries.GetClaimsByUserIdAsync(userId.Value);
            ViewBag.RecentClaims = recentClaims.Take(5).ToList();

            return View();
        }

        // Submit Claim (GET)
        public IActionResult LecturerSubmitClaim()
        {
            if (!IsUserLoggedIn("Lecturer"))
            {
                TempData["Error"] = "Please login as a Lecturer to access this page.";
                return RedirectToAction("Login");
            }

            return View();
        }

        // Submit Claim (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LecturerSubmitClaim(Claim claim, List<IFormFile> files)
        {
            if (!IsUserLoggedIn("Lecturer"))
            {
                TempData["Error"] = "Please login as a Lecturer to access this page.";
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var userId = HttpContext.Session.GetInt32("UserId");
                    if (userId == null)
                    {
                        TempData["Error"] = "User session expired. Please login again.";
                        return RedirectToAction("Login");
                    }

                    claim.UserID = userId.Value;
                    claim.Status = "Pending";
                    claim.SubmissionDate = DateTime.Now;

                    // First create the claim to get ClaimID
                    claim.ClaimID = await _queries.AddClaimAsync(claim);

                    // Handle file uploads
                    if (files != null && files.Count > 0)
                    {
                        foreach (var file in files)
                        {
                            if (file.Length > 0)
                            {
                                var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx" };
                                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                                if (!allowedExtensions.Contains(fileExtension))
                                {
                                    ModelState.AddModelError("", $"File type {fileExtension} is not allowed. Only PDF, DOCX, and XLSX files are permitted.");
                                    return View(claim);
                                }

                                if (file.Length > 5 * 1024 * 1024)
                                {
                                    ModelState.AddModelError("", $"File {file.FileName} is too large. Maximum file size is 5MB.");
                                    return View(claim);
                                }

                                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                                if (!Directory.Exists(uploadsFolder))
                                    Directory.CreateDirectory(uploadsFolder);

                                var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }

                                var document = new Document
                                {
                                    ClaimID = claim.ClaimID,
                                    FileName = file.FileName,
                                    FileType = fileExtension,
                                    FilePath = uniqueFileName,
                                    FileSize = file.Length,
                                    UploadDate = DateTime.Now
                                };

                                await _queries.AddDocumentAsync(document);
                            }
                        }
                    }

                    TempData["Success"] = $"Claim submitted successfully! Your claim ID is #{claim.ClaimID}.";
                    return RedirectToAction("LecturerTrack");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error submitting claim");
                    ModelState.AddModelError("", "An error occurred while submitting the claim. Please try again.");
                }
            }

            return View(claim);
        }

        // Track Claims
        public async Task<IActionResult> LecturerTrack()
        {
            if (!IsUserLoggedIn("Lecturer"))
            {
                TempData["Error"] = "Please login as a Lecturer to access this page.";
                return RedirectToAction("Login");
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            var claims = await _queries.GetClaimsByUserIdAsync(userId.Value);

            // Load documents for each claim
            foreach (var claim in claims)
            {
                claim.Documents = await _queries.GetDocumentsByClaimIdAsync(claim.ClaimID);
            }

            return View(claims);
        }

        // Edit/Resubmit Claim (GET)
        public async Task<IActionResult> LecturerEditClaim(int id)
        {
            if (!IsUserLoggedIn("Lecturer"))
            {
                TempData["Error"] = "Please login as a Lecturer to access this page.";
                return RedirectToAction("Login");
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            var claim = await _queries.GetClaimByIdAsync(id, userId.Value);

            if (claim == null)
            {
                TempData["Error"] = "Claim not found or you don't have permission to edit this claim.";
                return RedirectToAction("LecturerTrack");
            }

            if (claim.Status != "Rejected")
            {
                TempData["Error"] = "You can only edit rejected claims.";
                return RedirectToAction("LecturerTrack");
            }

            claim.Documents = await _queries.GetDocumentsByClaimIdAsync(claim.ClaimID);

            return View(claim);
        }

        // Edit/Resubmit Claim (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LecturerEditClaim(int claimId, Claim updatedClaim, List<IFormFile> files, string correctionNotes)
        {
            if (!IsUserLoggedIn("Lecturer"))
            {
                TempData["Error"] = "Please login as a Lecturer to access this page.";
                return RedirectToAction("Login");
            }

            var userId = HttpContext.Session.GetInt32("UserId");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingClaim = await _queries.GetClaimByIdAsync(claimId, userId.Value);

                    if (existingClaim == null)
                    {
                        TempData["Error"] = "Claim not found or you don't have permission to edit this claim.";
                        return RedirectToAction("LecturerTrack");
                    }

                    updatedClaim.ClaimID = claimId;
                    updatedClaim.UserID = userId.Value;
                    updatedClaim.CorrectionNotes = correctionNotes;
                    updatedClaim.Status = "Resubmitted";
                    updatedClaim.SubmissionDate = DateTime.Now;

                    if (files != null && files.Count > 0)
                    {
                        foreach (var file in files)
                        {
                            if (file.Length > 0)
                            {
                                var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx" };
                                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                                if (!allowedExtensions.Contains(fileExtension) || file.Length > 5 * 1024 * 1024)
                                    continue;

                                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                                if (!Directory.Exists(uploadsFolder))
                                    Directory.CreateDirectory(uploadsFolder);

                                var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }

                                var document = new Document
                                {
                                    ClaimID = claimId,
                                    FileName = file.FileName,
                                    FileType = fileExtension,
                                    FilePath = uniqueFileName,
                                    FileSize = file.Length,
                                    UploadDate = DateTime.Now
                                };

                                await _queries.AddDocumentAsync(document);
                            }
                        }
                    }

                    await _queries.UpdateClaimAsync(updatedClaim);
                    TempData["Success"] = $"Claim #{claimId} has been resubmitted successfully!";
                    return RedirectToAction("LecturerTrack");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating claim");
                    ModelState.AddModelError("", "An error occurred while updating the claim. Please try again.");
                }
            }

            return View(updatedClaim);
        }

        // Coordinator Dashboard
        public async Task<IActionResult> CoordinatorDashboard()
        {
            if (!IsUserLoggedIn("Coordinator"))
            {
                TempData["Error"] = "Please login as a Coordinator to access this page.";
                return RedirectToAction("Login");
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            var user = await _queries.GetUserByIdAsync(userId.Value);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Logout");
            }

            ViewBag.UserName = $"{user.FirstName} {user.Surname}";

            var totalPendingClaims = await _queries.GetClaimsCountAsync(null, "Pending") +
                                   await _queries.GetClaimsCountAsync(null, "Resubmitted");
            var totalVerifiedClaims = await _queries.GetClaimsCountAsync(null, "Verified");
            var totalClaims = await _queries.GetClaimsCountAsync();

            ViewBag.TotalPendingClaims = totalPendingClaims;
            ViewBag.TotalVerifiedClaims = totalVerifiedClaims;
            ViewBag.TotalClaims = totalClaims;

            return View();
        }

        // Coordinator Queue
        public async Task<IActionResult> CoordinatorQueue()
        {
            if (!IsUserLoggedIn("Coordinator"))
            {
                TempData["Error"] = "Please login as a Coordinator to access this page.";
                return RedirectToAction("Login");
            }

            var pendingClaims = await _queries.GetPendingClaimsAsync();

            // Load documents for each claim
            foreach (var claim in pendingClaims)
            {
                claim.Documents = await _queries.GetDocumentsByClaimIdAsync(claim.ClaimID);
            }

            return View(pendingClaims);
        }

        // Approve/Reject Claim (Coordinator)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CoordinatorApproveClaim(int claimId, string action, string notes = "")
        {
            if (!IsUserLoggedIn("Coordinator"))
            {
                TempData["Error"] = "Please login as a Coordinator to access this page.";
                return RedirectToAction("Login");
            }

            try
            {
                var claim = await _queries.GetClaimByIdAsync(claimId);

                if (claim != null)
                {
                    if (action == "approve")
                    {
                        await _queries.UpdateClaimStatusAsync(claimId, "Verified", notes);
                        TempData["Success"] = $"Claim #{claimId} from {claim.User?.FirstName} {claim.User?.Surname} has been verified successfully!";
                        _logger.LogInformation($"Coordinator verified claim #{claimId}");
                    }
                    else if (action == "reject")
                    {
                        if (string.IsNullOrEmpty(notes))
                        {
                            TempData["Error"] = "Please provide a reason for rejection.";
                            return RedirectToAction("CoordinatorQueue");
                        }

                        await _queries.UpdateClaimStatusAsync(claimId, "Rejected", notes);
                        TempData["Success"] = $"Claim #{claimId} from {claim.User?.FirstName} {claim.User?.Surname} has been rejected.";
                        _logger.LogInformation($"Coordinator rejected claim #{claimId}. Reason: {notes}");
                    }
                    else
                    {
                        TempData["Error"] = "Invalid action specified.";
                        return RedirectToAction("CoordinatorQueue");
                    }
                }
                else
                {
                    TempData["Error"] = "Claim not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing claim #{claimId} with action {action}");
                TempData["Error"] = "An error occurred while processing the claim. Please try again.";
            }

            return RedirectToAction("CoordinatorQueue");
        }

        // Manager Dashboard
        public async Task<IActionResult> ManagerDashboard()
        {
            if (!IsUserLoggedIn("Manager"))
            {
                TempData["Error"] = "Please login as a Manager to access this page.";
                return RedirectToAction("Login");
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            var user = await _queries.GetUserByIdAsync(userId.Value);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Logout");
            }

            ViewBag.UserName = $"{user.FirstName} {user.Surname}";

            var totalVerifiedClaims = await _queries.GetClaimsCountAsync(null, "Verified");
            var totalApprovedClaims = await _queries.GetClaimsCountAsync(null, "Approved");
            var totalRejectedClaims = await _queries.GetClaimsCountAsync(null, "Rejected");
            var totalClaims = await _queries.GetClaimsCountAsync();

            ViewBag.TotalVerifiedClaims = totalVerifiedClaims;
            ViewBag.TotalApprovedClaims = totalApprovedClaims;
            ViewBag.TotalRejectedClaims = totalRejectedClaims;
            ViewBag.TotalClaims = totalClaims;

            return View();
        }

        // Manager Queue
        public async Task<IActionResult> ManagerQueue()
        {
            if (!IsUserLoggedIn("Manager"))
            {
                TempData["Error"] = "Please login as a Manager to access this page.";
                return RedirectToAction("Login");
            }

            var verifiedClaims = await _queries.GetVerifiedClaimsAsync();

            // Load documents for each claim
            foreach (var claim in verifiedClaims)
            {
                claim.Documents = await _queries.GetDocumentsByClaimIdAsync(claim.ClaimID);
            }

            return View(verifiedClaims);
        }

        // Approve/Reject Claim (Manager)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManagerApproveClaim(int claimId, string action, string notes = "")
        {
            if (!IsUserLoggedIn("Manager"))
            {
                TempData["Error"] = "Please login as a Manager to access this page.";
                return RedirectToAction("Login");
            }

            try
            {
                var claim = await _queries.GetClaimByIdAsync(claimId);

                if (claim != null)
                {
                    if (action == "approve")
                    {
                        await _queries.UpdateClaimStatusAsync(claimId, "Approved", notes);
                        TempData["Success"] = $"Claim #{claimId} from {claim.User?.FirstName} {claim.User?.Surname} has been approved successfully!";
                        _logger.LogInformation($"Manager approved claim #{claimId}");
                    }
                    else if (action == "reject")
                    {
                        if (string.IsNullOrEmpty(notes))
                        {
                            TempData["Error"] = "Please provide a reason for rejection.";
                            return RedirectToAction("ManagerQueue");
                        }

                        await _queries.UpdateClaimStatusAsync(claimId, "Rejected", notes);
                        TempData["Success"] = $"Claim #{claimId} from {claim.User?.FirstName} {claim.User?.Surname} has been rejected.";
                        _logger.LogInformation($"Manager rejected claim #{claimId}. Reason: {notes}");
                    }
                    else
                    {
                        TempData["Error"] = "Invalid action specified.";
                        return RedirectToAction("ManagerQueue");
                    }
                }
                else
                {
                    TempData["Error"] = "Claim not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing manager action for claim #{claimId} with action {action}");
                TempData["Error"] = "An error occurred while processing the claim. Please try again.";
            }

            return RedirectToAction("ManagerQueue");
        }

        // View Document in Browser
        public async Task<IActionResult> ViewDocument(int documentId)
        {
            try
            {
                var documentInfo = await _queries.GetDocumentInfoAsync(documentId);
                if (documentInfo == null)
                {
                    return NotFound("Document not found.");
                }

                var fullPath = Path.Combine(_environment.WebRootPath, "uploads", documentInfo.FilePath);
                if (!System.IO.File.Exists(fullPath))
                {
                    return NotFound("Document file not found.");
                }

                // Return the file for viewing in browser
                var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
                return File(fileBytes, GetContentType(documentInfo.FileType));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error viewing document {documentId}");
                TempData["Error"] = "Error viewing document.";
            }

            return NotFound();
        }

        // Download Document
        public async Task<IActionResult> DownloadDocument(int documentId)
        {
            try
            {
                var documentInfo = await _queries.GetDocumentInfoAsync(documentId);
                if (documentInfo == null)
                {
                    return NotFound("Document not found.");
                }

                var fullPath = Path.Combine(_environment.WebRootPath, "uploads", documentInfo.FilePath);
                if (!System.IO.File.Exists(fullPath))
                {
                    return NotFound("Document file not found.");
                }

                var memory = new MemoryStream();
                using (var stream = new FileStream(fullPath, FileMode.Open))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                // For download with original filename
                return File(memory, GetContentType(documentInfo.FileType), documentInfo.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading document {documentId}");
                TempData["Error"] = "Error downloading document.";
            }

            return NotFound();
        }

        private string GetContentType(string fileType)
        {
            return fileType.ToLower() switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".doc" => "application/msword",
                ".xls" => "application/vnd.ms-excel",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }

        // Registration Page (GET)
        public IActionResult Register()
        {
            return View();
        }

        // Registration (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _queries.UserExistsAsync(user.Email);
                    if (existingUser)
                    {
                        ModelState.AddModelError("Email", "Email address is already registered. Please use a different email or login.");
                        return View(user);
                    }

                    user.RegistrationDate = DateTime.Now;
                    var result = await _queries.AddUserAsync(user);

                    if (result)
                    {
                        TempData["Success"] = $"Registration successful! You can now login as {user.Role}.";
                        return RedirectToAction("Login");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Failed to register user. Please try again.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during registration");
                    ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                }
            }

            return View(user);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}