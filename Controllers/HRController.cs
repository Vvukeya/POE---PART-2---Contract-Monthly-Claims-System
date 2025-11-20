using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using POE___PART_2___Contract_Monthly_Claims_System.Models;
using POE___PART_2___Contract_Monthly_Claims_System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Data.SqlClient;
using System.IO;

namespace POE___PART_2___Contract_Monthly_Claims_System.Controllers
{
    public class HRController : Controller
    {
        private readonly AllQueries _queries;
        private readonly IWebHostEnvironment _environment;

        public HRController(IWebHostEnvironment environment)
        {
            _environment = environment;
            _queries = new AllQueries();
        }

        private bool IsHRLoggedIn()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetString("UserId");
            return !string.IsNullOrEmpty(userId) && userRole == "HR";
        }

        // HR Dashboard
        public async Task<IActionResult> HRDashboard()
        {
            if (!IsHRLoggedIn())
            {
                TempData["Error"] = "Please login as HR to access this page.";
                return RedirectToAction("Login", "Home");
            }

            var userId = HttpContext.Session.GetString("UserId");
            var user = await _queries.GetUserByIdAsync(userId);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Logout", "Home");
            }

            ViewBag.UserName = $"{user.FirstName} {user.Surname}";

            // Get statistics for dashboard
            var totalLecturers = await GetLecturersCountAsync();
            var totalApprovedClaims = await _queries.GetClaimsCountAsync(null, "Approved");
            var totalClaims = await _queries.GetClaimsCountAsync();
            var pendingClaims = await _queries.GetClaimsCountAsync(null, "Pending");
            var verifiedClaims = await _queries.GetClaimsCountAsync(null, "Verified");
            var paidClaims = await GetPaidClaimsCountAsync();

            ViewBag.TotalLecturers = totalLecturers;
            ViewBag.TotalApprovedClaims = totalApprovedClaims;
            ViewBag.TotalClaims = totalClaims;
            ViewBag.PendingClaims = pendingClaims;
            ViewBag.VerifiedClaims = verifiedClaims;
            ViewBag.PaidClaims = paidClaims;

            return View();
        }

        // Manage Lecturers - View all lecturers
        public async Task<IActionResult> ManageLecturers()
        {
            if (!IsHRLoggedIn())
            {
                TempData["Error"] = "Please login as HR to access this page.";
                return RedirectToAction("Login", "Home");
            }

            var lecturers = await GetAllLecturersAsync();
            return View(lecturers);
        }

        // Edit Lecturer (GET)
        public async Task<IActionResult> EditLecturer(string id)
        {
            if (!IsHRLoggedIn())
            {
                TempData["Error"] = "Please login as HR to access this page.";
                return RedirectToAction("Login", "Home");
            }

            var lecturer = await _queries.GetUserByIdAsync(id);
            if (lecturer == null || lecturer.Role != "Lecturer")
            {
                TempData["Error"] = "Lecturer not found.";
                return RedirectToAction("ManageLecturers");
            }

            return View(lecturer);
        }

        // Edit Lecturer (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLecturer(string id, User lecturer)
        {
            if (!IsHRLoggedIn())
            {
                TempData["Error"] = "Please login as HR to access this page.";
                return RedirectToAction("Login", "Home");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var result = await UpdateLecturerAsync(lecturer);
                    if (result)
                    {
                        TempData["Success"] = $"Lecturer {lecturer.FirstName} {lecturer.Surname} updated successfully!";
                        return RedirectToAction("ManageLecturers");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Failed to update lecturer. Please try again.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while updating the lecturer.");
                }
            }

            return View(lecturer);
        }

        // Delete Lecturer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLecturer(string id)
        {
            if (!IsHRLoggedIn())
            {
                TempData["Error"] = "Please login as HR to access this page.";
                return RedirectToAction("Login", "Home");
            }

            try
            {
                var result = await DeleteLecturerAsync(id);
                if (result)
                {
                    TempData["Success"] = "Lecturer deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to delete lecturer. Please try again.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while deleting the lecturer.";
            }

            return RedirectToAction("ManageLecturers");
        }

        // Generate Reports - View all approved claims with totals
        public async Task<IActionResult> GenerateReports()
        {
            if (!IsHRLoggedIn())
            {
                TempData["Error"] = "Please login as HR to access this page.";
                return RedirectToAction("Login", "Home");
            }

            var allClaims = await _queries.GetAllClaimsAsync();
            var approvedClaims = allClaims.Where(c => c.Status == "Approved").ToList();

            // Calculate totals
            ViewBag.TotalAmount = approvedClaims.Sum(c => c.TotalAmount);
            ViewBag.TotalClaims = approvedClaims.Count;
            ViewBag.AverageAmount = approvedClaims.Any() ? approvedClaims.Average(c => c.TotalAmount) : 0;

            return View(approvedClaims);
        }

        // Generate Invoices - Group by lecturer for payment processing
        public async Task<IActionResult> GenerateInvoices()
        {
            if (!IsHRLoggedIn())
            {
                TempData["Error"] = "Please login as HR to access this page.";
                return RedirectToAction("Login", "Home");
            }

            var allClaims = await _queries.GetAllClaimsAsync();
            var approvedClaims = allClaims.Where(c => c.Status == "Approved").ToList();

            // Group by lecturer for invoice generation
            var lecturerInvoices = approvedClaims
                .GroupBy(c => new { c.UserID, c.User?.FirstName, c.User?.Surname, c.User?.Email })
                .Select(g => new LecturerInvoice
                {
                    UserID = g.Key.UserID,
                    LecturerName = $"{g.Key.FirstName} {g.Key.Surname}",
                    LecturerEmail = g.Key.Email,
                    TotalAmount = g.Sum(c => c.TotalAmount),
                    ClaimCount = g.Count(),
                    Claims = g.ToList(),
                    Period = $"{g.Min(c => c.Month)}/{g.Min(c => c.Year)} - {g.Max(c => c.Month)}/{g.Max(c => c.Year)}"
                })
                .ToList();

            ViewBag.GrandTotal = lecturerInvoices.Sum(i => i.TotalAmount);
            ViewBag.TotalInvoices = lecturerInvoices.Count;

            return View(lecturerInvoices);
        }

        // Generate PDF Invoice for a specific lecturer (all approved claims)
        public async Task<IActionResult> GenerateLecturerInvoice(string id)
        {
            if (!IsHRLoggedIn())
            {
                TempData["Error"] = "Please login as HR to access this page.";
                return RedirectToAction("Login", "Home");
            }

            try
            {
                var lecturer = await _queries.GetUserByIdAsync(id);
                if (lecturer == null || lecturer.Role != "Lecturer")
                {
                    TempData["Error"] = "Lecturer not found.";
                    return RedirectToAction("ManageLecturers");
                }

                var allClaims = await _queries.GetAllClaimsAsync();
                var approvedClaims = allClaims
                    .Where(c => c.UserID == id && c.Status == "Approved")
                    .ToList();

                if (!approvedClaims.Any())
                {
                    TempData["Error"] = "No approved claims found for this lecturer.";
                    return RedirectToAction("GenerateInvoices");
                }

                // Create a simple HTML invoice
                string htmlContent = GenerateInvoiceHtml(lecturer, approvedClaims);

                // Return HTML content that can be printed as PDF
                return Content(htmlContent, "text/html");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error generating invoice: " + ex.Message;
                return RedirectToAction("GenerateInvoices");
            }
        }
        // Download Invoice for a specific claim
        public async Task<IActionResult> DownloadInvoice(int claimId)
        {
            if (!IsHRLoggedIn())
            {
                TempData["Error"] = "Please login as HR to access this page.";
                return RedirectToAction("Login", "Home");
            }

            try
            {
                var claim = await _queries.GetClaimByIdAsync(claimId);
                if (claim == null)
                {
                    TempData["Error"] = "Claim not found.";
                    return RedirectToAction("ViewClaimStatuses");
                }

                var lecturer = await _queries.GetUserByIdAsync(claim.UserID);
                if (lecturer == null || lecturer.Role != "Lecturer")
                {
                    TempData["Error"] = "Lecturer not found.";
                    return RedirectToAction("ViewClaimStatuses");
                }

                var allClaims = await _queries.GetAllClaimsAsync();
                var paidClaims = allClaims
                    .Where(c => c.UserID == claim.UserID && c.ClaimID == claimId && c.Status == "Paid")
                    .ToList();

                if (!paidClaims.Any())
                {
                    TempData["Error"] = "No paid claim found for this invoice.";
                    return RedirectToAction("ViewClaimDetails", new { id = claimId });
                }

                // Create HTML invoice content
                string htmlContent = GenerateInvoiceHtml(lecturer, paidClaims);

                // Return as downloadable file with proper filename
                var bytes = System.Text.Encoding.UTF8.GetBytes(htmlContent);
                return File(bytes, "text/html", $"Invoice_{lecturer.UserID}_Claim_{claimId}_{DateTime.Now:yyyyMMdd}.html");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error downloading invoice: " + ex.Message;
                return RedirectToAction("ViewClaimDetails", new { id = claimId });
            }
        }

        // Updated HTML generator that works for single or multiple claims
        private string GenerateInvoiceHtml(User lecturer, List<Claim> claims)
        {
            double totalAmount = claims.Sum(c => c.TotalAmount);
            string period = claims.Count == 1
                ? $"{claims[0].Month}/{claims[0].Year}"
                : $"{claims.Min(c => c.Month)}/{claims.Min(c => c.Year)} - {claims.Max(c => c.Month)}/{claims.Max(c => c.Year)}";

            string claimDetails = string.Join("", claims.Select(claim => $@"
                    <tr>
                        <td>#{claim.ClaimID}</td>
                        <td>{claim.Month}/{claim.Year}</td>
                        <td>{claim.ModuleCode}</td>
                        <td>{claim.Hours}</td>
                        <td>{claim.HourlyRate.ToString("N2")}</td>
                        <td>{claim.TotalAmount.ToString("N2")}</td>
                    </tr>"));

            string html = $@"
    <!DOCTYPE html>
    <html>
    <head>
        <title>Lecturer Payment Invoice</title>
        <style>
            body {{ font-family: Arial, sans-serif; margin: 40px; line-height: 1.6; }}
            .header {{ text-align: center; margin-bottom: 30px; border-bottom: 2px solid #333; padding-bottom: 20px; }}
            .title {{ font-size: 24px; font-weight: bold; color: #2c3e50; }}
            .section {{ margin-bottom: 20px; }}
            .section-title {{ font-size: 18px; font-weight: bold; color: #34495e; margin-bottom: 10px; border-bottom: 1px solid #bdc3c7; padding-bottom: 5px; }}
            .info-row {{ display: flex; margin-bottom: 5px; }}
            .info-label {{ font-weight: bold; width: 150px; }}
            table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
            th, td {{ border: 1px solid #ddd; padding: 12px; text-align: left; }}
            th {{ background-color: #f2f2f2; font-weight: bold; }}
            .total-row {{ font-weight: bold; background-color: #e8f5e8; }}
            .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #bdc3c7; color: #7f8c8d; }}
            .action-buttons {{ display: flex; gap: 15px; margin-top: 20px; justify-content: center; }}
            .btn {{ padding: 12px 24px; border: none; border-radius: 5px; cursor: pointer; font-size: 16px; text-decoration: none; display: inline-block; }}
            .btn-primary {{ background: #1976d2; color: white; }}
            .btn-secondary {{ background: #6c757d; color: white; }}
            @media print {{
                body {{ margin: 20px; }}
                .no-print {{ display: none; }}
            }}
        </style>
    </head>
    <body>
        <div class='header'>
            <div class='title'>LECTURER PAYMENT INVOICE</div>
            <div>Contract Monthly Claims System</div>
        </div>

        <div class='section'>
            <div class='section-title'>Lecturer Information</div>
            <div class='info-row'>
                <div class='info-label'>Lecturer Name:</div>
                <div>{lecturer.FirstName} {lecturer.Surname}</div>
            </div>
            <div class='info-row'>
                <div class='info-label'>Lecturer ID:</div>
                <div>{lecturer.UserID}</div>
            </div>
            <div class='info-row'>
                <div class='info-label'>Email:</div>
                <div>{lecturer.Email}</div>
            </div>
            <div class='info-row'>
                <div class='info-label'>Phone:</div>
                <div>{lecturer.PhoneNumber}</div>
            </div>
            <div class='info-row'>
                <div class='info-label'>Address:</div>
                <div>{lecturer.Address}</div>
            </div>
        </div>

        <div class='section'>
            <div class='section-title'>Claims Summary</div>
            <div class='info-row'>
                <div class='info-label'>Period:</div>
                <div>{period}</div>
            </div>
            <div class='info-row'>
                <div class='info-label'>Total Claims:</div>
                <div>{claims.Count}</div>
            </div>
            <div class='info-row'>
                <div class='info-label'>Total Hours:</div>
                <div>{claims.Sum(c => c.Hours)}</div>
            </div>
        </div>

        <div class='section'>
            <div class='section-title'>Claim Details</div>
            <table>
                <thead>
                    <tr>
                        <th>Claim ID</th>
                        <th>Month/Year</th>
                        <th>Module Code</th>
                        <th>Hours</th>
                        <th>Rate (R)</th>
                        <th>Amount (R)</th>
                    </tr>
                </thead>
                <tbody>
                    {claimDetails}
                </tbody>
                <tfoot>
                    <tr class='total-row'>
                        <td colspan='5' style='text-align: right;'><strong>GRAND TOTAL:</strong></td>
                        <td><strong>R {totalAmount.ToString("N2")}</strong></td>
                    </tr>
                </tfoot>
            </table>
        </div>

        <div class='footer'>
            <div><strong>Generated on:</strong> {DateTime.Now.ToString("dd MMMM yyyy 'at' HH:mm")}</div>
            <div><strong>Status:</strong> {(claims.All(c => c.Status == "Paid") ? "PAID" : "APPROVED - Ready for Payment")}</div>
            <div>This is an automated invoice generated by CMCS System.</div>
            
            <div class='action-buttons no-print'>
                <button onclick='window.print()' class='btn btn-primary'>
                    🖨️ Print Invoice
                </button>
                <button onclick='window.close()' class='btn btn-secondary'>
                    ← Cancel & Close
                </button>
                <button onclick='window.history.back()' class='btn btn-secondary'>
                    ← Back to Previous
                </button>
            </div>
        </div>
    </body>
    </html>";

            return html;
        }

        // View Claim Statuses - Track all claims
        public async Task<IActionResult> ViewClaimStatuses()
        {
            if (!IsHRLoggedIn())
            {
                TempData["Error"] = "Please login as HR to access this page.";
                return RedirectToAction("Login", "Home");
            }

            var allClaims = await _queries.GetAllClaimsAsync();

            // Load documents for each claim
            foreach (var claim in allClaims)
            {
                claim.Documents = await _queries.GetDocumentsByClaimIdAsync(claim.ClaimID);
            }

            // Statistics for view
            ViewBag.TotalClaims = allClaims.Count;
            ViewBag.PendingClaims = allClaims.Count(c => c.Status == "Pending");
            ViewBag.VerifiedClaims = allClaims.Count(c => c.Status == "Verified");
            ViewBag.ApprovedClaims = allClaims.Count(c => c.Status == "Approved");
            ViewBag.RejectedClaims = allClaims.Count(c => c.Status == "Rejected");
            ViewBag.PaidClaims = allClaims.Count(c => c.Status == "Paid");

            return View(allClaims);
        }

        // View Claim Details
        public async Task<IActionResult> ViewClaimDetails(int id)
        {
            if (!IsHRLoggedIn())
            {
                TempData["Error"] = "Please login as HR to access this page.";
                return RedirectToAction("Login", "Home");
            }

            var claim = await _queries.GetClaimByIdAsync(id);
            if (claim == null)
            {
                TempData["Error"] = "Claim not found.";
                return RedirectToAction("ViewClaimStatuses");
            }

            // Load documents for the claim
            claim.Documents = await _queries.GetDocumentsByClaimIdAsync(claim.ClaimID);

            return View(claim);
        }

        // Process Payment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(int claimId)
        {
            if (!IsHRLoggedIn())
            {
                TempData["Error"] = "Please login as HR to access this page.";
                return RedirectToAction("Login", "Home");
            }

            try
            {
                var claim = await _queries.GetClaimByIdAsync(claimId);
                if (claim == null)
                {
                    TempData["Error"] = "Claim not found.";
                    return RedirectToAction("ViewClaimStatuses");
                }

                if (claim.Status != "Approved")
                {
                    TempData["Error"] = "Only approved claims can be paid.";
                    return RedirectToAction("ViewClaimDetails", new { id = claimId });
                }

                // Update claim status to Paid
                var hrUserName = HttpContext.Session.GetString("UserName");
                var updateResult = await UpdateClaimPaymentStatusAsync(claimId, "Paid", hrUserName);

                if (updateResult)
                {
                    TempData["Success"] = $"Payment processed successfully for Claim #{claimId}! Lecturer has been notified.";
                    return RedirectToAction("ViewClaimDetails", new { id = claimId });
                }
                else
                {
                    TempData["Error"] = "Failed to process payment. Please try again.";
                    return RedirectToAction("ViewClaimDetails", new { id = claimId });
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error processing payment: " + ex.Message;
                return RedirectToAction("ViewClaimDetails", new { id = claimId });
            }
        }

        // Export Payment Punch - CSV file for finance department
        // Export Payment Batch File for Finance Department
        public async Task<IActionResult> ExportPaymentBatch()
        {
            if (!IsHRLoggedIn())
            {
                TempData["Error"] = "Please login as HR to access this page.";
                return RedirectToAction("Login", "Home");
            }

            try
            {
                var allClaims = await _queries.GetAllClaimsAsync();
                var paidClaims = allClaims.Where(c => c.Status == "Paid").ToList();

                if (!paidClaims.Any())
                {
                    TempData["Error"] = "No paid claims found to export.";
                    return RedirectToAction("GenerateInvoices");
                }

                // Group by lecturer for batch processing
                var lecturerBatches = paidClaims
                    .GroupBy(c => new { c.UserID, c.User?.FirstName, c.User?.Surname, c.User?.Email })
                    .Select(g => new
                    {
                        LecturerID = g.Key.UserID,
                        LecturerName = $"{g.Key.FirstName} {g.Key.Surname}",
                        LecturerEmail = g.Key.Email,
                        TotalAmount = g.Sum(c => c.TotalAmount),
                        ClaimCount = g.Count(),
                        Claims = g.ToList()
                    })
                    .ToList();

                // Create batch file content (CSV format for finance systems)
                var batchContent = new System.Text.StringBuilder();

                // Batch Header
                batchContent.AppendLine("PAYMENT BATCH EXPORT - CONTRACT MONTHLY CLAIMS SYSTEM");
                batchContent.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                batchContent.AppendLine($"Total Lecturers: {lecturerBatches.Count}");
                batchContent.AppendLine($"Total Amount: R {lecturerBatches.Sum(l => l.TotalAmount):N2}");
                batchContent.AppendLine();

                // Batch Summary
                batchContent.AppendLine("BATCH SUMMARY");
                batchContent.AppendLine("Lecturer ID,Lecturer Name,Total Amount,Number of Claims,Payment Date");

                foreach (var lecturer in lecturerBatches)
                {
                    var latestPaymentDate = lecturer.Claims.Max(c => c.PaymentDate)?.ToString("yyyy-MM-dd") ?? "N/A";
                    batchContent.AppendLine(
                        $"{lecturer.LecturerID}," +
                        $"\"{lecturer.LecturerName}\"," +
                        $"{lecturer.TotalAmount}," +
                        $"{lecturer.ClaimCount}," +
                        $"{latestPaymentDate}"
                    );
                }

                batchContent.AppendLine();
                batchContent.AppendLine("DETAILED PAYMENT BREAKDOWN");
                batchContent.AppendLine("Lecturer ID,Lecturer Name,Claim ID,Period,Module Code,Hours,Rate,Amount,Payment Date,Paid By");

                // Detailed breakdown
                foreach (var lecturer in lecturerBatches)
                {
                    foreach (var claim in lecturer.Claims)
                    {
                        batchContent.AppendLine(
                            $"{lecturer.LecturerID}," +
                            $"\"{lecturer.LecturerName}\"," +
                            $"{claim.ClaimID}," +
                            $"{claim.Month}/{claim.Year}," +
                            $"{claim.ModuleCode}," +
                            $"{claim.Hours}," +
                            $"{claim.HourlyRate}," +
                            $"{claim.TotalAmount}," +
                            $"{claim.PaymentDate?.ToString("yyyy-MM-dd")}," +
                            $"\"{claim.PaidBy}\""
                        );
                    }
                }

                batchContent.AppendLine();
                batchContent.AppendLine("BATCH PROCESSING INSTRUCTIONS");
                batchContent.AppendLine("1. Use this file to process payments through your banking system");
                batchContent.AppendLine("2. Verify all amounts before processing");
                batchContent.AppendLine("3. Update payment status in CMCS after successful processing");
                batchContent.AppendLine("4. Retain this file for audit purposes");

                var bytes = System.Text.Encoding.UTF8.GetBytes(batchContent.ToString());
                return File(bytes, "text/csv", $"PaymentBatch_{DateTime.Now:yyyyMMdd_HHmm}.csv");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error exporting payment batch: " + ex.Message;
                return RedirectToAction("GenerateInvoices");
            }
        }

        // Export Reports to Excel
        public async Task<IActionResult> ExportReportsToExcel(string status = "Approved")
        {
            if (!IsHRLoggedIn())
            {
                TempData["Error"] = "Please login as HR to access this page.";
                return RedirectToAction("Login", "Home");
            }

            try
            {
                var allClaims = await _queries.GetAllClaimsAsync();
                var filteredClaims = status == "All"
                    ? allClaims
                    : allClaims.Where(c => c.Status == status).ToList();

                // Create CSV content (simple Excel-compatible format)
                var csvContent = new System.Text.StringBuilder();

                // Header
                csvContent.AppendLine($"Claims Report - {status} Claims - Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
                csvContent.AppendLine();

                // Column headers
                csvContent.AppendLine("ClaimID,LecturerID,LecturerName,LecturerEmail,Period,ModuleCode,Hours,Rate,TotalAmount,Status,SubmissionDate,PaymentDate,PaidBy");

                // Data rows
                foreach (var claim in filteredClaims)
                {
                    csvContent.AppendLine(
                        $"{claim.ClaimID}," +
                        $"{claim.User?.UserID}," +
                        $"\"{claim.User?.FirstName} {claim.User?.Surname}\"," +
                        $"\"{claim.User?.Email}\"," +
                        $"{claim.Month}/{claim.Year}," +
                        $"{claim.ModuleCode}," +
                        $"{claim.Hours}," +
                        $"{claim.HourlyRate}," +
                        $"{claim.TotalAmount}," +
                        $"{claim.Status}," +
                        $"{claim.SubmissionDate:yyyy-MM-dd}," +
                        $"{(claim.PaymentDate?.ToString("yyyy-MM-dd") ?? "N/A")}," +
                        $"\"{claim.PaidBy ?? "N/A"}\""
                    );
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent.ToString());
                return File(bytes, "text/csv", $"ClaimsReport_{status}_{DateTime.Now:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error exporting reports: " + ex.Message;
                return RedirectToAction("GenerateReports");
            }
        }

        // Private helper methods
        private async Task<int> GetLecturersCountAsync()
        {
            using (var connection = new SqlConnection(_queries.GetConnectionString()))
            {
                await connection.OpenAsync();
                string query = "SELECT COUNT(*) FROM Users WHERE Role = 'Lecturer'";
                using (var command = new SqlCommand(query, connection))
                {
                    return Convert.ToInt32(await command.ExecuteScalarAsync());
                }
            }
        }

        private async Task<int> GetPaidClaimsCountAsync()
        {
            using (var connection = new SqlConnection(_queries.GetConnectionString()))
            {
                await connection.OpenAsync();
                string query = "SELECT COUNT(*) FROM Claims WHERE Status = 'Paid'";
                using (var command = new SqlCommand(query, connection))
                {
                    return Convert.ToInt32(await command.ExecuteScalarAsync());
                }
            }
        }

        private async Task<List<User>> GetAllLecturersAsync()
        {
            using (var connection = new SqlConnection(_queries.GetConnectionString()))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM Users WHERE Role = 'Lecturer' ORDER BY FirstName, Surname";
                using (var command = new SqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var users = new List<User>();
                    while (await reader.ReadAsync())
                    {
                        users.Add(new User
                        {
                            UserID = reader.GetString(reader.GetOrdinal("UserID")),
                            Role = reader.GetString(reader.GetOrdinal("Role")),
                            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                            Surname = reader.GetString(reader.GetOrdinal("Surname")),
                            Address = reader.GetString(reader.GetOrdinal("Address")),
                            Email = reader.GetString(reader.GetOrdinal("Email")),
                            Password = reader.GetString(reader.GetOrdinal("Password")),
                            PhoneNumber = reader.GetString(reader.GetOrdinal("PhoneNumber")),
                            RegistrationDate = reader.GetDateTime(reader.GetOrdinal("RegistrationDate"))
                        });
                    }
                    return users;
                }
            }
        }

        private async Task<bool> UpdateLecturerAsync(User lecturer)
        {
            using (var connection = new SqlConnection(_queries.GetConnectionString()))
            {
                await connection.OpenAsync();
                string query = @"UPDATE Users SET 
                               FirstName = @FirstName, Surname = @Surname, Address = @Address, 
                               Email = @Email, PhoneNumber = @PhoneNumber
                               WHERE UserID = @UserID";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FirstName", lecturer.FirstName);
                    command.Parameters.AddWithValue("@Surname", lecturer.Surname);
                    command.Parameters.AddWithValue("@Address", lecturer.Address);
                    command.Parameters.AddWithValue("@Email", lecturer.Email);
                    command.Parameters.AddWithValue("@PhoneNumber", lecturer.PhoneNumber);
                    command.Parameters.AddWithValue("@UserID", lecturer.UserID);

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        private async Task<bool> DeleteLecturerAsync(string userId)
        {
            using (var connection = new SqlConnection(_queries.GetConnectionString()))
            {
                await connection.OpenAsync();
                string query = "DELETE FROM Users WHERE UserID = @UserID AND Role = 'Lecturer'";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userId);
                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        private async Task<bool> UpdateClaimPaymentStatusAsync(int claimId, string status, string paidBy)
        {
            using (var connection = new SqlConnection(_queries.GetConnectionString()))
            {
                await connection.OpenAsync();
                string query = @"UPDATE Claims SET Status = @Status, PaymentDate = @PaymentDate, PaidBy = @PaidBy 
                               WHERE ClaimID = @ClaimID";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@PaymentDate", DateTime.Now);
                    command.Parameters.AddWithValue("@PaidBy", paidBy);
                    command.Parameters.AddWithValue("@ClaimID", claimId);

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
        }
    }

    public class LecturerInvoice
    {
        public string UserID { get; set; } = string.Empty;
        public string LecturerName { get; set; } = string.Empty;
        public string LecturerEmail { get; set; } = string.Empty;
        public double TotalAmount { get; set; }
        public int ClaimCount { get; set; }
        public string Period { get; set; } = string.Empty;
        public List<Claim> Claims { get; set; } = new List<Claim>();
    }
}